using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

internal sealed record SouvenirRoll(string Event, List<StickerItem> Stickers);

internal static class SouvenirSystem
{
    private const int StickerCount = 4;
    private const string ResourceName = "InventorySimulator.Resources.souvenir_gold_catalog.json";

    private static readonly Lazy<SouvenirCatalog> Catalog = new(LoadCatalog);

    internal static SouvenirRoll? Roll(Random random, bool enabled, double chance)
    {
        if (!enabled || random.NextDouble() >= Math.Clamp(chance, 0.0, 1.0))
            return null;

        var catalog = Catalog.Value;
        if (!catalog.Enabled || catalog.Events.Count == 0)
            return null;

        var souvenirEvent = PickWeighted(random, catalog.Events, entry => entry.Weight);
        var stickerIds = RollCraft(random, souvenirEvent);
        var stickers = new List<StickerItem>(StickerCount);
        for (uint slot = 0; slot < StickerCount; slot++)
        {
            stickers.Add(new StickerItem
            {
                Def = stickerIds[(int)slot],
                Slot = slot,
                Schema = slot,
                Wear = 0.0f,
                Rotation = 0,
                X = null,
                Y = null
            });
        }

        return new SouvenirRoll(souvenirEvent.Name, stickers);
    }

    private static SouvenirCatalog LoadCatalog()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException($"Missing embedded resource '{ResourceName}'.");
            var document = JsonSerializer.Deserialize<SouvenirCatalogDocument>(stream)
                ?? throw new InvalidDataException("Embedded souvenir catalog is empty.");

