using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;

namespace AgentVoiceFix;

[MinimumApiVersion(80)]
public sealed class AgentVoiceFixPlugin : BasePlugin
{
    private const string CatalogResourceName = "AgentVoiceFix.Resources.agentvoicefix-current-catalog.json";
    private const string GamedataFileName = "AgentVoiceFix.gamedata.json";
    private const int MaxSoundNameBytes = 192;
    private const int NativeFunctionProbeBytes = 512;
    private const int NativeFunctionCandidateSearchBytes = 192;
    private const int MinimumSoundEventOffset = 0x100;
    private const int MaximumSoundEventOffset = 0x4000;

    private static readonly Regex PlausibleEventRegex = new(
        @"^[A-Za-z0-9_\-.]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public override string ModuleName => "Agent Voice Fix";
    public override string ModuleVersion => "1.1.3";
    public override string ModuleAuthor => "Private build";
    public override string ModuleDescription =>
        "Repairs invalid custom-Agent VO events and derives the native sound-event pointer offset from the on-disk server.dll image.";

    private readonly object _nativeStringLock = new();
    private readonly object _shuffleLock = new();
    private readonly Dictionary<string, nint> _nativeStrings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _exactFallbacks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _categoryFallbacks = new(StringComparer.Ordinal);
    private readonly HashSet<string> _blockedStems = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _currentCatalogStems = new(StringComparer.Ordinal);
    private readonly HashSet<string> _currentCatalogEvents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ShuffleBag> _shuffleBags = new(StringComparer.Ordinal);

    private AgentVoiceFixGamedata _gamedata = new();
    private MemoryFunctionVoid<nint, int>? _generateSceneForSound;
    private int _soundEventNamePointerOffset;
    private bool _hooked;

    [ThreadStatic]
    private static Stack<RestoreFrame?>? s_restoreFrames;

    private string GamedataPath => Path.Combine(ModuleDirectory, GamedataFileName);

    public override void Load(bool hotReload)
    {
        if (!OperatingSystem.IsWindows())
        {
            Logger.LogWarning("[AgentVoiceFix] Windows-only build; plugin remains inactive.");
            return;
        }

        try
        {
            LoadGamedata();
            LoadEmbeddedRulesAndCatalog();

            _generateSceneForSound = new MemoryFunctionVoid<nint, int>(
                _gamedata.WindowsSignature.Trim(),
                Addresses.ServerPath);

            OffsetDetectionResult offsetDetection = DetectSoundEventNamePointerOffset(
                Addresses.ServerPath,
                _gamedata.WindowsSignature.Trim());
            _soundEventNamePointerOffset = offsetDetection.Displacement;

            _generateSceneForSound.Hook(OnGenerateScenePre, HookMode.Pre);
            _generateSceneForSound.Hook(OnGenerateScenePost, HookMode.Post);
            _hooked = true;

            Logger.LogInformation(
                "[AgentVoiceFix] Active. function={Function} hookHandle={HookHandle} signatureFileOffset=0x{FileOffset:X} autoOffset=0x{Offset:X} catalogStems={Stems} healthyEvents={Events} exactRules={Exact} semanticRules={Semantic}",
                _gamedata.TargetFunction,
                _generateSceneForSound.Handle,
                offsetDetection.FunctionFileOffset,
                _soundEventNamePointerOffset,
                _currentCatalogStems.Count,
                _currentCatalogEvents.Count,
                _exactFallbacks.Count,
                _categoryFallbacks.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[AgentVoiceFix] Startup failed; plugin remains inactive.");
            SafeUnhook();
        }
    }

    public override void Unload(bool hotReload)
    {
        SafeUnhook();
        Logger.LogInformation("[AgentVoiceFix] Unloaded.");
    }

    private HookResult OnGenerateScenePre(DynamicHook hook)
    {
        Stack<RestoreFrame?> stack = s_restoreFrames ??= new Stack<RestoreFrame?>();
        stack.Push(null);

        try
        {
            nint sceneEntity = hook.GetParam<nint>(0);
            if (sceneEntity == 0)
                return HookResult.Continue;

            nint soundSlot = sceneEntity + _soundEventNamePointerOffset;
            nint originalSoundPointer = Marshal.ReadIntPtr(soundSlot);
            if (originalSoundPointer == 0)
                return HookResult.Continue;

            if (!TryReadUtf8StringBounded(originalSoundPointer, MaxSoundNameBytes, out string originalSound) ||
                !IsPlausibleVoiceEventName(originalSound))
            {
                return HookResult.Continue;
            }

            string? replacementSound = ResolveReplacement(originalSound);
            if (string.IsNullOrWhiteSpace(replacementSound) ||
                string.Equals(originalSound, replacementSound, StringComparison.Ordinal) ||
                !IsPlausibleVoiceEventName(replacementSound))
            {
                return HookResult.Continue;
            }

            nint replacementPointer = GetPersistentUtf8Pointer(replacementSound);
            Marshal.WriteIntPtr(soundSlot, replacementPointer);

            stack.Pop();
            stack.Push(new RestoreFrame(soundSlot, originalSoundPointer, replacementPointer));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[AgentVoiceFix] Exception in GenerateSceneForSound Pre hook.");
        }

        return HookResult.Continue;
    }

    private HookResult OnGenerateScenePost(DynamicHook hook)
    {
        Stack<RestoreFrame?>? stack = s_restoreFrames;
        if (stack is null || stack.Count == 0)
            return HookResult.Continue;

        RestoreFrame? frame = stack.Pop();
        if (frame is null)
            return HookResult.Continue;

        try
        {
            nint currentPointer = Marshal.ReadIntPtr(frame.Value.SoundSlot);
            if (currentPointer == frame.Value.ReplacementPointer)
                Marshal.WriteIntPtr(frame.Value.SoundSlot, frame.Value.OriginalPointer);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[AgentVoiceFix] Exception while restoring the original sound pointer.");
        }

        return HookResult.Continue;
    }

    private string? ResolveReplacement(string originalSound)
    {
        if (_exactFallbacks.TryGetValue(originalSound, out string[]? exactCandidates) && exactCandidates.Length > 0)
            return TakeFromShuffleBag("exact:" + originalSound, exactCandidates);

        string? stem = TryGetStem(originalSound);
        if (stem is null)
            return null;

        if (_blockedStems.Contains(stem))
        {
            return _categoryFallbacks.TryGetValue(stem, out string[]? semanticCandidates) && semanticCandidates.Length > 0
                ? TakeFromShuffleBag("category:" + stem, semanticCandidates)
                : null;
        }

        if (!_currentCatalogStems.TryGetValue(stem, out string[]? currentCandidates) || currentCandidates.Length == 0)
            return null;

        if (_currentCatalogEvents.Contains(originalSound))
            return null;

        return TakeFromShuffleBag("catalog:" + stem, currentCandidates);
    }

    private void LoadGamedata()
    {
        if (!File.Exists(GamedataPath))
        {
            throw new FileNotFoundException(
                $"Required gamedata file is missing: {GamedataPath}",
                GamedataPath);
        }

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        _gamedata = JsonSerializer.Deserialize<AgentVoiceFixGamedata>(
            File.ReadAllText(GamedataPath),
            options)
            ?? throw new InvalidDataException(
                "Gamedata JSON is empty or invalid.");

        if (string.IsNullOrWhiteSpace(_gamedata.TargetFunction))
            throw new InvalidDataException("TargetFunction is empty.");

        if (string.IsNullOrWhiteSpace(_gamedata.WindowsSignature))
            throw new InvalidDataException("WindowsSignature is empty.");
    }

    private static OffsetDetectionResult DetectSoundEventNamePointerOffset(
        string binaryPath,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(binaryPath) || !File.Exists(binaryPath))
        {
            throw new FileNotFoundException(
                $"The server binary used for automatic offset detection was not found: {binaryPath}",
                binaryPath);
        }

        byte[] image;
        try
        {
            image = File.ReadAllBytes(binaryPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unable to read server.dll for automatic offset detection: {binaryPath}",
                ex);
        }

        SignaturePattern pattern = ParseSignature(signature);
        int[] matches = FindSignatureMatches(image, pattern);
        if (matches.Length != 1)
        {
            string details = matches.Length == 0
                ? "none"
                : string.Join(", ", matches.Select(static offset => $"0x{offset:X}"));

            throw new InvalidOperationException(
                "The Windows signature did not resolve to exactly one location in the on-disk server.dll. " +
                $"matches={details}. Update WindowsSignature before enabling the plugin.");
        }

        int functionFileOffset = matches[0];
        int availableBytes = Math.Min(
            NativeFunctionProbeBytes,
            image.Length - functionFileOffset);
        if (availableBytes < 32)
        {
            throw new InvalidOperationException(
                $"The resolved function at file offset 0x{functionFileOffset:X} is too close to the end of server.dll.");
        }

        byte[] code = new byte[availableBytes];
        Buffer.BlockCopy(
            image,
            functionFileOffset,
            code,
            0,
            availableBytes);

        int displacement = DetectSoundEventNamePointerOffsetFromCode(code);
        return new OffsetDetectionResult(displacement, functionFileOffset);
    }

    private static int DetectSoundEventNamePointerOffsetFromCode(byte[] code)
    {
        List<SoundOffsetCandidate> candidates = [];
        int searchLimit = Math.Min(
            NativeFunctionCandidateSearchBytes,
            code.Length - 7);

        for (int instructionOffset = 0;
             instructionOffset <= searchLimit;
             instructionOffset++)
        {
            if (!TryDecodeThisPointerLoad(
                    code,
                    instructionOffset,
                    out int valueRegister,
                    out int displacement))
            {
                continue;
            }

            if (displacement is < MinimumSoundEventOffset or > MaximumSoundEventOffset ||
                (displacement & 0x7) != 0)
            {
                continue;
            }

            int nullTestEnd = FindNullTestEnd(
                code,
                instructionOffset + 7,
                instructionOffset + 24,
                valueRegister);
            if (nullTestEnd < 0)
                continue;

            // The same pointer must become RDX for the duration lookup.
            int durationArgumentMove = FindRegisterMove(
                code,
                nullTestEnd,
                instructionOffset + 160,
                valueRegister,
                destinationRegister: 2);
            if (durationArgumentMove < 0 ||
                !ContainsCall(
                    code,
                    durationArgumentMove + 3,
                    durationArgumentMove + 40))
            {
                continue;
            }

            // The same pointer must later become R9 for the
            // "Couldn't determine duration of %s" warning call.
            int warningArgumentMove = FindRegisterMove(
                code,
                durationArgumentMove + 3,
                instructionOffset + 320,
                valueRegister,
                destinationRegister: 9);
            if (warningArgumentMove < 0 ||
                !ContainsCall(
                    code,
                    warningArgumentMove + 3,
                    warningArgumentMove + 40))
            {
                continue;
            }

            candidates.Add(new SoundOffsetCandidate(
                instructionOffset,
                valueRegister,
                displacement,
                durationArgumentMove,
                warningArgumentMove));
        }

        SoundOffsetCandidate[] uniqueCandidates = candidates
            .GroupBy(static candidate => candidate.Displacement)
            .Select(static group => group.First())
            .ToArray();

        if (uniqueCandidates.Length != 1)
        {
            string details = uniqueCandidates.Length == 0
                ? "none"
                : string.Join(
                    ", ",
                    uniqueCandidates.Select(static candidate =>
                        $"0x{candidate.Displacement:X}@+0x{candidate.InstructionOffset:X}"));

            throw new InvalidOperationException(
                "Automatic sound-event offset detection did not produce exactly " +
                $"one verified candidate. candidates={details}. " +
                "Inspect the new native function before enabling the plugin.");
        }

        return uniqueCandidates[0].Displacement;
    }

    private static SignaturePattern ParseSignature(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            throw new FormatException("WindowsSignature is empty.");

        string[] tokens = signature.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            throw new FormatException("WindowsSignature contains no tokens.");

        byte[] bytes = new byte[tokens.Length];
        bool[] wildcards = new bool[tokens.Length];
        bool hasFixedByte = false;

        for (int index = 0; index < tokens.Length; index++)
        {
            string token = tokens[index];
            if (token is "?" or "??")
            {
                wildcards[index] = true;
                continue;
            }

            if (token.Length != 2 ||
                !byte.TryParse(
                    token,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out bytes[index]))
            {
                throw new FormatException(
                    $"WindowsSignature contains an invalid byte token at index {index}: '{token}'.");
            }

            hasFixedByte = true;
        }

        if (!hasFixedByte)
            throw new FormatException("WindowsSignature cannot consist entirely of wildcards.");

        return new SignaturePattern(bytes, wildcards);
    }

    private static int[] FindSignatureMatches(
        byte[] image,
        SignaturePattern pattern)
    {
        if (pattern.Bytes.Length > image.Length)
            return [];

        int anchorIndex = Array.FindIndex(
            pattern.Wildcards,
            static wildcard => !wildcard);
        if (anchorIndex < 0)
            return [];

        byte anchorByte = pattern.Bytes[anchorIndex];
        int maximumStart = image.Length - pattern.Bytes.Length;
        List<int> matches = [];
        int searchIndex = anchorIndex;

        while (searchIndex < image.Length)
        {
            int foundAnchor = Array.IndexOf(
                image,
                anchorByte,
                searchIndex);
            if (foundAnchor < 0)
                break;

            int candidateStart = foundAnchor - anchorIndex;
            if (candidateStart >= 0 &&
                candidateStart <= maximumStart &&
                SignatureMatchesAt(image, candidateStart, pattern))
            {
                matches.Add(candidateStart);
            }

            searchIndex = foundAnchor + 1;
        }

        return matches.ToArray();
    }

    private static bool SignatureMatchesAt(
        byte[] image,
        int start,
        SignaturePattern pattern)
    {
        for (int index = 0; index < pattern.Bytes.Length; index++)
        {
            if (!pattern.Wildcards[index] &&
                image[start + index] != pattern.Bytes[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryDecodeThisPointerLoad(
        byte[] code,
        int index,
        out int destinationRegister,
        out int displacement)
    {
        destinationRegister = -1;
        displacement = 0;

        if (index < 0 || index + 7 > code.Length)
            return false;

        byte rex = code[index];
        if (!IsRexW(rex) || code[index + 1] != 0x8B)
            return false;

        byte modRm = code[index + 2];

        // mov r64, [rcx + disp32]
        // mod=10 (disp32), r/m=001 (RCX), and REX.B must be clear.
        if ((modRm & 0xC7) != 0x81 || (rex & 0x01) != 0)
            return false;

        destinationRegister = ((modRm >> 3) & 0x07) +
            ((rex & 0x04) != 0 ? 8 : 0);
        displacement = BinaryPrimitives.ReadInt32LittleEndian(
            code.AsSpan(index + 3, 4));
        return true;
    }

    private static int FindNullTestEnd(
        byte[] code,
        int start,
        int end,
        int register)
    {
        int limit = Math.Min(end, code.Length);
        for (int index = Math.Max(0, start); index < limit; index++)
        {
            if (TryDecodeSelfTest(code, index, register, out int length) ||
                TryDecodeCompareZero(code, index, register, out length))
            {
                return index + length;
            }
        }

        return -1;
    }

    private static bool TryDecodeSelfTest(
        byte[] code,
        int index,
        int register,
        out int length)
    {
        length = 0;
        if (index < 0 || index + 3 > code.Length)
            return false;

        byte rex = code[index];
        byte modRm = code[index + 2];
        if (!IsRexW(rex) ||
            code[index + 1] != 0x85 ||
            (modRm & 0xC0) != 0xC0)
        {
            return false;
        }

        int left = ((modRm >> 3) & 0x07) +
            ((rex & 0x04) != 0 ? 8 : 0);
        int right = (modRm & 0x07) +
            ((rex & 0x01) != 0 ? 8 : 0);

        if (left != register || right != register)
            return false;

        length = 3;
        return true;
    }

    private static bool TryDecodeCompareZero(
        byte[] code,
        int index,
        int register,
        out int length)
    {
        length = 0;
        if (index < 0 || index + 4 > code.Length)
            return false;

        byte rex = code[index];
        byte modRm = code[index + 2];
        if (!IsRexW(rex) ||
            code[index + 1] != 0x83 ||
            (modRm & 0xC0) != 0xC0 ||
            ((modRm >> 3) & 0x07) != 7 ||
            code[index + 3] != 0)
        {
            return false;
        }

        int comparedRegister = (modRm & 0x07) +
            ((rex & 0x01) != 0 ? 8 : 0);
        if (comparedRegister != register)
            return false;

        length = 4;
        return true;
    }

    private static int FindRegisterMove(
        byte[] code,
        int start,
        int end,
        int sourceRegister,
        int destinationRegister)
    {
        int limit = Math.Min(end, code.Length - 2);
        for (int index = Math.Max(0, start); index < limit; index++)
        {
            if (TryDecodeRegisterMove(
                    code,
                    index,
                    out int decodedDestination,
                    out int decodedSource) &&
                decodedSource == sourceRegister &&
                decodedDestination == destinationRegister)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryDecodeRegisterMove(
        byte[] code,
        int index,
        out int destinationRegister,
        out int sourceRegister)
    {
        destinationRegister = -1;
        sourceRegister = -1;

        if (index < 0 || index + 3 > code.Length)
            return false;

        byte rex = code[index];
        byte opcode = code[index + 1];
        byte modRm = code[index + 2];
        if (!IsRexW(rex) ||
            (opcode != 0x8B && opcode != 0x89) ||
            (modRm & 0xC0) != 0xC0)
        {
            return false;
        }

        int regField = ((modRm >> 3) & 0x07) +
            ((rex & 0x04) != 0 ? 8 : 0);
        int rmField = (modRm & 0x07) +
            ((rex & 0x01) != 0 ? 8 : 0);

        if (opcode == 0x8B)
        {
            destinationRegister = regField;
            sourceRegister = rmField;
        }
        else
        {
            destinationRegister = rmField;
            sourceRegister = regField;
        }

        return true;
    }

    private static bool ContainsCall(byte[] code, int start, int end)
    {
        int limit = Math.Min(end, code.Length - 1);
        for (int index = Math.Max(0, start); index < limit; index++)
        {
            if (code[index] == 0xE8 && index + 5 <= code.Length)
                return true;

            if (code[index] == 0xFF &&
                index + 2 <= code.Length &&
                ((code[index + 1] >> 3) & 0x07) == 2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRexW(byte value) =>
        value >= 0x48 && value <= 0x4F && (value & 0x08) != 0;

    private void LoadEmbeddedRulesAndCatalog()
    {
        _exactFallbacks.Clear();
        _categoryFallbacks.Clear();
        _blockedStems.Clear();
        _currentCatalogStems.Clear();
        _currentCatalogEvents.Clear();
        _shuffleBags.Clear();

        foreach ((string badEvent, string[] candidates) in AgentVoiceFixRules.CreateDefaultExactFallbacks())
        {
            if (!IsPlausibleVoiceEventName(badEvent))
                continue;

            string[] clean = CleanCandidatePool(badEvent, candidates);
            if (clean.Length > 0)
                _exactFallbacks[badEvent] = clean;
        }

        foreach (string stem in AgentVoiceFixRules.CreateDefaultBlockedStems())
        {
            if (IsPlausibleVoiceEventName(stem))
                _blockedStems.Add(stem);
        }

        foreach ((string stem, string[] candidates) in AgentVoiceFixRules.CreateDefaultCategoryFallbacks())
        {
            if (!_blockedStems.Contains(stem))
                continue;

            string[] clean = CleanCandidatePool(string.Empty, candidates);
            if (clean.Length > 0)
                _categoryFallbacks[stem] = clean;
        }

        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CatalogResourceName)
            ?? throw new InvalidOperationException($"Embedded voice catalog not found: {CatalogResourceName}");

        CurrentVoiceCatalogDocument? document = JsonSerializer.Deserialize<CurrentVoiceCatalogDocument>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (document is null || document.SchemaVersion != 1)
            throw new InvalidDataException("Embedded voice catalog is empty or has an unsupported schema.");

        foreach ((string stem, string[] candidates) in document.NumberedStems)
        {
            if (!IsPlausibleVoiceEventName(stem))
                continue;

            string[] clean = candidates
                .Where(IsPlausibleVoiceEventName)
                .Where(candidate => string.Equals(TryGetStem(candidate), stem, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static candidate => candidate, StringComparer.Ordinal)
                .ToArray();

            if (clean.Length == 0)
                continue;

            _currentCatalogStems[stem] = clean;
            foreach (string candidate in clean)
                _currentCatalogEvents.Add(candidate);
        }

        if (_currentCatalogStems.Count == 0)
            throw new InvalidDataException("Embedded voice catalog contains no usable numbered stems.");

        FilterPoolDictionaryAgainstCatalog(_exactFallbacks);
        FilterPoolDictionaryAgainstCatalog(_categoryFallbacks);
    }

    private void FilterPoolDictionaryAgainstCatalog(Dictionary<string, string[]> pools)
    {
        foreach (string key in pools.Keys.ToArray())
        {
            string[] healthy = pools[key]
                .Where(_currentCatalogEvents.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (healthy.Length == 0)
                pools.Remove(key);
            else
                pools[key] = healthy;
        }
    }

    private static string[] CleanCandidatePool(string originalSound, IEnumerable<string>? candidates)
    {
        if (candidates is null)
            return [];

        return candidates
            .Where(IsPlausibleVoiceEventName)
            .Where(candidate => !string.Equals(candidate, originalSound, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private string TakeFromShuffleBag(string key, string[] candidates)
    {
        lock (_shuffleLock)
        {
            if (!_shuffleBags.TryGetValue(key, out ShuffleBag? bag) || !bag.HasSameCandidates(candidates))
            {
                bag = new ShuffleBag(candidates);
                _shuffleBags[key] = bag;
            }

            return bag.Take();
        }
    }

    private nint GetPersistentUtf8Pointer(string value)
    {
        lock (_nativeStringLock)
        {
            if (_nativeStrings.TryGetValue(value, out nint existing))
                return existing;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            nint pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer + bytes.Length, 0);
            _nativeStrings[value] = pointer;
            return pointer;
        }
    }

    private static bool TryReadUtf8StringBounded(nint pointer, int maxBytes, out string value)
    {
        value = string.Empty;
        byte[] buffer = new byte[maxBytes];
        int length = 0;

        try
        {
            for (; length < maxBytes; length++)
            {
                byte current = Marshal.ReadByte(pointer, length);
                if (current == 0)
                    break;
                buffer[length] = current;
            }
        }
        catch
        {
            return false;
        }

        if (length == 0 || length == maxBytes)
            return false;

        try
        {
            value = Encoding.UTF8.GetString(buffer, 0, length);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPlausibleVoiceEventName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 3 or > 191 || !value.Contains('.'))
            return false;

        return PlausibleEventRegex.IsMatch(value);
    }

    private static string? TryGetStem(string sound)
    {
        if (string.IsNullOrWhiteSpace(sound))
            return null;

        int digitStart = sound.Length;
        while (digitStart > 0 && sound[digitStart - 1] is >= '0' and <= '9')
            digitStart--;

        int numberWidth = sound.Length - digitStart;
        if (numberWidth is < 1 or > 3 || digitStart <= 0)
            return null;

        int stemEnd = digitStart;
        if (stemEnd > 0 && sound[stemEnd - 1] == '_')
            stemEnd--;

        return stemEnd > 0 ? sound[..stemEnd] : null;
    }

    private void SafeUnhook()
    {
        if (!_hooked || _generateSceneForSound is null)
            return;

        try
        {
            _generateSceneForSound.Unhook(OnGenerateScenePre, HookMode.Pre);
            _generateSceneForSound.Unhook(OnGenerateScenePost, HookMode.Post);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[AgentVoiceFix] Error while removing GenerateSceneForSound hooks.");
        }
        finally
        {
            _hooked = false;
            _generateSceneForSound = null;
        }
    }

    private readonly record struct SignaturePattern(
        byte[] Bytes,
        bool[] Wildcards);

    private readonly record struct OffsetDetectionResult(
        int Displacement,
        int FunctionFileOffset);

    private readonly record struct SoundOffsetCandidate(
        int InstructionOffset,
        int ValueRegister,
        int Displacement,
        int DurationArgumentMove,
        int WarningArgumentMove);

    private readonly record struct RestoreFrame(
        nint SoundSlot,
        nint OriginalPointer,
        nint ReplacementPointer);

    private sealed class ShuffleBag
    {
        private readonly string[] _canonical;
        private string[] _bag = [];
        private int _index;

        public ShuffleBag(string[] candidates)
        {
            _canonical = candidates
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static candidate => candidate, StringComparer.Ordinal)
                .ToArray();
            Refill();
        }

        public bool HasSameCandidates(string[] candidates)
        {
            string[] other = candidates
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static candidate => candidate, StringComparer.Ordinal)
                .ToArray();
            return _canonical.SequenceEqual(other, StringComparer.Ordinal);
        }

        public string Take()
        {
            if (_index >= _bag.Length)
                Refill();
            return _bag[_index++];
        }

        private void Refill()
        {
            _bag = _canonical.ToArray();
            Random.Shared.Shuffle(_bag);
            _index = 0;
        }
    }
}

public sealed class AgentVoiceFixGamedata
{
    /// <summary>
    /// Human-readable name of the native function located by WindowsSignature.
    /// This field is informational and appears in startup logs.
    /// </summary>
    public string TargetFunction { get; set; } = string.Empty;

    /// <summary>
    /// Windows AOB signature used to locate the target function in server.dll.
    /// </summary>
    public string WindowsSignature { get; set; } = string.Empty;
}
