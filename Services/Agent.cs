using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

internal static class AgentCatalog
{
    internal readonly struct Definition
    {
        public ushort DefIndex { get; }
        public byte TeamNum { get; }
        public string Name { get; }
        public string Model { get; }
        public string VoicePrefix { get; }
        public bool HasFemaleVoice { get; }
        public bool HasExplicitVoicePrefix { get; }

        public bool IsDefaultMapCharacter => DefIndex is 5036 or 5037;

        // Female agents still need gender identity when items_game has no explicit voice prefix.
        public bool RequiresDedicatedVoiceIdentity => HasExplicitVoicePrefix || HasFemaleVoice;
        public bool UsesInheritedClassVoice => !RequiresDedicatedVoiceIdentity;

        // Agents without explicit vo_prefix keep the engine-selected class voice.
        public string EffectiveVoicePrefix => VoicePrefix;

        public Definition(
            ushort defIndex,
            byte teamNum,
            string name,
            string model,
            string voicePrefix,
            bool hasFemaleVoice
        )
        {
            DefIndex = defIndex;
            TeamNum = teamNum;
            Name = name;
            Model = model;
            HasExplicitVoicePrefix = !string.IsNullOrWhiteSpace(voicePrefix);
            VoicePrefix = AgentCatalog.NormalizeExplicitVoicePrefix(voicePrefix);
            HasFemaleVoice = AgentCatalog.IsFemaleVoicePrefix(VoicePrefix, hasFemaleVoice);
        }
    }

