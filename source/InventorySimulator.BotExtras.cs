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
    private readonly Dictionary<int, nint> _botExtraGlovePawnHandles = new();

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
            _botExtraGlovePawnHandles.Clear();

            foreach (var model in Inventories.GetBotCtModels())
                Server.PrecacheModel(model);

            foreach (var model in Inventories.GetBotTModels())
                Server.PrecacheModel(model);

            // Agent mode is applied directly from the catalog and must not rely on a BOT GC
            // inventory object. Precache every official model used by the DefIndex pools.
            foreach (var model in AgentCatalog.GetAllModels())
                Server.PrecacheModel(model);
        });

        RegisterEventHandler<EventPlayerSpawn>(OnBotExtraPlayerSpawn, HookMode.Pre);
        RegisterEventHandler<EventRoundMvp>(OnBotExtraRoundMvp, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnBotExtraPlayerTeam);
    }

    // Apply persistent cosmetic extras before the spawn event is broadcast to clients.
    // This keeps the default glove model hidden while the final EconGloves item is prepared.
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

        // Select an agent DefIndex from the generated inventory. BOTs use a synchronous temporary
        // LOADOUT_SLOT_CLOTHING_CUSTOMPLAYER override so SetModelFromLoadout can initialize the
        // agent response/radio identity even without a Steam/GC inventory or BotHider.
        bool configuredAgentMode = Inventories.AreBotAgentsEnabled();
        bool forcedMinimumModel = ConVars.MinModels.Value > 0;
        ushort? assignedAgentDef = null;
        var inventory = player.GetState().Inventory;
        if (configuredAgentMode
            && !forcedMinimumModel
            && inventory?.Agents.TryGetValue(player.TeamNum, out var agentItem) == true)
        {
            assignedAgentDef = agentItem.Def;
        }

        string model = string.Empty;
        if (!configuredAgentMode && !forcedMinimumModel)
        {
            if (!_botExtraModels.TryGetValue(slot, out string? cachedModel))
            {
                string[] modelPool = (CsTeam)player.TeamNum == CsTeam.CounterTerrorist
                    ? Inventories.GetBotCtModels()
                    : Inventories.GetBotTModels();

                if (modelPool.Length > 0)
                {
                    cachedModel = modelPool[_botExtraRng.Next(modelPool.Length)];
                    _botExtraModels[slot] = cachedModel;
                }
                else
                {
                    cachedModel = string.Empty;
                }
            }

            model = cachedModel ?? string.Empty;
        }

        if (!_botExtraKits.ContainsKey(slot)
            && Inventories.TryGetRandomBotMusicKit(_botExtraRng, out int selectedMusicKit))
        {
            _botExtraKits[slot] = selectedMusicKit;
        }

        if (!_botExtraGloves.ContainsKey(slot)
            && Inventories.TryGetRandomBotGlove(_botExtraRng, out var generatedGlove))
        {
            _botExtraGloves[slot] = generatedGlove;
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
        var pawnHandle = pawn.Handle;
        var assignedModel = model ?? string.Empty;

        int? musicKitId = _botExtraKits.TryGetValue(slot, out int kitId)
            ? kitId
            : null;

        Inventories.BotGloveSkin? glove = null;
        if (_botExtraGloves.TryGetValue(slot, out var cachedGlove))
            glove = cachedGlove;

        // Prepare the final glove item before the spawn event is broadcast.  Agent and music
        // keep their proven dev1.4.1 NextFrame timing; only the glove visibility window changes.
        if (glove.HasValue)
        {
            var selectedGlove = glove.Value;
            pawn.AcceptInput("SetBodygroup", value: "first_or_third_person,0");
            ApplyBotInventoryGloves(
                player,
                pawn,
                selectedGlove.DefIndex,
                selectedGlove.PaintKit,
                slotSeed,
                slotWear
            );
        }

        Server.NextFrame(() =>
        {
            if (!IsBotInventoryTarget(player) || !player.IsValid)
                return;

            var currentPawn = player.PlayerPawn.Value;
            if (currentPawn == null
                || !currentPawn.IsValid
                || currentPawn.Handle != pawnHandle)
                return;

            if (configuredAgentMode && assignedAgentDef.HasValue)
            {
                if (player.IsBot)
                {
                    bool applied = AgentVoiceSync.ApplyModelAndVoice(
                        player,
                        currentPawn,
                        assignedAgentDef.Value,
                        "next-frame-bot-direct"
                    );

                    if (applied)
                    {
                        var c = currentPawn.Render;
                        currentPawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
                        Utilities.SetStateChanged(currentPawn, "CBaseModelEntity", "m_clrRender");
                    }
                    else
                    {
                        currentPawn.SetModelFromClass();
                        Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");
                    }

                    // BOT spawn setup can still touch character fields after the direct model is
                    // assigned. Reapply the identity block on the following world update.
                    Server.NextWorldUpdate(() =>
                    {
                        if (!player.IsValid || !player.IsBot)
                            return;

                        var voicePawn = player.PlayerPawn.Value;
                        if (voicePawn == null
                            || !voicePawn.IsValid
                            || voicePawn.Handle != pawnHandle)
                            return;

                        AgentVoiceSync.ApplyVoiceOnly(
                            player,
                            voicePawn,
                            assignedAgentDef.Value,
                            "next-world-update-bot-direct"
                        );
                    });
                }
                else
                {
                    // Custom paid agents are initialized by the loadout native only. A following
                    // SetModelFromClass can overwrite the paid-agent response profile with the map
                    // class. Humans must not be kept in the BOT reconciliation map.
                    AgentVoiceSync.StopTrackingPawn(currentPawn);
                    currentPawn.SetModelFromLoadout();
                    Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");
                }
            }
            else if (forcedMinimumModel)
            {
                currentPawn.SetModelFromLoadout();
                currentPawn.SetModelFromClass();
                Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");
            }
            else if (!configuredAgentMode
                && !forcedMinimumModel
                && !string.IsNullOrWhiteSpace(assignedModel))
            {
                // Legacy visual-only model pool. Kept only when Agents.Enabled is false.
                currentPawn.SetModel(assignedModel);
                Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");

                var c = currentPawn.Render;
                currentPawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
                Utilities.SetStateChanged(currentPawn, "CBaseModelEntity", "m_clrRender");
            }

            if (musicKitId.HasValue)
                ApplyBotExtraMusicKit(player, musicKitId.Value, 0);

            if (glove.HasValue)
            {
                var selectedGlove = glove.Value;
                var currentGloves = currentPawn.EconGloves;
                bool needsRewrite = !currentGloves.Initialized
                    || currentGloves.ItemDefinitionIndex != selectedGlove.DefIndex;

                ApplyBotInventoryGloves(
                    player,
                    currentPawn,
                    selectedGlove.DefIndex,
                    selectedGlove.PaintKit,
                    slotSeed,
                    slotWear,
                    forceRewrite: needsRewrite
                );

                // The old implementation left bodygroup 0 active for 0.2 seconds.  One world
                // update is sufficient for the engine to commit EconGloves and minimizes the
                // default-glove/empty-hand window seen by observers.
                Server.NextWorldUpdate(() =>
                {
                    if (!IsBotInventoryTarget(player) || !player.IsValid)
                        return;

                    var finalPawn = player.PlayerPawn.Value;
                    if (finalPawn == null
                        || !finalPawn.IsValid
                        || finalPawn.Handle != pawnHandle)
                        return;

                    var finalGloves = finalPawn.EconGloves;
                    if (!finalGloves.Initialized
                        || finalGloves.ItemDefinitionIndex != selectedGlove.DefIndex)
                    {
                        ApplyBotInventoryGloves(
                            player,
                            finalPawn,
                            selectedGlove.DefIndex,
                            selectedGlove.PaintKit,
                            slotSeed,
                            slotWear,
                            forceRewrite: true
                        );
                    }

                    finalPawn.AcceptInput("SetBodygroup", value: "first_or_third_person,1");
                });
            }

        });

        return HookResult.Continue;
    }

    private void ApplyBotInventoryGloves(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        ushort defIndex,
        int paintKit,
        int seed,
        float wear,
        bool forceRewrite = false
    )
    {
        if (_setAttrByName == null)
            return;

        try
        {
            if (!IsBotInventoryTarget(player) || pawn == null || !pawn.IsValid)
                return;

            var item = pawn.EconGloves;
            bool samePawn = _botExtraGlovePawnHandles.TryGetValue(player.Slot, out nint appliedPawn)
                && appliedPawn == pawn.Handle;
            bool alreadyApplied = samePawn
                && item.Initialized
                && item.ItemDefinitionIndex == defIndex;

            if (alreadyApplied && !forceRewrite)
                return;

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
            _botExtraGlovePawnHandles[player.Slot] = pawn.Handle;
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
        _botExtraGlovePawnHandles.Remove(slot);

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
