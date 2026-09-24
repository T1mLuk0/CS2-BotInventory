/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "T1mLuk0 + Dilemma (based on ianlucas)";
    public override string ModuleDescription =>
        "Advanced BOT cosmetic inventory with personalized agent models/voice and compact JSON config";
    public override string ModuleName => "BotInventory";
    public override string ModuleVersion => "1.3.0";

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

        // Controller state must not survive map/controller lifetimes.
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            CCSPlayerControllerExtensions.ClearAllStates();
            Inventories.ClearAgentAssignments();
            AgentVoiceSync.Clear();
        });
        RegisterListener<Listeners.OnEntityDeleted>(OnControllerEntityDeleted);
        RegisterEventHandler<EventPlayerDisconnect>(OnBotInventoryDisconnect, HookMode.Post);

        // BOT ownership is resolved by controller/inventory handle, not SteamID/SOCache.
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPre, HookMode.Pre);
        Natives.CCSPlayerInventory_GetItemInLoadout.Hook(GetItemInLoadout, HookMode.Post);
        _inventoryHooksRegistered = true;

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
