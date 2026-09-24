/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace InventorySimulator;

public class CCSPlayerControllerState(
    ulong steamId,
    ulong ownerKey,
    nint controllerHandle,
    PlayerInventory? inventory
)
{
    public ulong SteamID = steamId;
    public ulong OwnerKey = ownerKey;
    public nint ControllerHandle = controllerHandle;
    public bool IsFetching = false;
    public bool IsAuthenticating = false;
    public bool IsLoadedFromFile = false;
    public long WsUpdatedAt = 0;
    public long SprayUsedAt = 0;
    public PlayerInventory? Inventory = inventory;
    public Timer? UseCmdTimer;
    public bool IsUseCmdBlocked = false;
    public Action? PostFetchCallback;

    private static readonly ConcurrentDictionary<
        (ulong OwnerKey, int Team, int Slot, ushort DefIndex, bool IsLoadoutView),
        nint
    > _econItemViewManager = [];

    // Agent views are fresh to prevent cached definition/model data leaking between agents.
    private static readonly ConcurrentDictionary<
        (ulong OwnerKey, int Team, ushort DefIndex),
        nint
    > _agentItemViewManager = [];

    public void TriggerPostFetch()
    {
        if (PostFetchCallback != null)
        {
            PostFetchCallback();
            PostFetchCallback = null;
        }
    }

    public void DisposeUseCmdTimer()
    {
        UseCmdTimer?.Kill();
        UseCmdTimer = null;
    }

    public nint GetEconItemView(
        int team,
        int slot,
        InventoryItem item,
        nint copyFrom = 0,
        bool isLoadoutView = false
    )
    {
        ushort defIndex = item.Def ?? 0;
        var key = (OwnerKey, team, slot, defIndex, isLoadoutView);
        ulong? accountSteamId = SteamID != 0 ? SteamID : null;

        if (_econItemViewManager.TryGetValue(key, out var ptr))
        {
            var existingItemView = new CEconItemView(ptr);
            existingItemView.ApplyAttributes(
                item,
                (loadout_slot_t)slot,
                accountSteamId,
                assignNewItemId: false
            );
            return ptr;
        }

        // Clone the complete game-owned view so loadout/MVP static fields stay intact.
        var itemView = SchemaHelper.CreateCEconItemView(copyFrom);
        itemView.ApplyAttributes(
            item,
            (loadout_slot_t)slot,
            accountSteamId,
            assignNewItemId: true
        );
        _econItemViewManager[key] = itemView.Handle;
        return itemView.Handle;
    }

    public nint GetAgentEconItemView(int team, ushort defIndex, uint fallbackAccountId)
    {
        var key = (OwnerKey, team, defIndex);
        if (_agentItemViewManager.TryGetValue(key, out var ptr))
        {
            var existing = new CEconItemView(ptr);
            existing.ApplyAgentIdentity(defIndex, fallbackAccountId, assignNewItemId: false);
            return ptr;
        }

        var itemView = SchemaHelper.CreateCEconItemView();
        itemView.ApplyAgentIdentity(defIndex, fallbackAccountId, assignNewItemId: true);
        _agentItemViewManager[key] = itemView.Handle;
        return itemView.Handle;
    }

    public void ClearEconItemView()
    {
        foreach (var key in _econItemViewManager.Keys)
        {
            if (key.OwnerKey != OwnerKey)
                continue;

            if (_econItemViewManager.TryRemove(key, out var ptr) && ptr != nint.Zero)
                Marshal.FreeHGlobal(ptr);
        }

        foreach (var key in _agentItemViewManager.Keys)
        {
            if (key.OwnerKey != OwnerKey)
                continue;

            if (_agentItemViewManager.TryRemove(key, out var ptr) && ptr != nint.Zero)
                Marshal.FreeHGlobal(ptr);
        }
    }

    public static void ClearAllEconItemView()
    {
        foreach (var key in _econItemViewManager.Keys)
        {
            if (_econItemViewManager.TryRemove(key, out var ptr) && ptr != nint.Zero)
                Marshal.FreeHGlobal(ptr);
        }

        foreach (var key in _agentItemViewManager.Keys)
        {
            if (_agentItemViewManager.TryRemove(key, out var ptr) && ptr != nint.Zero)
                Marshal.FreeHGlobal(ptr);
        }
    }
}
