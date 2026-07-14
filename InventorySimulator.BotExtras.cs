using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

public partial class InventorySimulator
{
    private readonly Random _botExtraRng = new();

    private readonly Dictionary<int, string> _botExtraModels = new();
    private readonly Dictionary<int, int> _botExtraKits = new();
    private readonly Dictionary<int, Inventories.BotGloveSkin> _botExtraGloves = new();
    private readonly Dictionary<int, int> _botExtraGloveSeeds = new();
    private readonly Dictionary<int, float> _botExtraGloveWears = new();

    private MemoryFunctionVoid<nint, string, float>? _setAttrByName;
    private ulong _nextBotExtraItemId = 0xF00DCAFE;
private void InitBotExtras()
    {
        try
        {
            _setAttrByName = new MemoryFunctionVoid<nint, string, float>(
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? "55 48 89 E5 41 57 41 56 49 89 FE 41 55 41 54 53 48 89 F3 48 83 EC ? F3 0F 11 85"
                    : "40 53 55 41 56 48 81 EC 90 00 00 00");
        }
        catch (Exception ex)
        {
            _setAttrByName = null;
            Logger.LogError($"[BotInventory] SetOrAddAttributeValueByName signature failed: {ex.Message}，手套功能会失效");
        }

        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _botExtraModels.Clear();
            _botExtraKits.Clear();
            _botExtraGloves.Clear();
            _botExtraGloveSeeds.Clear();
            _botExtraGloveWears.Clear();

            foreach (var model in Inventories.GetBotCtModels())
                Server.PrecacheModel(model);

            foreach (var model in Inventories.GetBotTModels())
                Server.PrecacheModel(model);
        });

        RegisterEventHandler<EventPlayerSpawn>(OnBotExtraPlayerSpawn);
        RegisterEventHandler<EventRoundMvp>(OnBotExtraRoundMvp, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnBotExtraPlayerTeam);
    }

    // Apply persistent cosmetic extras when a target player spawns
    private HookResult OnBotExtraPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!IsBotInventoryTarget(player))
            return HookResult.Continue;

        if (player!.PlayerPawn == null
            || !player.PlayerPawn.IsValid
            || player.PlayerPawn.Value == null
            || !player.PlayerPawn.Value.IsValid)
            return HookResult.Continue;

        if ((CsTeam)player.TeamNum != CsTeam.CounterTerrorist
            && (CsTeam)player.TeamNum != CsTeam.Terrorist)
            return HookResult.Continue;

        int slot = player.Slot;

        if (!_botExtraModels.TryGetValue(slot, out string? model))
        {
            string[] modelPool = (CsTeam)player.TeamNum == CsTeam.CounterTerrorist
                ? Inventories.GetBotCtModels()
                : Inventories.GetBotTModels();

            if (modelPool.Length > 0)
            {
                model = modelPool[_botExtraRng.Next(modelPool.Length)];
                _botExtraModels[slot] = model;
            }
            else
            {
                model = string.Empty;
            }
        }

        if (!_botExtraKits.ContainsKey(slot)
            && Inventories.TryGetRandomBotMusicKit(_botExtraRng, out int selectedMusicKit))
        {
            _botExtraKits[slot] = selectedMusicKit;
        }

        if (!_botExtraGloves.ContainsKey(slot)
            && Inventories.TryGetRandomBotGlove(_botExtraRng, out var selectedGlove))
        {
            _botExtraGloves[slot] = selectedGlove;
        }

        int slotSeed = 0;
        float slotWear = 0.0f;

        if (_botExtraGloves.ContainsKey(slot))
        {
            if (!_botExtraGloveSeeds.ContainsKey(slot))
                _botExtraGloveSeeds[slot] = Inventories.CreateBotGloveSeed(_botExtraRng);

            if (!_botExtraGloveWears.ContainsKey(slot))
                _botExtraGloveWears[slot] = Inventories.CreateBotGloveWear(_botExtraRng);

            slotSeed = _botExtraGloveSeeds[slot];
            slotWear = _botExtraGloveWears[slot];
        }

        var pawn = player.PlayerPawn.Value;
        var assignedModel = model ?? string.Empty;

        int? musicKitId = _botExtraKits.TryGetValue(slot, out int kitId)
            ? kitId
            : null;

        Inventories.BotGloveSkin? glove = null;
        if (_botExtraGloves.TryGetValue(slot, out var cachedGlove))
            glove = cachedGlove;

        var gloveSeed = slotSeed;
        var gloveWear = slotWear;

        Server.NextFrame(() =>
        {
            if (!IsBotInventoryTarget(player))
                return;

            if (pawn == null || !pawn.IsValid)
                return;

            if (!string.IsNullOrWhiteSpace(assignedModel))
            {
                pawn.SetModel(assignedModel);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");

                var c = pawn.Render;
                pawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }

            if (musicKitId.HasValue)
                ApplyBotExtraMusicKit(player, musicKitId.Value, 0);

            if (glove.HasValue)
            {
                var selectedGlove = glove.Value;
                Server.NextFrame(() =>
                {
                    if (!IsBotInventoryTarget(player) || !pawn.IsValid) return;
                    ApplyBotInventoryGloves(player, pawn, selectedGlove.DefIndex, selectedGlove.PaintKit, gloveSeed, gloveWear);
                    Server.NextFrame(() =>
                    {
                        if (IsBotInventoryTarget(player) && pawn.IsValid)
                            ApplyBotInventoryGloves(player, pawn, selectedGlove.DefIndex, selectedGlove.PaintKit, gloveSeed, gloveWear);
                    });
                });
            }  

            string musicLog = musicKitId.HasValue ? musicKitId.Value.ToString() : "disabled";
            string gloveLog = glove.HasValue ? $"{glove.Value.Name}, seed={gloveSeed}, wear={gloveWear:0.0000}" : "disabled";

            Logger.LogInformation($"[BotInventory] Bot extras: slot={slot}, music={musicLog}, glove={gloveLog}");
        });

        return HookResult.Continue;
    }

    private void ApplyBotInventoryGloves(CCSPlayerController player, CCSPlayerPawn pawn, ushort defIndex, int paintKit, int seed, float wear)
    {
        if (_setAttrByName == null)
            return;

        try
        {
            if (!IsBotInventoryTarget(player) || pawn == null || !pawn.IsValid)
                return;

            var item = pawn.EconGloves;

            item.NetworkedDynamicAttributes.Attributes.RemoveAll();
            item.AttributeList.Attributes.RemoveAll();

            item.ItemDefinitionIndex = defIndex;
            AssignBotExtraItemId(item);

            _setAttrByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture prefab", paintKit);
            _setAttrByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture seed", (float)seed);
            _setAttrByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture wear", wear);

            _setAttrByName.Invoke(item.AttributeList.Handle, "set item texture prefab", paintKit);
            _setAttrByName.Invoke(item.AttributeList.Handle, "set item texture seed", (float)seed);
            _setAttrByName.Invoke(item.AttributeList.Handle, "set item texture wear", wear);

            item.Initialized = true;

            pawn.AcceptInput("SetBodygroup", value: "first_or_third_person,0");
            AddTimer(0.2f, () =>
            {
                if (pawn.IsValid)
                    pawn.AcceptInput("SetBodygroup", value: "first_or_third_person,1");
            });
        }
        catch (Exception ex)
        {
            Logger.LogError($"[BotInventory] ApplyBotInventoryGloves failed: {ex.Message}");
        }
    }

    private void AssignBotExtraItemId(CEconItemView item)
    {
        var id = unchecked(_nextBotExtraItemId++);
        item.ItemID = id;
        item.ItemIDLow = (uint)(id & 0xFFFFFFFF);
        item.ItemIDHigh = (uint)(id >> 32);
    }

    private HookResult OnBotExtraPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!IsBotInventoryTarget(player))
            return HookResult.Continue;

        int slot = player!.Slot;

        _botExtraModels.Remove(slot);
        _botExtraGloves.Remove(slot);
        _botExtraGloveSeeds.Remove(slot);
        _botExtraGloveWears.Remove(slot);

        return HookResult.Continue;
    }

    // Publish the assigned music kit through the original MVP event
    private HookResult OnBotExtraRoundMvp(EventRoundMvp @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!IsBotInventoryTarget(player))
            return HookResult.Continue;

        if (!_botExtraKits.TryGetValue(player!.Slot, out int kitId))
            return HookResult.Continue;

        ApplyBotExtraMusicKit(player, kitId, 0);
        @event.Musickitid = kitId;
        @event.Musickitmvps = 0;
        @event.Nomusic = 0;

        return HookResult.Continue;
    }

    // Synchronize every music-kit field used by the controller and MVP panel
    private static void ApplyBotExtraMusicKit(CCSPlayerController player, int kitId, int musicKitMvps)
    {
        var inventory = player.InventoryServices;
        if (inventory != null)
        {
            inventory.MusicID = checked((ushort)kitId);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }

        player.MusicKitID = kitId;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");
        player.MusicKitMVPs = musicKitMvps;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitMVPs");
        player.MvpNoMusic = false;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_bMvpNoMusic");
    }

    private bool IsBotInventoryTarget(CCSPlayerController? player)
    {
         if (player == null || !player.IsValid || player.IsHLTV)
         return false;

         if (player.IsBot)
         return true;

         return Inventories.ShouldApplyToPlayers();
    }
}
