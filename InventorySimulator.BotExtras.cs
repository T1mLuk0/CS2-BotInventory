using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

public partial class InventorySimulator
{
    private readonly Random _botExtraRng = new();

    private readonly Dictionary<int, int> _botExtraKits = new();
    private readonly Dictionary<int, Inventories.BotGloveSkin> _botExtraGloves = new();
    private readonly Dictionary<int, int> _botExtraGloveSeeds = new();
    private readonly Dictionary<int, float> _botExtraGloveWears = new();
    private readonly Dictionary<int, nint> _botExtraGlovePawnHandles = new();

    private MemoryFunctionVoid<nint, string, float>? _botGloveSetAttrByName;

    private ulong _nextBotExtraItemId = 0xF00DCAFE;

    private void InitBotExtras()
    {
        try
        {
            _botGloveSetAttrByName = new MemoryFunctionVoid<nint, string, float>(
                GameData.GetSignature("CAttributeList::SetOrAddAttributeValueByName")
            );
        }
        catch (Exception ex)
        {
            _botGloveSetAttrByName = null;
            Logger.LogError(
                $"[BotInventory] Failed to initialize glove attribute native from external gamedata: {ex.Message}"
            );
        }

        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _botExtraKits.Clear();
            _botExtraGloves.Clear();
            _botExtraGloveSeeds.Clear();
            _botExtraGloveWears.Clear();
            _botExtraGlovePawnHandles.Clear();

            // Agent models are applied without GC inventory, so precache the catalog models.
            foreach (var model in AgentCatalog.GetAllModels())
                Server.PrecacheModel(model);
        });

        RegisterEventHandler<EventPlayerSpawn>(OnBotExtraPlayerSpawn, HookMode.Pre);
        RegisterEventHandler<EventRoundMvp>(OnBotExtraRoundMvp, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnBotExtraPlayerTeam);
    }

    // Prepare cosmetics before spawn is broadcast so default gloves are not exposed.
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

        // Use a temporary custom-player loadout override to initialize agent model/voice identity.
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

        int? musicKitId = _botExtraKits.TryGetValue(slot, out int kitId)
            ? kitId
            : null;

        Inventories.BotGloveSkin? glove = null;
        if (_botExtraGloves.TryGetValue(slot, out var cachedGlove))
            glove = cachedGlove;

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
                // Keep female character identity in sync even when no explicit voice prefix exists.
                if (AgentCatalog.TryGet(assignedAgentDef.Value, out var agentDef))
                {
                    bool appliedSafeDedicatedVoice = false;
                    if (agentDef.RequiresDedicatedVoiceIdentity)
                    {
                        appliedSafeDedicatedVoice = AgentVoiceSync.ApplyModelAndVoice(
                            player,
                            currentPawn,
                            assignedAgentDef.Value,
                            "bot-extra-spawn-safe-voice"
                        );
                    }

                    if (!appliedSafeDedicatedVoice)
                    {
                        if (agentDef.IsDefaultMapCharacter)
                        {
                            currentPawn.SetModelFromClass();
                        }
                        else
                        {
                            currentPawn.SetModel(agentDef.Model);
                        }

                        Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");
                    }

                    var c = currentPawn.Render;
                    currentPawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
                    Utilities.SetStateChanged(currentPawn, "CBaseModelEntity", "m_clrRender");

                    // Spawn can rewrite character scalars; restore them on the next world update.
                    if (agentDef.RequiresDedicatedVoiceIdentity)
                    {
                        ushort finalAgentDef = assignedAgentDef.Value;
                        Server.NextWorldUpdate(() =>
                        {
                            if (!IsBotInventoryTarget(player) || !player.IsValid)
                                return;

                            var finalPawn = player.PlayerPawn.Value;
                            if (finalPawn == null
                                || !finalPawn.IsValid
                                || finalPawn.Handle != pawnHandle)
                                return;

                            AgentVoiceSync.ApplyVoiceOnly(
                                player,
                                finalPawn,
                                finalAgentDef,
                                "bot-extra-post-world-voice"
                            );
                        });
                    }
                }
            }
            else if (forcedMinimumModel)
            {
                currentPawn.SetModelFromLoadout();
                currentPawn.SetModelFromClass();
                Utilities.SetStateChanged(currentPawn, "CBaseEntity", "m_CBodyComponent");
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

                // Wait one world update for EconGloves to commit before restoring the bodygroup.
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
        if (_botGloveSetAttrByName == null)
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

            _botGloveSetAttrByName.Invoke(
                item.NetworkedDynamicAttributes.Handle,
                "set item texture prefab",
                paintKit
            );
            _botGloveSetAttrByName.Invoke(
                item.NetworkedDynamicAttributes.Handle,
                "set item texture seed",
                (float)seed
            );
            _botGloveSetAttrByName.Invoke(
                item.NetworkedDynamicAttributes.Handle,
                "set item texture wear",
                wear
            );

            _botGloveSetAttrByName.Invoke(
                item.AttributeList.Handle,
                "set item texture prefab",
                paintKit
            );
            _botGloveSetAttrByName.Invoke(
                item.AttributeList.Handle,
                "set item texture seed",
                (float)seed
            );
            _botGloveSetAttrByName.Invoke(
                item.AttributeList.Handle,
                "set item texture wear",
                wear
            );

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

        _botExtraGloves.Remove(slot);
        _botExtraGloveSeeds.Remove(slot);
        _botExtraGloveWears.Remove(slot);
        _botExtraGlovePawnHandles.Remove(slot);

        return HookResult.Continue;
    }

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

    private static bool IsBotInventoryTarget(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsHLTV)
            return false;

        return player.IsBot || Inventories.ShouldApplyToPlayers();
    }
}
