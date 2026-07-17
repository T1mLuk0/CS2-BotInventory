using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

/// <summary>
/// Supplies one synchronous custom-player loadout item while the engine initializes a BOT agent.
/// </summary>
internal static class AgentLoadoutOverride
{
    [ThreadStatic]
    private static Scope? _current;

    internal sealed class Scope : IDisposable
    {
        private readonly Scope? _previous;
        private bool _disposed;

        internal CCSPlayerController Player { get; }
        internal ushort DefIndex { get; }
        internal bool WasConsumed { get; private set; }
        internal ushort ReturnedDefIndex { get; private set; }
        internal bool UsedFreshAgentView { get; private set; }

        internal Scope(CCSPlayerController player, ushort defIndex, Scope? previous)
        {
            Player = player;
            DefIndex = defIndex;
            _previous = previous;
        }

        internal void RecordConsumption(ushort returnedDefIndex, bool usedFreshAgentView)
        {
            WasConsumed = true;
            ReturnedDefIndex = returnedDefIndex;
            UsedFreshAgentView = usedFreshAgentView;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _current = _previous;
        }
    }

    public static Scope Begin(CCSPlayerController player, ushort defIndex)
    {
        var scope = new Scope(player, defIndex, _current);
        _current = scope;
        return scope;
    }

    public static bool TryResolve(int team, int slot, out Scope scope)
    {
        var current = _current;
        if (
            current == null
            || !current.Player.IsValid
            || slot != (int)loadout_slot_t.LOADOUT_SLOT_CLOTHING_CUSTOMPLAYER
            || !AgentCatalog.IsValidForTeam(current.DefIndex, (byte)team)
        )
        {
            scope = null!;
            return false;
        }

        scope = current;
        return true;
    }
}
