/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public static class CEconItemViewExtensions
{
    public static readonly ulong MinimumCustomItemID = 65155030971;
    private static ulong NextItemId = MinimumCustomItemID;

    public static void ApplyAttributes(
        this CEconItemView self,
        InventoryItem item,
        loadout_slot_t? slot,
        ulong? steamId,
        bool assignNewItemId = true
    )
    {
        var isMelee = slot == loadout_slot_t.LOADOUT_SLOT_MELEE;

        // Set the definition before advertising the view as initialized.  Most views are copied
        // from a game-owned item, but freshly constructed agent views start at DefIndex 0.  Keeping
        // this order prevents any reader from observing an initialized view with stale static data.
        if (item.Def != null)
            self.ItemDefinitionIndex = item.Def.Value;

        AssignStableItemId(self, assignNewItemId);

        if (steamId != null)
            self.AccountID = new CSteamID(steamId.Value).GetAccountID().m_AccountID;
        if (isMelee)
            self.EntityQuality = 3;
        else
            self.EntityQuality = item.Stattrak >= 0 ? 9 : (item.Souvenir ? 12 : 4);
        if (item.Nametag != null)
            self.CustomName = item.Nametag;
        var customAttrs = item.GetAttributes();
        var attrs = self.NetworkedDynamicAttributes;
        attrs.Attributes.RemoveAll();
        foreach (var (attributeName, value) in customAttrs)
            attrs.SetOrAddAttributeValueByName(attributeName, value);

        self.Initialized = true;
    }

    /// <summary>
    /// Initializes a clean, non-cloned custom-player item view.  Agent character setup must not
    /// inherit the original BOT loadout view because CEconItemView::operator= can carry cached
    /// static-definition state from a different agent even after m_iItemDefinitionIndex is changed.
    /// </summary>
    public static void ApplyAgentIdentity(
        this CEconItemView self,
        ushort defIndex,
        uint accountId,
        bool assignNewItemId
    )
    {
        self.Initialized = false;
        self.ItemDefinitionIndex = defIndex;
        AssignStableItemId(self, assignNewItemId);
        self.AccountID = accountId;
        self.EntityQuality = 4;
        self.NetworkedDynamicAttributes.Attributes.RemoveAll();
        self.Initialized = true;
    }

    private static void AssignStableItemId(CEconItemView self, bool assignNewItemId)
    {
        // Keep a cached CEconItemView's identity stable.  The loadout/MVP code may query the same
        // view many times per frame; changing ItemID on every query prevents material caches from
        // settling even though the item definition still resolves.
        if (!assignNewItemId && self.ItemID >= MinimumCustomItemID)
            return;

        var itemId = NextItemId++;
        self.ItemID = itemId;
        self.ItemIDLow = (uint)(itemId & 0xFFFFFFFF);
        self.ItemIDHigh = (uint)(itemId >> 32);
    }
}