    private static readonly Dictionary<ushort, Definition> Definitions = new()
    {
        [5036] = new(5036, 2, "Standard map-based T", "agents/models/tm_phoenix/tm_phoenix.vmdl", "", false),
        [5037] = new(5037, 3, "Standard map-based CT", "agents/models/ctm_sas/ctm_sas.vmdl", "", false),
        [4613] = new(4613, 2, "Bloody Darryl The Strapped", "agents/models/tm_professional/tm_professional_varf5.vmdl", "professional_epic", false),
        [4619] = new(4619, 3, "'Blueberries' Buckshot", "agents/models/ctm_st6/ctm_st6_variantj.vmdl", "", false),
        [4680] = new(4680, 3, "'Two Times' McCoy — USAF TACP", "agents/models/ctm_st6/ctm_st6_variantl.vmdl", "", false),
        [4711] = new(4711, 3, "Cmdr. Mae 'Dead Cold' Jamison", "agents/models/ctm_swat/ctm_swat_variante.vmdl", "swat_epic", true),
        [4712] = new(4712, 3, "1st Lieutenant Farlow", "agents/models/ctm_swat/ctm_swat_variantf.vmdl", "swat_fem", true),
        [4713] = new(4713, 3, "John 'Van Healen' Kask", "agents/models/ctm_swat/ctm_swat_variantg.vmdl", "", false),
        [4714] = new(4714, 3, "Bio-Haz Specialist", "agents/models/ctm_swat/ctm_swat_varianth.vmdl", "", false),
        [4715] = new(4715, 3, "Sergeant Bombson", "agents/models/ctm_swat/ctm_swat_varianti.vmdl", "", false),
        [4716] = new(4716, 3, "Chem-Haz Specialist", "agents/models/ctm_swat/ctm_swat_variantj.vmdl", "", false),
        [4718] = new(4718, 2, "Rezan the Redshirt", "agents/models/tm_balkan/tm_balkan_variantk.vmdl", "", false),
        [4726] = new(4726, 2, "Sir Bloody Miami Darryl", "agents/models/tm_professional/tm_professional_varf.vmdl", "professional_epic", false),
        [4727] = new(4727, 2, "Safecracker Voltzmann", "agents/models/tm_professional/tm_professional_varg.vmdl", "professional_fem", true),
        [4728] = new(4728, 2, "Little Kev", "agents/models/tm_professional/tm_professional_varh.vmdl", "", false),
        [4730] = new(4730, 2, "Getaway Sally", "agents/models/tm_professional/tm_professional_varj.vmdl", "professional_fem", true),
        [4732] = new(4732, 2, "Number K", "agents/models/tm_professional/tm_professional_vari.vmdl", "", false),
        [4733] = new(4733, 2, "Sir Bloody Silent Darryl", "agents/models/tm_professional/tm_professional_varf1.vmdl", "professional_epic", false),
        [4734] = new(4734, 2, "Sir Bloody Skullhead Darryl", "agents/models/tm_professional/tm_professional_varf2.vmdl", "professional_epic", false),
        [4735] = new(4735, 2, "Sir Bloody Darryl Royale", "agents/models/tm_professional/tm_professional_varf3.vmdl", "professional_epic", false),
        [4736] = new(4736, 2, "Sir Bloody Loudmouth Darryl", "agents/models/tm_professional/tm_professional_varf4.vmdl", "professional_epic", false),
        [4749] = new(4749, 3, "Sous-Lieutenant Medic", "agents/models/ctm_gendarmerie/ctm_gendarmerie_varianta.vmdl", "gendarmerie_male", false),
        [4750] = new(4750, 3, "Chem-Haz Capitaine", "agents/models/ctm_gendarmerie/ctm_gendarmerie_variantb.vmdl", "gendarmerie_male", false),
        [4751] = new(4751, 3, "Chef d'Escadron Rouchard", "agents/models/ctm_gendarmerie/ctm_gendarmerie_variantc.vmdl", "gendarmerie_fem_epic", true),
        [4752] = new(4752, 3, "Aspirant", "agents/models/ctm_gendarmerie/ctm_gendarmerie_variantd.vmdl", "gendarmerie_male", false),
        [4753] = new(4753, 3, "Officer Jacques Beltram", "agents/models/ctm_gendarmerie/ctm_gendarmerie_variante.vmdl", "gendarmerie_male", false),
        [4756] = new(4756, 3, "Lieutenant 'Tree Hugger' Farlow", "agents/models/ctm_swat/ctm_swat_variantk.vmdl", "swat_fem", true),
        [4757] = new(4757, 3, "Cmdr. Davida 'Goggles' Fernandez", "agents/models/ctm_diver/ctm_diver_varianta.vmdl", "seal_fem", true),
        [4771] = new(4771, 3, "Cmdr. Frank 'Wet Sox' Baroud", "agents/models/ctm_diver/ctm_diver_variantb.vmdl", "seal_diver_01", false),
        [4772] = new(4772, 3, "Lieutenant Rex Krikey", "agents/models/ctm_diver/ctm_diver_variantc.vmdl", "seal_diver_02", false),
        [4773] = new(4773, 2, "Elite Trapper Solman", "agents/models/tm_jungle_raider/tm_jungle_raider_varianta.vmdl", "jungle_male", false),
        [4774] = new(4774, 2, "Crasswater The Forgotten", "agents/models/tm_jungle_raider/tm_jungle_raider_variantb.vmdl", "jungle_male_epic", false),
        [4775] = new(4775, 2, "Arno The Overgrown", "agents/models/tm_jungle_raider/tm_jungle_raider_variantc.vmdl", "jungle_male", false),
        [4776] = new(4776, 2, "Col. Mangos Dabisi", "agents/models/tm_jungle_raider/tm_jungle_raider_variantd.vmdl", "jungle_male", false),
        [4777] = new(4777, 2, "Vypa Sista of the Revolution", "agents/models/tm_jungle_raider/tm_jungle_raider_variante.vmdl", "jungle_fem_epic", true),
        [4778] = new(4778, 2, "Trapper Aggressor", "agents/models/tm_jungle_raider/tm_jungle_raider_variantf.vmdl", "jungle_fem", true),
        [4780] = new(4780, 2, "'Medium Rare' Crasswater", "agents/models/tm_jungle_raider/tm_jungle_raider_variantb2.vmdl", "jungle_male_epic", false),
        [4781] = new(4781, 2, "Trapper", "agents/models/tm_jungle_raider/tm_jungle_raider_variantf2.vmdl", "jungle_fem", true),
        [5105] = new(5105, 2, "Ground Rebel", "agents/models/tm_leet/tm_leet_variantg.vmdl", "", false),
        [5106] = new(5106, 2, "Osiris", "agents/models/tm_leet/tm_leet_varianth.vmdl", "", false),
        [5107] = new(5107, 2, "Prof. Shahmat", "agents/models/tm_leet/tm_leet_varianti.vmdl", "", false),
        [5108] = new(5108, 2, "The Elite Mr. Muhlik", "agents/models/tm_leet/tm_leet_variantf.vmdl", "leet_epic", false),
        [5109] = new(5109, 2, "Jungle Rebel", "agents/models/tm_leet/tm_leet_variantj.vmdl", "", false),
        [5205] = new(5205, 2, "Soldier", "agents/models/tm_phoenix/tm_phoenix_varianth.vmdl", "", false),
        [5206] = new(5206, 2, "Enforcer", "agents/models/tm_phoenix/tm_phoenix_variantf.vmdl", "", false),
        [5207] = new(5207, 2, "Slingshot", "agents/models/tm_phoenix/tm_phoenix_variantg.vmdl", "", false),
        [5208] = new(5208, 2, "Street Soldier", "agents/models/tm_phoenix/tm_phoenix_varianti.vmdl", "", false),
        [5305] = new(5305, 3, "Operator", "agents/models/ctm_fbi/ctm_fbi_variantf.vmdl", "", false),
        [5306] = new(5306, 3, "Markus Delrow", "agents/models/ctm_fbi/ctm_fbi_variantg.vmdl", "", false),
        [5307] = new(5307, 3, "Michael Syfers", "agents/models/ctm_fbi/ctm_fbi_varianth.vmdl", "", false),
        [5308] = new(5308, 3, "Special Agent Ava", "agents/models/ctm_fbi/ctm_fbi_variantb.vmdl", "fbihrt_epic", true),
        [5400] = new(5400, 3, "3rd Commando Company", "agents/models/ctm_st6/ctm_st6_variantk.vmdl", "gsg9", false),
        [5401] = new(5401, 3, "Seal Team 6 Soldier", "agents/models/ctm_st6/ctm_st6_variante.vmdl", "", false),
        [5402] = new(5402, 3, "Buckshot", "agents/models/ctm_st6/ctm_st6_variantg.vmdl", "", false),
        [5403] = new(5403, 3, "'Two Times' McCoy — TACP Cavalry", "agents/models/ctm_st6/ctm_st6_variantm.vmdl", "", false),
        [5404] = new(5404, 3, "Lt. Commander Ricksaw", "agents/models/ctm_st6/ctm_st6_varianti.vmdl", "seal_epic", false),
        [5405] = new(5405, 3, "Primeiro Tenente", "agents/models/ctm_st6/ctm_st6_variantn.vmdl", "", false),
        [5500] = new(5500, 2, "Dragomir", "agents/models/tm_balkan/tm_balkan_variantf.vmdl", "", false),
        [5501] = new(5501, 2, "Maximus", "agents/models/tm_balkan/tm_balkan_varianti.vmdl", "", false),
        [5502] = new(5502, 2, "Rezan The Ready", "agents/models/tm_balkan/tm_balkan_variantg.vmdl", "", false),
        [5503] = new(5503, 2, "Blackwolf", "agents/models/tm_balkan/tm_balkan_variantj.vmdl", "", false),
        [5504] = new(5504, 2, "'The Doctor' Romanov", "agents/models/tm_balkan/tm_balkan_varianth.vmdl", "balkan_epic", false),
        [5505] = new(5505, 2, "Dragomir — Sabre Footsoldier", "agents/models/tm_balkan/tm_balkan_variantl.vmdl", "", false),
        [5601] = new(5601, 3, "B Squadron Officer", "agents/models/ctm_sas/ctm_sas_variantf.vmdl", "", false),
        [5602] = new(5602, 3, "D Squadron Officer", "agents/models/ctm_sas/ctm_sas_variantg.vmdl", "", false),
    };

