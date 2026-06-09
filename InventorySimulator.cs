/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using BotHiderApi;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0";
    private static readonly PluginCapability<IBotHiderApi> _botHiderCapability =
        new("bothider:api");
    private IBotHiderApi? _botHiderApi;
    private bool IsBotPlayer(CCSPlayerController? c)
        => c != null && (c.IsBot || (_botHiderApi?.IsManagedBot(c.Slot) ?? false));


    public override void Load(bool hotReload)
    {
        CSS.Initialize(this);
        ConVars.Initialize(this);
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        RegisterListener<Listeners.OnEntityDeleted>(OnEntityDeleted);
        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect, HookMode.Post);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre);
        RegisterEventHandler<EventRoundMvp>(OnRoundMvpPre);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Post);
        Natives.CCSPlayerController_ProcessUsercmds.Hook(OnProcessUsercmds, HookMode.Post);
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPre, HookMode.Pre);
        Natives.CCSPlayerInventory_GetItemInLoadout.Hook(GetItemInLoadout, HookMode.Post);
        ConVars.File.ValueChanged += OnFileChanged;
        ConVars.IsRequireInventory.ValueChanged += OnIsRequireInventoryChanged;
        OnFileChanged(null, ConVars.File.Value);
        OnIsRequireInventoryChanged(null, ConVars.IsRequireInventory.Value);
    }
    

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            
            try
            {
                
                _botHiderApi = _botHiderCapability.Get();

           
            Inventories.ResolveBotHiderApi();
            }
            catch
            {
                _botHiderApi = null;
            }
        }

    public void OnFileChanged(object? _, string value)
    {
        if (Inventories.Load(value))
        {
            foreach (var player in Utilities.GetPlayers())
            {
                
                if (IsBotPlayer(player))  // It dosn't work while I m setting a logic to recognize the true player
                {
                    
                    if (Inventories.TryGet(player.SteamID, out var inventory, player))
                    {
                        player.GetState().Inventory = inventory;
                    }
                }
                else  
                {
                    
                    player.GetState().Inventory = null;
                }
            }
        }
    }

    public void OnIsRequireInventoryChanged(object? _, bool value)
    {
        if (ConVars.IsRequireInventory.Value)
            Natives.CServerSideClientBase_ActivatePlayer.Hook(OnActivatePlayerPre, HookMode.Pre);
        else
            Natives.CServerSideClientBase_ActivatePlayer.Unhook(OnActivatePlayerPre, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        CCSPlayerControllerState.ClearAllEconItemView();
    }
}
