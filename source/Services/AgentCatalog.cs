namespace InventorySimulator;

/// <summary>
/// Current CS2 tradable-agent metadata derived from scripts/items/items_game.txt.
/// DefIndex is the item-definition ID used by LOADOUT_SLOT_CLOTHING_CUSTOMPLAYER;
/// it is not a weapon PaintKit.
/// </summary>
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

        // items_game entries without vo_prefix intentionally inherit the map/BOT class voice.
        // They must remain visual-only: rebuilding them as paid-agent loadout items can clear the
        // existing Radio/DamageReact/Response graph even though the model itself is valid.
        public bool UsesInheritedClassVoice => !HasExplicitVoicePrefix;

        // Only explicit items_game vo_prefix values are synchronized.  Inherited agents keep the
        // engine-selected class prefix exactly as it was initialized during the normal spawn path.
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
        [5400] = new(5400, 3, "3rd Commando Company", "agents/models/ctm_st6/ctm_st6_variantk.vmdl", "ctm_gsg9", false),
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

        // items_game uses this inventory-facing alias for DefIndex 5400, while the actual
        // sound events are named gsg9.*.
        if (normalizedPrefix == "ctm_gsg9")
            return "gsg9";

        return normalizedPrefix;
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