    public static bool TryGet(ushort defIndex, out Definition definition)
        => Definitions.TryGetValue(defIndex, out definition);

    public static bool IsValidForTeam(ushort defIndex, byte teamNum)
        => Definitions.TryGetValue(defIndex, out var definition)
            && definition.TeamNum == teamNum;

    public static IEnumerable<string> GetAllModels()
        => Definitions.Values
            .Where(definition => !definition.IsDefaultMapCharacter)
            .Select(definition => definition.Model)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeExplicitVoicePrefix(string explicitPrefix)
    {
        string normalizedPrefix = explicitPrefix.Trim().ToLowerInvariant();

        // items_game names this identity ctm_gsg9, while sound-event definitions use gsg9.*.
        return normalizedPrefix == "ctm_gsg9" ? "gsg9" : normalizedPrefix;
    }

    private static bool IsFemaleVoicePrefix(string voicePrefix, bool fallbackHint)
    {
        return voicePrefix switch
        {
            "fbihrt_epic" => true,
            "swat_epic" => true,
            "swat_fem" => true,
            "gendarmerie_fem" => true,
            "gendarmerie_fem_epic" => true,
            "seal_fem" => true,
            "professional_fem" => true,
            "jungle_fem" => true,
            "jungle_fem_epic" => true,
            "" => fallbackHint,
            _ => false,
        };
    }

}

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

