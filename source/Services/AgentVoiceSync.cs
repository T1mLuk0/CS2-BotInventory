using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

internal static class AgentVoiceSync
{
    // Agents with an explicit vo_prefix need a clean paid-agent loadout item so their model and
    // dedicated voice bank are initialized together. Agents without vo_prefix remain visual-only
    // and keep the map/BOT class response services created by the game.
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
                $"[BotInventory] Agent voice reinitialization failed at {stage} "
                + $"for slot={player.Slot}, DefIndex={defIndex}: {ex}"
            );
            return false;
        }
    }

    // Preserve explicit paid-agent scalar identity only while the BOT itself controls the pawn.
    // Human takeover is intentionally ignored: missing friendly-fire responses after takeover are
    // a game behavior and rebuilding response services here can reintroduce model/voice mismatch.
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
            || !agent.HasExplicitVoicePrefix
            || agent.TeamNum != player.TeamNum)
            return;

        // During takeover the BOT and human can reference the same pawn. Do not write any character
        // or response-related fields until the game returns control to the BOT.
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
        // No vo_prefix means this item intentionally reuses the map/BOT class voice. Change only
        // the visible model and leave character definitions and response services untouched.
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
        if (!agent.HasExplicitVoicePrefix)
            throw new InvalidOperationException(
                $"Inherited-class agent DefIndex={agent.DefIndex} reached explicit loadout initialization."
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

        // A direct model assignment is a visual fallback only. The requested scalar identity is
        // retained, but no extra class-model rebuild is performed because that can select wrong VO.
        if (!coherentAgentView)
        {
            pawn.SetModel(agent.Model);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        }

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
            : engineFemale;

        if (string.IsNullOrWhiteSpace(finalPrefix))
            return;

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

    private static void ApplyControllerDefinitions(
        CCSPlayerController controller,
        CCSPlayerPawn pawn,
        ushort defIndex
    )
    {
        ApplyControllerDefinition(controller, defIndex);

        // The original BOT controller can remain attached to the pawn during normal lifecycle
        // transitions. Keep its mirrored character index aligned while the BOT owns the pawn.
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
            // The value is also consumed server-side.
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
            // Voice selection is consumed server-side as well.
        }
    }
}