            var events = new List<SouvenirEvent>();
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var eventDocument in document.Events)
            {
                if (!eventDocument.Enabled)
                    continue;
                if (string.IsNullOrWhiteSpace(eventDocument.Name)
                    || eventDocument.Weight <= 0
                    || eventDocument.GeneratedWeight < 0
                    || !usedNames.Add(eventDocument.Name))
                {
                    throw new InvalidDataException("Embedded souvenir catalog contains an invalid event.");
                }

                var goldStickers = eventDocument.GoldStickers
                    .Where(sticker => sticker > 0)
                    .Distinct()
                    .ToArray();
                if (goldStickers.Length == 0
                    || (!eventDocument.AllowDuplicates && goldStickers.Length < StickerCount))
                {
                    throw new InvalidDataException(
                        $"Souvenir event '{eventDocument.Name}' has an insufficient Gold sticker pool.");
                }

                var crafts = new List<SouvenirCraft>();
                var usedCrafts = new HashSet<string>(StringComparer.Ordinal);
                foreach (var craftDocument in eventDocument.Crafts)
                {
                    if (!craftDocument.Enabled)
                        continue;
                    if (craftDocument.Weight <= 0
                        || craftDocument.Stickers.Count != StickerCount
                        || craftDocument.Stickers.Any(sticker => !goldStickers.Contains(sticker)))
                    {
                        throw new InvalidDataException(
                            $"Souvenir event '{eventDocument.Name}' contains an invalid craft.");
                    }

                    var stickerIds = craftDocument.Stickers.ToArray();
                    if (!usedCrafts.Add(string.Join(",", stickerIds)))
                    {
                        throw new InvalidDataException(
                            $"Souvenir event '{eventDocument.Name}' contains a duplicate craft.");
                    }
                    crafts.Add(new SouvenirCraft(craftDocument.Weight, stickerIds));
                }

                if (eventDocument.GeneratedWeight == 0 && crafts.Count == 0)
                {
                    throw new InvalidDataException(
                        $"Souvenir event '{eventDocument.Name}' has no craft source.");
                }

                events.Add(new SouvenirEvent(
                    eventDocument.Name,
                    eventDocument.Weight,
                    eventDocument.GeneratedWeight,
                    eventDocument.AllowDuplicates,
                    goldStickers,
                    crafts));
            }

            if (document.Enabled && events.Count == 0)
                throw new InvalidDataException("Embedded souvenir catalog has no enabled events.");

            return new SouvenirCatalog(document.Enabled, events);
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError(
                $"BotInventory disabled souvenir crafts: {ex.Message}"
            );
            return new SouvenirCatalog(false, Array.Empty<SouvenirEvent>());
        }
    }

    private static IReadOnlyList<uint> RollCraft(Random random, SouvenirEvent souvenirEvent)
    {
        var totalWeight = souvenirEvent.GeneratedWeight;
        foreach (var craft in souvenirEvent.Crafts)
            totalWeight = checked(totalWeight + craft.Weight);

        var roll = random.Next(totalWeight);
        if (roll < souvenirEvent.GeneratedWeight)
            return PickGoldStickers(random, souvenirEvent.GoldStickers, souvenirEvent.AllowDuplicates);
        roll -= souvenirEvent.GeneratedWeight;

        foreach (var craft in souvenirEvent.Crafts)
        {
            if (roll < craft.Weight)
                return craft.Stickers;
            roll -= craft.Weight;
        }

        throw new InvalidOperationException("Souvenir craft selection did not resolve.");
    }

    private static IReadOnlyList<uint> PickGoldStickers(
        Random random,
        IReadOnlyList<uint> stickers,
        bool allowDuplicates)
    {
        var result = new uint[StickerCount];
        if (stickers.Count < StickerCount)
        {
            if (!allowDuplicates)
                throw new InvalidOperationException("Souvenir Gold sticker pool is too small.");
            for (var index = 0; index < StickerCount; index++)
                result[index] = stickers[random.Next(stickers.Count)];
            return result;
        }

        var indexes = Enumerable.Range(0, stickers.Count).ToArray();
        for (var index = 0; index < StickerCount; index++)
        {
            var selected = random.Next(index, indexes.Length);
            (indexes[index], indexes[selected]) = (indexes[selected], indexes[index]);
            result[index] = stickers[indexes[index]];
        }
        return result;
    }

    private static T PickWeighted<T>(Random random, IReadOnlyList<T> values, Func<T, int> getWeight)
    {
        var totalWeight = 0;
        foreach (var value in values)
            totalWeight = checked(totalWeight + getWeight(value));

        var roll = random.Next(totalWeight);
        foreach (var value in values)
        {
            var weight = getWeight(value);
            if (roll < weight)
                return value;
            roll -= weight;
        }

        throw new InvalidOperationException("Souvenir event selection did not resolve.");
    }

    private sealed record SouvenirCatalog(bool Enabled, IReadOnlyList<SouvenirEvent> Events);

    private sealed record SouvenirEvent(
        string Name,
        int Weight,
        int GeneratedWeight,
        bool AllowDuplicates,
        IReadOnlyList<uint> GoldStickers,
        IReadOnlyList<SouvenirCraft> Crafts);

    private sealed record SouvenirCraft(int Weight, IReadOnlyList<uint> Stickers);

    private sealed class SouvenirCatalogDocument
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; } = true;

        [JsonPropertyName("events")]
        public List<SouvenirEventDocument> Events { get; init; } = [];
    }

    private sealed class SouvenirEventDocument
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; } = true;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("weight")]
        public int Weight { get; init; } = 1;

        [JsonPropertyName("generatedWeight")]
        public int GeneratedWeight { get; init; } = 1;

        [JsonPropertyName("allowDuplicates")]
        public bool AllowDuplicates { get; init; }

        [JsonPropertyName("goldStickers")]
        public List<uint> GoldStickers { get; init; } = [];

        [JsonPropertyName("crafts")]
        public List<SouvenirCraftDocument> Crafts { get; init; } = [];
    }

    private sealed class SouvenirCraftDocument
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; } = true;

        [JsonPropertyName("weight")]
        public int Weight { get; init; } = 1;

        [JsonPropertyName("stickers")]
        public List<uint> Stickers { get; init; } = [];
    }
}