internal static class AgentVoiceSync
{
    // Explicit-voice agents need a fresh loadout view; inherited-voice agents stay visual-only.
    private sealed class TrackedPawnAgent
    {
        public ushort DefIndex { get; set; }
        public string VoicePrefix { get; set; } = string.Empty;
        public bool HasFemaleVoice { get; set; }
        public bool HasVoiceSnapshot { get; set; }
    }

    private static readonly Dictionary<nint, TrackedPawnAgent> PawnAgents = new();

    public static bool ApplyModelAndVoice(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        ushort defIndex,
        string stage
    )
    {
        if (!TryValidate(player, defIndex, out var agent))
            return false;

        try
        {
            if (agent.IsDefaultMapCharacter)
            {
                ForgetPawn(pawn);
                pawn.SetModelFromClass();
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                return true;
            }

            if (agent.UsesInheritedClassVoice)
            {
                ForgetPawn(pawn);
                ApplyInheritedClassVoiceModel(pawn, agent);
                return true;
            }

            var tracked = RememberPawn(pawn, agent.DefIndex);
            InitializeFromCharacterDefinition(player, pawn, agent, tracked);
            return true;
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError(
                $"[BotInventory] Agent model/voice initialization failed at {stage} "
                + $"for slot={player.Slot}, DefIndex={defIndex}: {ex}"
            );
            return false;
        }
    }

    public static bool ApplyVoiceOnly(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        ushort defIndex,
        string stage
    )
    {
        if (!TryValidate(player, defIndex, out var agent))
            return false;

        try
        {
            if (agent.IsDefaultMapCharacter)
            {
                ForgetPawn(pawn);
                return true;
            }

            // Inherited-voice agents keep the voice graph created by the normal spawn path.
            if (agent.UsesInheritedClassVoice)
            {
                ForgetPawn(pawn);
                return true;
            }

            // Explicit-voice agents enter SetModelFromLoadout once; later passes restore only scalars.
            var tracked = RememberPawn(pawn, agent.DefIndex);
            ApplyControllerDefinitions(player, pawn, agent.DefIndex);
            ApplyPawnDefinition(pawn, agent.DefIndex);

            string prefix = tracked.HasVoiceSnapshot
                ? tracked.VoicePrefix
                : agent.EffectiveVoicePrefix;
            bool female = tracked.HasVoiceSnapshot
                ? tracked.HasFemaleVoice
                : agent.HasFemaleVoice;

            if (!string.IsNullOrWhiteSpace(prefix) || female)
            {
                WriteVoiceIdentity(pawn, prefix, female);
                tracked.VoicePrefix = prefix;
                tracked.HasFemaleVoice = female;
                tracked.HasVoiceSnapshot = true;
            }

            return true;
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError(
                $"[BotInventory] Agent voice synchronization failed at {stage} "
                + $"for slot={player.Slot}, DefIndex={defIndex}: {ex}"
            );
            return false;
        }
    }

