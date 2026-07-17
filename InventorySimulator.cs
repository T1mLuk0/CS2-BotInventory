/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;

namespace InventorySimulator;

public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "T1mLuk0 + Dilemma (based on ianlucas)";
    public override string ModuleDescription =>
        "Stable BOT paid-agent model and voice initialization";
    public override string ModuleName => "Bot-Inventory";
    public override string ModuleVersion => "1.3.0-dev1.4.20-agentvoice-stable-clean";

    private bool _inventoryHooksRegistered;

    private bool IsBotPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsHLTV)
            return false;

        if (player.IsBot)
            return true;

        return Inventories.ShouldApplyToPlayers();
    }

    public override void Load(bool hotReload)
    {
        CSS.Initialize(this);
        Inventories.InitBotInventoryConfig();
        InitBotExtras();

        // Keep explicit paid-agent identity coherent while a BOT controls the pawn. Agents without
        // vo_prefix are visual-only, and human takeover is intentionally left to the game.
        AddTimer(
            0.25f,
            ReconcileAgentVoiceIdentities,
            TimerFlags.REPEAT
        );

        // State is keyed by the controller entity and must not survive a map/controller lifetime.
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            CCSPlayerControllerExtensions.ClearAllStates();
            Inventories.ClearAgentAssignments();
            AgentVoiceSync.Clear();
        });
        RegisterListener<Listeners.OnEntityDeleted>(OnControllerEntityDeleted);
        RegisterEventHandler<EventPlayerDisconnect>(OnBotInventoryDisconnect, HookMode.Post);

        // Keep the original InventorySimulator injection mechanism.  Only the owner lookup is
        // changed: BOTs are resolved through their controller/inventory handle, not SteamID/SOCache.
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPre, HookMode.Pre);
        Natives.CCSPlayerInventory_GetItemInLoadout.Hook(GetItemInLoadout, HookMode.Post);
        _inventoryHooksRegistered = true;

        Logger.LogInformation(
            "[BotInventory] Loaded CEconItemView bridge. Bot owners use controller/inventory handles; BotHider is not required."
        );
    }

    private static void ReconcileAgentVoiceIdentities()
    {
        foreach (var player in Utilities.GetPlayers())
            AgentVoiceSync.ReconcilePlayer(player);
    }

    private void OnControllerEntityDeleted(CEntityInstance entity)
    {
        if (entity.DesignerName != "cs_player_controller")
            return;

        var controller = entity.As<CCSPlayerController>();
        CCSPlayerControllerExtensions.RemoveState(controller.Index);
    }

    private HookResult OnBotInventoryDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null)
            CCSPlayerControllerExtensions.RemoveState(player.Index);
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        if (_inventoryHooksRegistered)
        {
            VirtualFunctions.GiveNamedItemFunc.Unhook(OnGiveNamedItemPre, HookMode.Pre);
            Natives.CCSPlayerInventory_GetItemInLoadout.Unhook(GetItemInLoadout, HookMode.Post);
            _inventoryHooksRegistered = false;
        }

        CCSPlayerControllerExtensions.ClearAllStates();
        Inventories.ClearAgentAssignments();
        AgentVoiceSync.Clear();
    }
}
