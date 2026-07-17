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

            // During BOT character initialization, resolve the exact temporary agent item
            // without relying on SteamID, SOCache, or a discoverable inventory handle.
            if (AgentLoadoutOverride.TryResolve(team, slot, out var agentScope))
            {
                var overridePlayer = agentScope.Player;
                ushort overrideDefIndex = agentScope.DefIndex;
                // Critical voice-coherence fix: construct the custom-player view from zero instead
                // of cloning originalReturn.  The original BOT slot can contain a different diver,
                // generic faction model, or stale item-definition cache.  Manually changing only
                // ItemDefinitionIndex on that clone can make SetModelFromLoadout bind the old voice
                // while a later SetModel call displays the requested model.
                uint fallbackAccountId = overridePlayer.SteamID != 0
                    ? new CSteamID(overridePlayer.SteamID).GetAccountID().m_AccountID
                    : 0xB0700000u | (overridePlayer.Index & 0x0000FFFFu);
                var overrideState = overridePlayer.GetState();
                nint overrideItemViewPtr = overrideState.GetAgentEconItemView(
                    team,
                    overrideDefIndex,
                    fallbackAccountId
                );

                var overrideItemView = new CEconItemView(overrideItemViewPtr);
                ushort returnedDefIndex = overrideItemView.ItemDefinitionIndex;
                agentScope.RecordConsumption(
                    returnedDefIndex,
                    usedFreshAgentView: true
                );

                hook.SetReturn(overrideItemViewPtr);
                return HookResult.Changed;
            }

            if (inventoryHandle == nint.Zero)
                return HookResult.Continue;

            var player = FindControllerByInventoryHandle(inventoryHandle);
            if (player == null || !IsBotPlayer(player))
                return HookResult.Continue;

            // Do not inject the configured BOT agent through the ordinary loadout hook.  The game
            // must finish its normal map-class spawn first so Radio/DamageReact/Response services
            // have a valid baseline.  Explicit vo_prefix agents are consumed later through the
            // scoped AgentLoadoutOverride; inherited-voice agents are applied as visual-only models.
            if (player.IsBot
                && slot == (int)loadout_slot_t.LOADOUT_SLOT_CLOTHING_CUSTOMPLAYER
                && Inventories.AreBotAgentsEnabled()
                && ConVars.MinModels.Value <= 0)
            {
                return HookResult.Continue;
            }

            // A BOT without a GC/SOCache can legitimately have a null original loadout item.
            // Melee/gloves/agents/music do not need a fallback defindex, so create their view
            // directly from the generated inventory when possible.
            ushort fallbackDefIndex = 0;
            if (originalReturn != nint.Zero)
                fallbackDefIndex = new CEconItemView(originalReturn).ItemDefinitionIndex;

            var controllerState = player.GetState();
            EnsureInventory(player, controllerState);

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

            // CCSPlayerInventory is embedded in InventoryServices at the gamedata offset.
            // The resulting address is the same `this` pointer received by GetItemInLoadout.
            if (inventoryServices.GetInventory().Handle == inventoryHandle)
                return player;
        }

        return null;
    }
}