    public static bool ApplyModelOnly(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        ushort defIndex,
        string stage
    )
    {
        if (!TryValidate(player, defIndex, out var agent))
            return false;

        try
        {
            if (agent.IsDefaultMapCharacter)
            {
                pawn.SetModelFromClass();
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                return true;
            }

            ApplyDirectAgentModel(pawn, agent);
            return true;
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError(
                $"[BotInventory] Agent model synchronization failed at {stage} "
                + $"for slot={player.Slot}, DefIndex={defIndex}: {ex}"
            );
            return false;
        }
    }

    // Do not rewrite agent identity while a human has taken over the BOT pawn.
    public static void ReconcilePlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsHLTV || !player.IsBot)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        if (!PawnAgents.TryGetValue(pawn.Handle, out var tracked))
            return;

        if (!AgentCatalog.TryGet(tracked.DefIndex, out var agent)
            || agent.IsDefaultMapCharacter
            || !agent.RequiresDedicatedVoiceIdentity
            || agent.TeamNum != player.TeamNum)
            return;

        // BOT and human may share the pawn during takeover; wait until BOT control returns.
        if (TryFindHumanTakeoverController(pawn.Handle, out _))
            return;

        RestoreTrackedIdentity(player, pawn, tracked);
    }

    public static void StopTrackingPawn(CCSPlayerPawn pawn)
    {
        ForgetPawn(pawn);
    }

    public static void Clear()
    {
        PawnAgents.Clear();
    }

    // Compatibility alias for existing call sites.
    public static void Apply(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        ushort defIndex,
        string stage
    )
    {
        ApplyVoiceOnly(player, pawn, defIndex, stage);
    }

    private static bool TryValidate(
        CCSPlayerController player,
        ushort defIndex,
        out AgentCatalog.Definition agent
    )
    {
        if (!AgentCatalog.TryGet(defIndex, out agent))
        {
            CSS.Plugin.Logger.LogWarning(
                $"[BotInventory] Unknown agent DefIndex={defIndex}; custom agent was skipped."
            );
            return false;
        }

        if (agent.TeamNum != player.TeamNum)
        {
            CSS.Plugin.Logger.LogError(
                $"[BotInventory] Refused cross-team agent DefIndex={defIndex} ({agent.Name}): "
                + $"agent team={agent.TeamNum}, player team={player.TeamNum}."
            );
            return false;
        }

        return true;
    }

    private static void ApplyInheritedClassVoiceModel(
        CCSPlayerPawn pawn,
        AgentCatalog.Definition agent
    )
    {
        // No vo_prefix: change only the model and preserve the map/BOT voice identity.
        ApplyDirectAgentModel(pawn, agent);
    }

    private static void ApplyDirectAgentModel(
        CCSPlayerPawn pawn,
        AgentCatalog.Definition agent
    )
    {
        pawn.SetModel(agent.Model);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
    }

    private static void InitializeFromCharacterDefinition(
        CCSPlayerController player,
        CCSPlayerPawn pawn,
        AgentCatalog.Definition agent,
        TrackedPawnAgent tracked
    )
    {
        if (!agent.RequiresDedicatedVoiceIdentity)
            throw new InvalidOperationException(
                $"Inherited-class agent DefIndex={agent.DefIndex} reached dedicated voice initialization."
            );

        ApplyControllerDefinitions(player, pawn, agent.DefIndex);
        ApplyPawnDefinition(pawn, agent.DefIndex);
        WriteVoiceIdentity(pawn, agent.EffectiveVoicePrefix, agent.HasFemaleVoice);

        bool coherentAgentView;
        using (var loadoutOverride = AgentLoadoutOverride.Begin(player, agent.DefIndex))
        {
            pawn.SetModelFromLoadout();
            coherentAgentView = loadoutOverride.WasConsumed
                && loadoutOverride.UsedFreshAgentView
                && loadoutOverride.ReturnedDefIndex == agent.DefIndex;
        }

        var nativeVoice = ReadEngineVoiceIdentity(pawn);

        if (!coherentAgentView)
        {
            CSS.Plugin.Logger.LogWarning(
                $"[BotInventory] Agent loadout initialization was not coherent for "
                + $"slot={player.Slot}, DefIndex={agent.DefIndex} ({agent.Name}); "
                + "forcing the configured agent model while preserving explicit VO identity."
            );
        }

        // SetModelFromLoadout initializes agent response services; the visible model is applied directly.
        ApplyDirectAgentModel(pawn, agent);

        ApplyControllerDefinitions(player, pawn, agent.DefIndex);
        ApplyPawnDefinition(pawn, agent.DefIndex);
        CaptureAndNormalizeVoiceIdentity(
            pawn,
            tracked,
            agent,
            nativeVoice.Prefix,
            nativeVoice.Female
        );
    }

    private static (string Prefix, bool Female) ReadEngineVoiceIdentity(CCSPlayerPawn pawn)
    {
        string prefix = Schema.GetString(
            pawn.Handle,
            "CCSPlayerPawn",
            "m_strVOPrefix"
        );
        bool female = Schema.GetRef<bool>(
            pawn.Handle,
            "CCSPlayerPawn",
            "m_bHasFemaleVoice"
        );
        return (prefix, female);
    }

    private static void CaptureAndNormalizeVoiceIdentity(
        CCSPlayerPawn pawn,
        TrackedPawnAgent tracked,
        AgentCatalog.Definition agent,
        string enginePrefix,
        bool engineFemale
    )
    {
        string finalPrefix = !string.IsNullOrWhiteSpace(agent.EffectiveVoicePrefix)
            ? agent.EffectiveVoicePrefix
            : enginePrefix;
        bool finalFemale = !string.IsNullOrWhiteSpace(agent.EffectiveVoicePrefix)
            ? agent.HasFemaleVoice
            : agent.HasFemaleVoice || engineFemale;

        // Empty voice prefixes still synchronize gender.
        WriteVoiceIdentity(pawn, finalPrefix, finalFemale);
        tracked.VoicePrefix = finalPrefix;
        tracked.HasFemaleVoice = finalFemale;
        tracked.HasVoiceSnapshot = true;
    }

    private static void RestoreTrackedIdentity(
        CCSPlayerController controller,
        CCSPlayerPawn pawn,
        TrackedPawnAgent tracked
    )
    {
        ApplyControllerDefinitions(controller, pawn, tracked.DefIndex);
        ApplyPawnDefinition(pawn, tracked.DefIndex);

        if (tracked.HasVoiceSnapshot)
            WriteVoiceIdentity(pawn, tracked.VoicePrefix, tracked.HasFemaleVoice);
    }

    private static void WriteVoiceIdentity(
        CCSPlayerPawn pawn,
        string prefix,
        bool hasFemaleVoice
    )
    {
        ref bool femaleVoice = ref Schema.GetRef<bool>(
            pawn.Handle,
            "CCSPlayerPawn",
            "m_bHasFemaleVoice"
        );
        if (femaleVoice != hasFemaleVoice)
        {
            femaleVoice = hasFemaleVoice;
            MarkPawnSchemaFieldChanged(pawn, "m_bHasFemaleVoice");
        }

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            string currentPrefix = Schema.GetString(
                pawn.Handle,
                "CCSPlayerPawn",
                "m_strVOPrefix"
            );
            if (!string.Equals(currentPrefix, prefix, StringComparison.OrdinalIgnoreCase))
            {
                Schema.SetString(
                    pawn.Handle,
                    "CCSPlayerPawn",
                    "m_strVOPrefix",
                    prefix
                );
                MarkPawnSchemaFieldChanged(pawn, "m_strVOPrefix");
            }
        }
    }

    private static void ApplyControllerDefinitions(
        CCSPlayerController controller,
        CCSPlayerPawn pawn,
        ushort defIndex
    )
    {
        ApplyControllerDefinition(controller, defIndex);

        // Keep the controller character index aligned while the BOT owns the pawn.
        var originalController = pawn.OriginalController.Value;
        if (originalController != null
            && originalController.IsValid
            && originalController.Handle != controller.Handle)
        {
            ApplyControllerDefinition(originalController, defIndex);
        }
    }

    private static void ApplyControllerDefinition(
        CCSPlayerController player,
        ushort defIndex
    )
    {
        ref ushort controllerDef = ref Schema.GetRef<ushort>(
            player.Handle,
            "CCSPlayerController",
            "m_nPawnCharacterDefIndex"
        );
        if (controllerDef == defIndex)
            return;

        controllerDef = defIndex;
        MarkControllerSchemaFieldChanged(player, "m_nPawnCharacterDefIndex");
    }

    private static void ApplyPawnDefinition(CCSPlayerPawn pawn, ushort defIndex)
    {
        ref ushort pawnDef = ref Schema.GetRef<ushort>(
            pawn.Handle,
            "CCSPlayerPawn",
            "m_nCharacterDefIndex"
        );
        if (pawnDef == defIndex)
            return;

        pawnDef = defIndex;
        MarkPawnSchemaFieldChanged(pawn, "m_nCharacterDefIndex");
    }

    private static bool IsControllingBot(CCSPlayerController player)
    {
        try
        {
            return player.ControllingBot;
        }
        catch
        {
            try
            {
                return Schema.GetRef<bool>(
                    player.Handle,
                    "CCSPlayerController",
                    "m_bControllingBot"
                );
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool TryFindHumanTakeoverController(
        nint pawnHandle,
        out CCSPlayerController controller
    )
    {
        foreach (var candidate in Utilities.GetPlayers())
        {
            if (candidate == null
                || !candidate.IsValid
                || candidate.IsHLTV
                || candidate.IsBot
                || !IsControllingBot(candidate))
                continue;

            var candidatePawn = candidate.PlayerPawn.Value;
            if (candidatePawn != null
                && candidatePawn.IsValid
                && candidatePawn.Handle == pawnHandle)
            {
                controller = candidate;
                return true;
            }
        }

        controller = null!;
        return false;
    }

    private static TrackedPawnAgent RememberPawn(CCSPlayerPawn pawn, ushort defIndex)
    {
        if (pawn.Handle == nint.Zero)
            return new TrackedPawnAgent { DefIndex = defIndex };

        if (!PawnAgents.TryGetValue(pawn.Handle, out var tracked))
        {
            tracked = new TrackedPawnAgent();
            PawnAgents[pawn.Handle] = tracked;
        }

        if (tracked.DefIndex != defIndex)
        {
            tracked.DefIndex = defIndex;
            tracked.VoicePrefix = string.Empty;
            tracked.HasFemaleVoice = false;
            tracked.HasVoiceSnapshot = false;
        }

        return tracked;
    }

    private static void ForgetPawn(CCSPlayerPawn pawn)
    {
        if (pawn.Handle != nint.Zero)
            PawnAgents.Remove(pawn.Handle);
    }

    private static void MarkControllerSchemaFieldChanged(
        CCSPlayerController player,
        string fieldName
    )
    {
        try
        {
            if (Schema.IsSchemaFieldNetworked("CCSPlayerController", fieldName))
                Utilities.SetStateChanged(player, "CCSPlayerController", fieldName);
        }
        catch
        {
        }
    }

    private static void MarkPawnSchemaFieldChanged(CCSPlayerPawn pawn, string fieldName)
    {
        try
        {
            if (Schema.IsSchemaFieldNetworked("CCSPlayerPawn", fieldName))
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", fieldName);
        }
        catch
        {
        }
    }
}
