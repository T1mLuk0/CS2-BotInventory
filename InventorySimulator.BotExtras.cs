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
    private readonly Dictionary<int, int> _botExtraGloves = new();
    private readonly Dictionary<int, int> _botExtraGloveSeeds = new();
    private readonly Dictionary<int, float> _botExtraGloveWears = new();

    private MemoryFunctionVoid<nint, string, float>? _setAttrByName;
    private ulong _nextBotExtraItemId = 0xF00DCAFE;
    private bool _handlingBotExtraMvp = false;

    private static readonly (ushort DefIndex, int PaintKit, string Name)[] HighValueGloves =
    {
    // Sport Gloves
    (5030, 10048, "Sport Gloves | Vice"), // Vice
    (5030, 1407, "Sport Gloves | Big Game"),  // Big Game
    (5030, 1406, "Sport Gloves | Nocts"),  // Nocts
    (5030, 10038, "Sport Gloves | Hedge Maze"), // Hedge Maze
    (5030, 1405, "Sport Gloves | Scarlet Shamagh"), // Scarlet Shamagh
    (5030, 1417, "Sport Gloves | Bronze Morph"),  // Bronze Morph
    (5030, 10045, "Sport Gloves | Amphibious"), // Amphibious 
    (5030, 10037, "Sport Gloves | Pandora's Box"), // Pandora's Box
    (5030, 1410, "Sport Gloves | Omega"), // Omega
    (5030, 10073, "Sport Gloves | Slingshot"), // Slingshot
    (5030, 10018, "Sport Gloves | Superconductor"), // Superconductor    
    (5030, 10076, "Sport Gloves | Nocts"), // Nocts
    (5030, 10047, "Sport Gloves | Omega"), // Omega
    (5030, 10075, "Sport Gloves | Scarlet Shamagh"), // Scarlet Shamagh
    (5030, 10074, "Sport Gloves | Big Game"), // Big Game
    (5030, 10046, "Sport Gloves | Bronze Morph"), // Bronze Morph
    (5030, 1408, "Sport Gloves | Creme Pinstripe"), // Creme Pinstripe
    (5030, 1409, "Sport Gloves | Red Racer"), // Red Racer
    (5030, 10019, "Sport Gloves | Arid"), // Arid

    // Moto Gloves
    (5033, 10026, "Moto Gloves | Spearmint"), // Spearmint
    (5033, 10078, "Moto Gloves | Smoke Out"), // Smoke Out
    (5033, 10052, "Moto Gloves | Polygon"), // Polygon
    (5033, 10079, "Moto Gloves | Blood Pressure"), // Blood Pressure
    (5033, 10049, "Moto Gloves | POW!"), // POW!
    (5033, 10051, "Moto Gloves | Transport"), // Transport
    (5033, 10050, "Moto Gloves | Turtle"), // Turtle
    (5033, 10077, "Moto Gloves | Finish Line"), // Finish Line
    (5033, 10080, "Moto Gloves | 3rd Commando Company"), // 3rd Commando Company
    (5033, 10028, "Moto Gloves | Cool Mint"), // Cool Mint
    (5033, 10024, "Moto Gloves | Eclipse"), // Eclipse
    (5033, 10027, "Moto Gloves | Boom!"), // Boom!

    // Specialist Gloves
    (5034, 10033, "Specialist Gloves | Crimson Kimono"), // Crimson Kimono
    (5034, 10034, "Specialist Gloves | Emerald Web"), // Emerald Web
    (5034, 1413, "Specialist Gloves | Marble Fade"),  // Marble Fade
    (5034, 1438, "Specialist Gloves | Pillow Punchers"),  // Pillow Punchers
    (5034, 1416, "Specialist Gloves | Tiger Strike"),  // Tiger Strike
    (5034, 10061, "Specialist Gloves | Crimson Web"), // Crimson Web
    (5034, 1440, "Specialist Gloves | Cloud Chaser"), // Cloud Chaser
    (5034, 1437, "Specialist Gloves | Big Swell"), // Big Swell
    (5034, 10063, "Specialist Gloves | Fade"), // Fade
    (5034, 1414, "Specialist Gloves | Blackbook"), // Blackbook
    (5034, 10065, "Specialist Gloves | Marble Fade"), // Marble Fade
    (5034, 10064, "Specialist Gloves | Mogul"), // Mogul
    (5034, 10068, "Specialist Gloves | Field Agent"), // Field Agent
    (5034, 1415, "Specialist Gloves | Chocolate Chesterfield"), // Chocolate Chesterfield
    (5034, 10067, "Specialist Gloves | Tiger Strike"), // Tiger Strike
    (5034, 10066, "Specialist Gloves | Lt. Commander"), // Lt. Commander
    (5034, 10062, "Specialist Gloves | Buckshot"), // Buckshot
    (5034, 10030, "Specialist Gloves | Forest DDPAT"), // Forest DDPAT
    (5034, 10035, "Specialist Gloves | Foundation"), // Foundation

    // Driver Gloves
    (5031, 1399, "Driver Gloves | Rezan the Red"),  // Rezan the Red
    (5031, 10016, "Driver Gloves | Crimson Weave"), // Crimson Weave
    (5031, 1401, "Driver Gloves | Imperial Plaid"), // Imperial Plaid
    (5031, 10041, "Driver Gloves | King Snake"), // King Snake
    (5031, 1398, "Driver Gloves | Wave Chaser"),  // Wave Chaser
    (5031, 10070, "Driver Gloves | Snow Leopard"), // Snow Leopard
    (5031, 10042, "Driver Gloves | Imperial Plaid"), // Imperial Plaid
    (5031, 10072, "Driver Gloves | Black Tie"), // Black Tie
    (5031, 1400, "Driver Gloves | Brocade Flowers"), // Brocade Flowers
    (5031, 1404, "Driver Gloves | Seigaiha"), // Seigaiha
    (5031, 1402, "Driver Gloves | Garden"), // Garden
    (5031, 1439, "Driver Gloves | Hand Sweaters"), // Hand Sweaters
    (5031, 10071, "Driver Gloves | Queen Jaguar"), // Queen Jaguar
    (5031, 1412, "Driver Gloves | Plum Quill"), // Plum Quill
    (5031, 10069, "Driver Gloves | Rezan the Red"), // Rezan the Red
    (5031, 10043, "Driver Gloves | Overtake"), // Overtake
    (5031, 10044, "Driver Gloves | Racing Green"), // Racing Green
    (5031, 10013, "Driver Gloves | Lunar Weave"), // Lunar Weave
    (5031, 10015, "Driver Gloves | Convoy"), // Convoy
    (5031, 10040, "Driver Gloves | Diamondback"), // Diamondback
    };
    
      

    private static readonly int[] HighValueMusicKits =
    {
        2, 3, 4, 5, 6, 7, 8, 9, 10,
        14, 15, 16, 17, 18, 19, 20,
        22, 23, 24, 25, 26, 27, 28,
        32, 33, 34, 35, 36, 37, 38,
        39, 40, 41, 42, 43, 44, 45,
    };

    private static readonly string[] CtModels =
    {
        "agents\\models\\ctm_sas\\ctm_sas_variantf.vmdl",
        "agents\\models\\ctm_sas\\ctm_sas_variantg.vmdl",
        "agents\\models\\ctm_st6\\ctm_st6_variantg.vmdl",
        "agents\\models\\ctm_st6\\ctm_st6_varianti.vmdl",
        "agents\\models\\ctm_st6\\ctm_st6_variantj.vmdl",
        "agents\\models\\ctm_st6\\ctm_st6_variantk.vmdl",
        "agents\\models\\ctm_fbi\\ctm_fbi_variantf.vmdl",
        "agents\\models\\ctm_fbi\\ctm_fbi_variantg.vmdl",
        "agents\\models\\ctm_swat\\ctm_swat_variantj.vmdl",
        "agents\\models\\ctm_gendarmerie\\ctm_gendarmerie_variante.vmdl",
    };

    private static readonly string[] TModels =
    {
        "agents\\models\\tm_professional\\tm_professional_varf.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varf1.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varf2.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varf3.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varf4.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varf5.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varg.vmdl",
        "agents\\models\\tm_professional\\tm_professional_varh.vmdl",
        "agents\\models\\tm_jungle_raider\\tm_jungle_raider_variantf.vmdl",
        "agents\\models\\tm_leet\\tm_leet_varianti.vmdl",
    };

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

            foreach (var model in CtModels)
                Server.PrecacheModel(model);

            foreach (var model in TModels)
                Server.PrecacheModel(model);
        });

        RegisterEventHandler<EventPlayerSpawn>(OnBotExtraPlayerSpawn);
        RegisterEventHandler<EventRoundMvp>(OnBotExtraRoundMvp, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnBotExtraPlayerTeam);
    }

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
            string[] modelPool = (CsTeam)player.TeamNum == CsTeam.CounterTerrorist ? CtModels : TModels;
            model = modelPool[_botExtraRng.Next(modelPool.Length)];
            _botExtraModels[slot] = model;
        }

        if (!_botExtraKits.ContainsKey(slot))
            _botExtraKits[slot] = HighValueMusicKits[_botExtraRng.Next(HighValueMusicKits.Length)];

        if (!_botExtraGloves.ContainsKey(slot))
            _botExtraGloves[slot] = _botExtraRng.Next(HighValueGloves.Length);

        if (!_botExtraGloveSeeds.ContainsKey(slot))
            _botExtraGloveSeeds[slot] = _botExtraRng.Next(1, 10000);

        if (!_botExtraGloveWears.ContainsKey(slot))
            _botExtraGloveWears[slot] = (float)(_botExtraRng.NextDouble() * 0.06);

        var pawn = player.PlayerPawn.Value;
        var assignedModel = model;
        var musicKitId = _botExtraKits[slot];
        var glove = HighValueGloves[_botExtraGloves[slot]];
        var gloveSeed = _botExtraGloveSeeds[slot];
        var gloveWear = _botExtraGloveWears[slot];

        Server.NextFrame(() =>
        {
            if (!IsBotInventoryTarget(player))
                return;

            if (pawn == null || !pawn.IsValid)
                return;

            pawn.SetModel(assignedModel);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");

            var c = pawn.Render;
            pawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

            player.MusicKitID = musicKitId;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");

            ApplyBotInventoryGloves(player, pawn, glove.DefIndex, glove.PaintKit, gloveSeed, gloveWear);

            AddTimer(0.10f, () =>
            {
                if (IsBotInventoryTarget(player) && pawn.IsValid)
                    ApplyBotInventoryGloves(player, pawn, glove.DefIndex, glove.PaintKit, gloveSeed, gloveWear);
            });

            AddTimer(0.25f, () =>
            {
                if (IsBotInventoryTarget(player) && pawn.IsValid)
                    ApplyBotInventoryGloves(player, pawn, glove.DefIndex, glove.PaintKit, gloveSeed, gloveWear);
            });

            Logger.LogInformation($"[BotInventory] Bot extras: slot={slot}, music={musicKitId}, glove={glove.Name}, seed={gloveSeed}, wear={gloveWear:0.0000}");
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

    private HookResult OnBotExtraRoundMvp(EventRoundMvp @event, GameEventInfo info)
    {
        if (_handlingBotExtraMvp)
            return HookResult.Continue;

        var player = @event.Userid;

        if (!IsBotInventoryTarget(player))
            return HookResult.Continue;

        if (!_botExtraKits.TryGetValue(player!.Slot, out int kitId))
            return HookResult.Continue;

        info.DontBroadcast = true;
        _handlingBotExtraMvp = true;

        if (player.MusicKitID != kitId)
        {
            player.MusicKitID = kitId;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");
        }

        EventRoundMvp? newEvent = null;

        try
        {
            newEvent = new EventRoundMvp(true)
            {
                Userid = player,
                Musickitid = kitId,
                Nomusic = 0,
                Reason = @event.Reason,
                Value = @event.Value,
            };

            foreach (var human in Utilities.GetPlayers()
                         .Where(p => p.IsValid && !p.IsHLTV && !p.IsBot))
            {
                try
                {
                    newEvent.FireEventToClient(human);
                }
                catch
                {
                }
            }
        }
        finally
        {
            try
            {
                newEvent?.Free();
            }
            catch
            {
            }

            _handlingBotExtraMvp = false;
        }

        return HookResult.Continue;
    }

    private bool IsBotInventoryTarget(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsHLTV)
            return false;


        return IsBotPlayer(player);
    }
}