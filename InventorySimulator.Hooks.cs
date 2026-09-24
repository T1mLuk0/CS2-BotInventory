/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public HookResult OnGiveNamedItemPre(DynamicHook hook)
    {
        try
        {
            var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
            var designerName = hook.GetParam<string>(1);
            var controller = itemServices.GetController();

            if (!IsBotPlayer(controller) || controller?.InventoryServices == null)
                return HookResult.Continue;

            var itemDef = SchemaHelper.GetItemSchema()?.GetItemDefinitionByName(designerName);
            if (itemDef == null)
                return HookResult.Continue;

            var controllerState = controller.GetState();
            EnsureInventory(controller, controllerState);

            var item = controllerState.Inventory?.GetItemForSlot(
                controller.TeamNum,
                itemDef.DefaultLoadoutSlot,
                itemDef.DefIndex,
                ConVars.IsFallbackTeam.Value
            );

            if (item != null)
            {
                hook.SetParam(
                    3,
                    controllerState.GetEconItemView(
                        controller.TeamNum,
                        (int)itemDef.DefaultLoadoutSlot,
                        item
                    )
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[BotInventory] GiveNamedItem pre hook failed: {ex.Message}");
        }

        return HookResult.Continue;
    }

    public HookResult GetItemInLoadout(DynamicHook hook)
    {
        try
        {
            var inventoryHandle = hook.GetParam<nint>(0);
            int team = hook.GetParam<int>(1);
            int slot = hook.GetParam<int>(2);
            nint originalReturn = hook.GetReturn<nint>();

            if (inventoryHandle == nint.Zero)
                return HookResult.Continue;

            var player = FindControllerByInventoryHandle(inventoryHandle);
            if (player == null || !IsBotPlayer(player))
                return HookResult.Continue;

            // BOTs without GC/SOCache can have a null original loadout item.
            ushort fallbackDefIndex = 0;
            if (originalReturn != nint.Zero)
                fallbackDefIndex = new CEconItemView(originalReturn).ItemDefinitionIndex;

            var controllerState = player.GetState();
            EnsureInventory(player, controllerState);

            // Agent VO initialization requires a fresh item view to avoid stale definition data.
            if (AgentLoadoutOverride.TryResolve(team, slot, out var agentOverride)
                && agentOverride.Player.Handle == player.Handle)
            {
                uint accountId = originalReturn != nint.Zero
                    ? new CEconItemView(originalReturn).AccountID
                    : player.SteamID != 0
                        ? new CSteamID(player.SteamID).GetAccountID().m_AccountID
                        : 0;

                nint agentView = controllerState.GetAgentEconItemView(
                    team,
                    agentOverride.DefIndex,
                    accountId
                );
                ushort returnedDef = new CEconItemView(agentView).ItemDefinitionIndex;
                agentOverride.RecordConsumption(returnedDef, usedFreshAgentView: true);
                hook.SetReturn(agentView);
                return HookResult.Changed;
            }

            var item = controllerState.Inventory?.GetItemForSlot(
                (byte)team,
                (loadout_slot_t)slot,
                fallbackDefIndex,
                ConVars.IsFallbackTeam.Value,
                ConVars.MinModels.Value
            );

            if (item == null)
                return HookResult.Continue;

            hook.SetReturn(
                controllerState.GetEconItemView(
                    team,
                    slot,
                    item,
                    originalReturn,
                    isLoadoutView: true
                )
            );
            return HookResult.Changed;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[BotInventory] GetItemInLoadout post hook failed: {ex.Message}");
            return HookResult.Continue;
        }
    }

    private static void EnsureInventory(
        CCSPlayerController controller,
        CCSPlayerControllerState controllerState
    )
    {
        if (controllerState.Inventory != null)
            return;

        if (Inventories.TryGet(controller.SteamID, out var inventory, controller))
        {
            inventory.InitializeWearOverrides();
            controllerState.Inventory = inventory;
        }
    }

    private static CCSPlayerController? FindControllerByInventoryHandle(nint inventoryHandle)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsHLTV)
                continue;

            var inventoryServices = player.InventoryServices;
            if (inventoryServices == null || inventoryServices.Handle == nint.Zero)
                continue;

            // InventoryServices embeds CCSPlayerInventory at the gamedata offset used here.
            if (inventoryServices.GetInventory().Handle == inventoryHandle)
                return player;
        }

        return null;
    }
}
