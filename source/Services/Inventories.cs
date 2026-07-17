using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace InventorySimulator;

public static class Inventories
{
    private static readonly Dictionary<ulong, PlayerInventory> _loadedInventories = [];
    private static readonly string _inventoryFileDir =
        "csgo/addons/counterstrikesharp/configs/plugins/InventorySimulator";
    private static readonly Random _rng = new Random();
    
    private const byte TEAM_CT = 3;
    private const byte TEAM_T = 2;
    private static readonly Random _random = new();
    private static readonly object _randomLock = new();
    private static ulong? _hostSteamId = null;

    private sealed class AgentAssignment
    {
        public ushort CtDef { get; init; }
        public ushort TDef { get; init; }
    }

    // One assignment per controller owner. Special agents are selected from the currently unused
    // definitions, so two connected players do not receive the same agent for the same team.
    private static readonly Dictionary<ulong, AgentAssignment> _agentAssignments = [];

private static bool IsBotPlayer(CCSPlayerController? c)
{
    if (c == null || !c.IsValid || c.IsHLTV)
        return false;

    if (c.IsBot)
        return true;

    return ShouldApplyToPlayers();
}
    // BotInventory JSON config
    // address: addons/counterstrikesharp/configs/plugins/Botinventory/BotInventory.json
    private const string BOT_INVENTORY_CONFIG_FILE = "BotInventory.json";
    private static BotInventoryConfig _botInventoryConfig = CreateDefaultBotInventoryConfig();
    private static bool _botInventoryConfigLoaded = false;
    private static readonly JsonSerializerOptions _botInventoryJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private sealed class BotInventoryConfig
    {
        public bool ApplyToPlayers { get; set; } = false;
        public WeaponSkinConfig Weapons { get; set; } = new();
        public KnifeSkinSection Knives { get; set; } = new();
        // These sections are used by InventorySimulator.BotExtras.cs.
        // They are kept in the same BotInventory.json so all BOT appearance data is in one place.
        public GloveSkinSection Gloves { get; set; } = new();
        public MusicKitConfig MusicKits { get; set; } = new();
        public ModelConfig Models { get; set; } = new();
        public AgentSkinConfig Agents { get; set; } = new();
        public StickerSkinConfig Stickers { get; set; } = new();
        public StatTrakSkinConfig StatTrak { get; set; } = new();
        public SouvenirConfig Souvenir { get; set; } = new();
    }

    private sealed class WeaponSkinConfig
    {
        public bool Enabled { get; set; } = true;
        public float MaxWear { get; set; } = 0.2f;
        public int MinSeed { get; set; } = 1;
        public int MaxSeed { get; set; } = 10000;
        public Dictionary<string, int[]> PaintKits { get; set; } = new();
    }

    private sealed class KnifeSkinSection
    {
        public bool Enabled { get; set; } = true;
        public float MaxWear { get; set; } = 0.09f;
        public List<KnifeSkinConfig> Items { get; set; } = new();
    }

    private sealed class KnifeSkinConfig
    {
        public int Def { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int[] PaintKits { get; set; } = Array.Empty<int>();
    }

    private sealed class GloveSkinSection
    {
        public bool Enabled { get; set; } = true;
        public float MaxWear { get; set; } = 0.06f;
        public int MinSeed { get; set; } = 1;
        public int MaxSeed { get; set; } = 10000;
        public List<GloveSkinConfig> Items { get; set; } = new();
    }

    private sealed class GloveSkinConfig
    {
        public int Def { get; set; }
        public int Paint { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    private sealed class MusicKitConfig
    {
        public bool Enabled { get; set; } = true;
        public int[] Ids { get; set; } = Array.Empty<int>();
    }

    private sealed class ModelConfig
    {
        public bool Enabled { get; set; } = true;
        public string[] CtModels { get; set; } = Array.Empty<string>();
        public string[] TModels { get; set; } = Array.Empty<string>();
    }

    public readonly struct BotGloveSkin
    {
        public ushort DefIndex { get; }
        public int PaintKit { get; }
        public string Name { get; }

        public BotGloveSkin(ushort defIndex, int paintKit, string name)
        {
            DefIndex = defIndex;
            PaintKit = paintKit;
            Name = name;
        }
    }

    private sealed class AgentSkinConfig
    {
        public bool Enabled { get; set; } = true;
        public double Chance { get; set; } = 1.0;

        // Agent cosmetics are item definitions, not paint kits. BotExtras resolves the model
        // directly from this DefIndex and synchronizes the character/VO fields, so plain BOTs do
        // not depend on CCSPlayerInventory, GC loadout state, or BotHider.
        public int[] CtDefIndexes { get; set; } = new[]
        {
            4757, 4771, 5308, 4751, 5404, 4711, 4750, 4772, 4712, 5307,
            5403, 4680, 4619, 4753, 4756, 4715, 4713, 4749, 5306, 5402,
            5602, 5405, 5400, 4716, 4714, 5401, 5305, 5601, 4752
        };

        public int[] TDefIndexes { get; set; } = new[]
        {
            4726, 4736, 4735, 4777, 4734, 4733, 4780, 4774, 5504, 5108,
            4732, 4613, 4773, 4727, 4775, 5503, 4718, 5502, 5107, 4730,
            4728, 4776, 4781, 5207, 5501, 5500, 5106, 4778, 5109, 5208,
            5505, 5105, 5206, 5205
        };
    }

    private sealed class StickerSkinConfig
    {
        public bool Enabled { get; set; } = true;
        public int Kato14Min { get; set; } = 48;
        public int Kato14Max { get; set; } = 172;
        public double Kato14Rate { get; set; } = 0.6;
        public int NormalMin { get; set; } = 3000;
        public int NormalMax { get; set; } = 5000;
    }

    private sealed class StatTrakSkinConfig
    {
        public bool Enabled { get; set; } = true;
        public double Chance { get; set; } = 0.6;
        public int CounterMin { get; set; } = 1;
        public int CounterMax { get; set; } = 10001;
        public Dictionary<string, int[]> WeaponPaints { get; set; } = new();
    }

    private sealed class SouvenirConfig
    {
        public bool Enabled { get; set; } = true;
        public double Chance { get; set; } = 0.6;
    }

    private static BotInventoryConfig CreateDefaultBotInventoryConfig()
    {
        return new BotInventoryConfig
        {
            ApplyToPlayers = false,
            
            Weapons = new WeaponSkinConfig
            {
                Enabled = true,
                MaxWear = 0.2f,
                PaintKits = new Dictionary<string, int[]>
                {
                    ["61"] = new[] { 25, 60, 115, 183, 217, 221, 236, 277, 290, 313, 318, 332, 339, 364, 443, 454, 489, 504, 540, 637, 653, 657, 705, 796, 817, 818, 830, 922, 991, 1027, 1031, 1040, 1065, 1102, 1136, 1142, 1173, 1186, 1217, 1253, 1284, 1323, 1377, 1431 },
                    ["4"] = new[] { 2, 3, 38, 40, 48, 84, 129, 152, 159, 208, 230, 278, 293, 353, 367, 381, 399, 437, 479, 495, 532, 586, 607, 623, 680, 694, 713, 732, 789, 799, 808, 832, 918, 957, 963, 988, 1016, 1039, 1079, 1100, 1119, 1120, 1121, 1122, 1123, 1158, 1167, 1200, 1208, 1227, 1240, 1265, 1282, 1312, 1348, 1357, 1421 },
                    ["1"] = new[] { 17, 37, 40, 61, 90, 114, 138, 185, 231, 232, 237, 273, 296, 328, 347, 351, 397, 425, 468, 469, 470, 509, 527, 603, 645, 711, 757, 764, 805, 841, 938, 945, 962, 992, 1006, 1050, 1054, 1056, 1090, 1189, 1257, 1318, 1360, 1430 },
                    ["2"] = new[] { 28, 43, 46, 47, 112, 139, 153, 190, 220, 249, 261, 276, 307, 330, 396, 447, 450, 453, 491, 528, 544, 625, 658, 710, 747, 824, 860, 895, 903, 978, 998, 1005, 1086, 1091, 1126, 1156, 1169, 1263, 1290, 1335, 1347, 1373 },
                    ["3"] = new[] { 3, 44, 46, 78, 141, 151, 210, 223, 252, 254, 265, 274, 352, 377, 387, 427, 464, 510, 530, 585, 605, 646, 660, 693, 729, 784, 831, 837, 906, 932, 979, 1002, 1062, 1082, 1093, 1128, 1168, 1262, 1336, 1380, 1429 },
                    ["7"] = new[] { 14, 44, 72, 113, 122, 142, 170, 172, 180, 226, 282, 300, 302, 316, 340, 341, 380, 394, 422, 456, 474, 490, 506, 524, 600, 639, 656, 675, 707, 724, 745, 795, 801, 836, 885, 912, 921, 941, 959, 1004, 1018, 1035, 1070, 1087, 1141, 1143, 1171, 1179, 1207, 1218, 1221, 1238, 1283, 1288, 1309, 1352, 1358, 1397, 1425 },
                    ["16"] = new[] { 8, 16, 17, 101, 118, 155, 164, 167, 176, 187, 215, 255, 309, 336, 384, 400, 449, 471, 480, 512, 533, 588, 632, 664, 695, 730, 780, 793, 811, 844, 874, 926, 971, 985, 993, 1041, 1063, 1097, 1149, 1165, 1209, 1210, 1228, 1255, 1266, 1281, 1313, 1353, 1364, 1432 },
                    ["60"] = new[] { 1177, 1433, 1166, 984, 106, 1376, 1340, 714, 1017, 1338, 497, 1216, 644, 430, 1130, 383, 360, 445, 946, 587, 254, 548, 1311, 301, 1001, 681, 326, 1223, 321, 1243, 257, 631, 60, 1073, 440, 792, 862, 217, 160, 663, 189, 1059, 1319, 77, 235 },
                    ["9"] = new[] { 30, 51, 72, 84, 137, 163, 174, 181, 212, 227, 251, 259, 279, 344, 395, 424, 446, 451, 475, 525, 584, 640, 662, 691, 718, 736, 756, 788, 803, 819, 838, 887, 917, 943, 975, 1026, 1029, 1058, 1144, 1170, 1206, 1213, 1222, 1239, 1280, 1324, 1346, 1356, 1378, 1422 },
                    ["39"] = new[] { 28, 39, 61, 98, 101, 136, 186, 243, 247, 287, 298, 363, 378, 487, 519, 553, 598, 613, 686, 702, 750, 765, 815, 861, 864, 897, 901, 934, 955, 966, 1022, 1048, 1084, 1151, 1234, 1270, 1320, 1394 },
                    ["8"] = new[] { 9, 10, 33, 46, 47, 73, 100, 110, 121, 134, 173, 197, 246, 280, 305, 375, 444, 455, 507, 541, 583, 601, 674, 690, 708, 727, 740, 758, 779, 794, 823, 845, 886, 913, 927, 942, 995, 1033, 1088, 1198, 1249, 1308, 1339, 1362 },
                    ["33"] = new[] { 5, 11, 15, 28, 102, 141, 175, 209, 213, 245, 250, 354, 365, 423, 442, 481, 500, 536, 627, 649, 696, 719, 728, 752, 782, 847, 893, 935, 940, 1007, 1023, 1096, 1133, 1163, 1246, 1326, 1354, 1386, 1436 },
                    ["13"] = new[] { 76, 83, 101, 119, 192, 216, 235, 237, 239, 241, 246, 264, 294, 297, 308, 379, 398, 428, 460, 478, 494, 546, 629, 647, 661, 790, 807, 842, 939, 972, 981, 1013, 1032, 1038, 1071, 1147, 1178, 1185, 1264, 1275, 1296, 1314, 1383, 1434 },
                    ["10"] = new[] { 22, 47, 60, 92, 154, 178, 194, 218, 240, 244, 260, 288, 371, 429, 461, 477, 492, 529, 604, 626, 659, 723, 835, 863, 869, 882, 904, 919, 999, 1053, 1066, 1092, 1127, 1146, 1184, 1202, 1219, 1241, 1302, 1321, 1365, 1393 },
                    ["23"] = new[] { 161, 753, 768, 781, 798, 800, 810, 846, 872, 888, 915, 923, 949, 974, 986, 1061, 1137, 1180, 1231, 1274, 1294, 1344, 1366, 1385 },
                    ["24"] = new[] { 15, 17, 37, 70, 90, 93, 131, 169, 175, 193, 250, 281, 333, 362, 392, 412, 436, 441, 488, 556, 615, 652, 672, 688, 704, 725, 778, 802, 851, 879, 916, 990, 1003, 1008, 1049, 1085, 1157, 1175, 1194, 1203, 1236, 1303, 1351, 1387, 1426 },
                    ["19"] = new[] { 20, 67, 100, 111, 124, 127, 133, 156, 169, 175, 182, 228, 234, 244, 283, 311, 335, 342, 359, 486, 516, 593, 611, 636, 669, 717, 726, 744, 759, 776, 828, 849, 911, 925, 936, 969, 977, 1000, 1015, 1020, 1074, 1154, 1190, 1199, 1233, 1250, 1256, 1277, 1291, 1332, 1361, 1419 },
                    ["29"] = new[] { 5, 30, 41, 83, 119, 171, 204, 246, 250, 256, 323, 345, 390, 405, 434, 458, 517, 552, 596, 638, 655, 673, 720, 797, 814, 870, 880, 953, 1014, 1140, 1155, 1160, 1272, 1391, 1427 },
                    ["27"] = new[] { 32, 34, 39, 70, 99, 100, 171, 177, 198, 291, 327, 385, 431, 462, 473, 499, 535, 608, 633, 666, 703, 737, 754, 773, 787, 822, 909, 948, 961, 1072, 1089, 1132, 1188, 1220, 1245, 1306, 1355 },
                    ["26"] = new[] { 3, 13, 25, 70, 148, 149, 159, 164, 171, 203, 224, 236, 267, 293, 306, 349, 376, 457, 508, 526, 542, 594, 641, 676, 692, 770, 775, 829, 873, 884, 973, 1083, 1099, 1125, 1325, 1374, 1392, 1418 },
                    ["25"] = new[] { 42, 95, 96, 135, 146, 166, 169, 205, 238, 240, 314, 320, 348, 370, 393, 407, 505, 521, 557, 616, 654, 689, 706, 731, 760, 821, 834, 850, 970, 994, 1021, 1046, 1078, 1103, 1135, 1174, 1182, 1201, 1215, 1254, 1267, 1287, 1333, 1381 },
                    ["35"] = new[] { 3, 25, 62, 99, 107, 145, 158, 164, 166, 170, 191, 214, 225, 248, 263, 286, 294, 298, 299, 323, 324, 356, 450, 484, 537, 590, 634, 699, 716, 746, 785, 809, 890, 929, 987, 1051, 1077, 1162, 1192, 1247, 1261, 1331, 1337, 1350, 1368 },
                    ["14"] = new[] { 22, 75, 120, 151, 170, 202, 243, 266, 401, 452, 472, 496, 547, 648, 827, 875, 900, 902, 933, 983, 1042, 1148, 1242, 1298, 1370, 1435 },
                    ["28"] = new[] { 28, 144, 201, 240, 285, 298, 317, 355, 369, 432, 483, 514, 610, 698, 763, 783, 920, 950, 958, 1012, 1043, 1080, 1152, 1260, 1300 },
                    ["36"] = new[] { 15, 27, 34, 77, 78, 99, 102, 125, 130, 162, 164, 168, 207, 219, 230, 258, 271, 295, 358, 373, 388, 404, 426, 466, 467, 501, 551, 592, 650, 668, 678, 741, 749, 774, 777, 786, 813, 825, 848, 907, 928, 968, 982, 1030, 1044, 1081, 1153, 1212, 1230, 1248, 1273, 1307, 1315, 1317, 1345, 1369, 1420 },
                    ["63"] = new[] { 12, 32, 147, 218, 268, 269, 270, 297, 298, 315, 322, 325, 333, 334, 350, 366, 435, 453, 476, 543, 602, 622, 643, 687, 709, 859, 933, 937, 944, 976, 1036, 1064, 1076, 1195, 1329, 1390 },
                    ["30"] = new[] { 2, 17, 36, 159, 179, 206, 216, 235, 242, 248, 272, 289, 303, 374, 439, 459, 463, 520, 539, 555, 599, 614, 671, 684, 722, 733, 738, 766, 791, 795, 816, 839, 889, 905, 964, 1010, 1024, 1159, 1214, 1235, 1252, 1279, 1286, 1299, 1322, 1384 },
                    ["32"] = new[] { 21, 32, 71, 95, 104, 184, 211, 246, 275, 327, 338, 346, 357, 389, 443, 485, 515, 550, 591, 635, 667, 700, 878, 894, 951, 960, 997, 1019, 1055, 1138, 1181, 1224, 1259, 1292, 1342, 1359 },
                    ["34"] = new[] { 33, 39, 61, 100, 141, 148, 199, 262, 298, 329, 331, 366, 368, 386, 403, 448, 482, 549, 609, 630, 679, 697, 715, 734, 755, 804, 820, 867, 910, 931, 1037, 1094, 1134, 1193, 1211, 1225, 1258, 1278, 1301, 1310, 1330, 1341, 1375, 1388, 1423 },
                    ["17"] = new[] { 3, 17, 32, 38, 44, 98, 101, 126, 140, 157, 188, 246, 284, 310, 333, 337, 343, 372, 402, 433, 498, 534, 589, 651, 665, 682, 742, 748, 761, 812, 826, 840, 871, 898, 908, 947, 965, 1009, 1025, 1045, 1067, 1075, 1098, 1131, 1150, 1164, 1204, 1229, 1244, 1269, 1285, 1295, 1334, 1349, 1367 },
                    ["38"] = new[] { 46, 70, 100, 116, 117, 157, 159, 165, 196, 232, 298, 312, 391, 406, 502, 518, 597, 612, 642, 685, 865, 883, 896, 914, 954, 1028, 1139, 1226, 1327, 1343, 1371 },
                    ["40"] = new[] { 26, 60, 70, 96, 99, 128, 147, 200, 222, 233, 253, 304, 319, 361, 503, 513, 538, 554, 624, 670, 743, 751, 762, 868, 877, 899, 935, 956, 967, 989, 996, 1052, 1060, 1101, 1161, 1187, 1251, 1271, 1289, 1304, 1316, 1372, 1379 },
                    ["64"] = new[] { 12, 27, 37, 40, 123, 522, 523, 595, 683, 701, 721, 798, 843, 866, 892, 924, 952, 1011, 1047, 1145, 1232, 1237, 1276, 1293, 1363, 1389 },
                    ["11"] = new[] { 6, 8, 46, 72, 74, 147, 195, 229, 235, 294, 382, 438, 465, 493, 511, 545, 606, 628, 677, 712, 739, 806, 891, 930, 980, 1034, 1095, 1129, 1305, 1328 },
                }
            },
            Knives = new KnifeSkinSection
            {
                Enabled = true,
                MaxWear = 0.09f,
                Items = new List<KnifeSkinConfig>
                {
                    new KnifeSkinConfig { Def = 500, Name = @"Bayonet", Enabled = false, PaintKits = new[] { 413, 420, 38, 417, 568 } },
                    new KnifeSkinConfig { Def = 507, Name = @"Karambit", Enabled = true, PaintKits = new[] { 415, 572, 44, 42, 568, 12, 59, 38, 617, 413 } },
                    new KnifeSkinConfig { Def = 508, Name = @"M9 Bayonet", Enabled = true, PaintKits = new[] { 417, 413, 418, 568, 12, 59, 38, 415, 617 } },
                    new KnifeSkinConfig { Def = 515, Name = @"Butterfly", Enabled = true, PaintKits = new[] { 619, 44, 572, 413, 415, 12, 59, 38, 617 } },
                    new KnifeSkinConfig { Def = 522, Name = @"Stiletto", Enabled = false, PaintKits = new[] { 417, 420, 0, 418, 419 } },
                    new KnifeSkinConfig { Def = 523, Name = @"Talon", Enabled = true, PaintKits = new[] { 855, 38, 854, 853, 12, 59, 415, 617 } },
                    new KnifeSkinConfig { Def = 525, Name = @"Skeleton", Enabled = false, PaintKits = new[] { 0, 413, 44, 415, 416 } },
                }
            },
            Gloves = new GloveSkinSection
            {
                Enabled = true,
                MaxWear = 0.06f,
                MinSeed = 1,
                MaxSeed = 10000,
                Items = new List<GloveSkinConfig>
                {
                    new GloveSkinConfig { Def = 5030, Paint = 10048, Name = @"Sport Gloves | Vice", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1407, Name = @"Sport Gloves | Big Game", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1406, Name = @"Sport Gloves | Nocts", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10038, Name = @"Sport Gloves | Hedge Maze", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1405, Name = @"Sport Gloves | Scarlet Shamagh", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1417, Name = @"Sport Gloves | Bronze Morph", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10045, Name = @"Sport Gloves | Amphibious", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10037, Name = @"Sport Gloves | Pandora's Box", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1410, Name = @"Sport Gloves | Omega", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10073, Name = @"Sport Gloves | Slingshot", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10018, Name = @"Sport Gloves | Superconductor", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10076, Name = @"Sport Gloves | Nocts", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10047, Name = @"Sport Gloves | Omega", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10075, Name = @"Sport Gloves | Scarlet Shamagh", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10074, Name = @"Sport Gloves | Big Game", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10046, Name = @"Sport Gloves | Bronze Morph", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1408, Name = @"Sport Gloves | Creme Pinstripe", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 1409, Name = @"Sport Gloves | Red Racer", Enabled = true },
                    new GloveSkinConfig { Def = 5030, Paint = 10019, Name = @"Sport Gloves | Arid", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10026, Name = @"Moto Gloves | Spearmint", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10078, Name = @"Moto Gloves | Smoke Out", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10052, Name = @"Moto Gloves | Polygon", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10079, Name = @"Moto Gloves | Blood Pressure", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10049, Name = @"Moto Gloves | POW!", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10051, Name = @"Moto Gloves | Transport", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10050, Name = @"Moto Gloves | Turtle", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10077, Name = @"Moto Gloves | Finish Line", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10080, Name = @"Moto Gloves | 3rd Commando Company", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10028, Name = @"Moto Gloves | Cool Mint", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10024, Name = @"Moto Gloves | Eclipse", Enabled = true },
                    new GloveSkinConfig { Def = 5033, Paint = 10027, Name = @"Moto Gloves | Boom!", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10033, Name = @"Specialist Gloves | Crimson Kimono", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10034, Name = @"Specialist Gloves | Emerald Web", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1413, Name = @"Specialist Gloves | Marble Fade", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1438, Name = @"Specialist Gloves | Pillow Punchers", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1416, Name = @"Specialist Gloves | Tiger Strike", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10061, Name = @"Specialist Gloves | Crimson Web", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1440, Name = @"Specialist Gloves | Cloud Chaser", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1437, Name = @"Specialist Gloves | Big Swell", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10063, Name = @"Specialist Gloves | Fade", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1414, Name = @"Specialist Gloves | Blackbook", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10065, Name = @"Specialist Gloves | Marble Fade", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10064, Name = @"Specialist Gloves | Mogul", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10068, Name = @"Specialist Gloves | Field Agent", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 1415, Name = @"Specialist Gloves | Chocolate Chesterfield", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10067, Name = @"Specialist Gloves | Tiger Strike", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10066, Name = @"Specialist Gloves | Lt. Commander", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10062, Name = @"Specialist Gloves | Buckshot", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10030, Name = @"Specialist Gloves | Forest DDPAT", Enabled = true },
                    new GloveSkinConfig { Def = 5034, Paint = 10035, Name = @"Specialist Gloves | Foundation", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1399, Name = @"Driver Gloves | Rezan the Red", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10016, Name = @"Driver Gloves | Crimson Weave", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1401, Name = @"Driver Gloves | Imperial Plaid", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10041, Name = @"Driver Gloves | King Snake", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1398, Name = @"Driver Gloves | Wave Chaser", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10070, Name = @"Driver Gloves | Snow Leopard", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10042, Name = @"Driver Gloves | Imperial Plaid", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10072, Name = @"Driver Gloves | Black Tie", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1400, Name = @"Driver Gloves | Brocade Flowers", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1404, Name = @"Driver Gloves | Seigaiha", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1402, Name = @"Driver Gloves | Garden", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1439, Name = @"Driver Gloves | Hand Sweaters", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10071, Name = @"Driver Gloves | Queen Jaguar", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 1412, Name = @"Driver Gloves | Plum Quill", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10069, Name = @"Driver Gloves | Rezan the Red", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10043, Name = @"Driver Gloves | Overtake", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10044, Name = @"Driver Gloves | Racing Green", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10013, Name = @"Driver Gloves | Lunar Weave", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10015, Name = @"Driver Gloves | Convoy", Enabled = true },
                    new GloveSkinConfig { Def = 5031, Paint = 10040, Name = @"Driver Gloves | Diamondback", Enabled = true }
                }
            },
            MusicKits = new MusicKitConfig
            {
                Enabled = true,
                Ids = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 15, 16, 17, 18, 19, 20, 22, 23, 24, 25, 26, 27, 28, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45 }
            },
            Models = new ModelConfig
            {
                Enabled = true,
                CtModels = new[] { @"agents\\models\\ctm_sas\\ctm_sas_variantf.vmdl", @"agents\\models\\ctm_sas\\ctm_sas_variantg.vmdl", @"agents\\models\\ctm_st6\\ctm_st6_variantg.vmdl", @"agents\\models\\ctm_st6\\ctm_st6_varianti.vmdl", @"agents\\models\\ctm_st6\\ctm_st6_variantj.vmdl", @"agents\\models\\ctm_st6\\ctm_st6_variantk.vmdl", @"agents\\models\\ctm_fbi\\ctm_fbi_variantf.vmdl", @"agents\\models\\ctm_fbi\\ctm_fbi_variantg.vmdl", @"agents\\models\\ctm_swat\\ctm_swat_variantj.vmdl", @"agents\\models\\ctm_gendarmerie\\ctm_gendarmerie_variante.vmdl" },
                TModels = new[] { @"agents\\models\\tm_professional\\tm_professional_varf.vmdl", @"agents\\models\\tm_professional\\tm_professional_varf1.vmdl", @"agents\\models\\tm_professional\\tm_professional_varf2.vmdl", @"agents\\models\\tm_professional\\tm_professional_varf3.vmdl", @"agents\\models\\tm_professional\\tm_professional_varf4.vmdl", @"agents\\models\\tm_professional\\tm_professional_varf5.vmdl", @"agents\\models\\tm_professional\\tm_professional_varg.vmdl", @"agents\\models\\tm_professional\\tm_professional_varh.vmdl", @"agents\\models\\tm_jungle_raider\\tm_jungle_raider_variantf.vmdl", @"agents\\models\\tm_leet\\tm_leet_varianti.vmdl" }
            },
            Agents = new AgentSkinConfig
            {
                Enabled = true,
                Chance = 1.0
            },
            Stickers = new StickerSkinConfig
            {
                Enabled = true,
                Kato14Min = 48,
                Kato14Max = 172,
                Kato14Rate = 0.6,
                NormalMin = 3000,
                NormalMax = 5000
            },
            StatTrak = new StatTrakSkinConfig
            {
                Enabled = true,
                Chance = 0.6,
                CounterMin = 1,
                CounterMax = 10001,
                WeaponPaints = new Dictionary<string, int[]>
                {
                    ["61"] = new[] { 60, 115, 183, 217, 221, 277, 290, 313, 339, 489, 504, 540, 637, 653, 657, 705, 817, 991, 1040, 1102, 1136, 1142, 1173, 1186, 1431 },
                    ["4"] = new[] { 48, 129, 230, 278, 353, 381, 399, 479, 495, 532, 586, 607, 623, 680, 694, 713, 808, 918, 957, 963, 988, 1039, 1100, 1158, 1167, 1208, 1227, 1348, 1421 },
                    ["1"] = new[] { 61, 114, 185, 231, 232, 273, 351, 397, 425, 509, 527, 603, 645, 711, 805, 841, 945, 962, 1050, 1090, 1189, 1430 },
                    ["2"] = new[] { 112, 190, 220, 261, 276, 307, 396, 491, 528, 544, 625, 658, 710, 895, 903, 978, 1091, 1126, 1156, 1169, 1347 },
                    ["3"] = new[] { 44, 223, 265, 274, 352, 387, 427, 510, 530, 585, 605, 646, 660, 693, 837, 906, 979, 1093, 1128, 1168, 1429 },
                    ["7"] = new[] { 14, 44, 113, 180, 226, 282, 302, 316, 380, 394, 422, 474, 490, 506, 524, 600, 639, 656, 675, 707, 801, 836, 885, 941, 959, 1035, 1087, 1141, 1143, 1171, 1207, 1221, 1352, 1425 },
                    ["16"] = new[] { 118, 155, 176, 187, 215, 255, 309, 336, 384, 400, 480, 512, 533, 588, 632, 664, 695, 811, 844, 971, 985, 1041, 1097, 1149, 1165, 1210, 1228, 1353, 1432 },
                    ["60"] = new[] { 1433, 1166, 984, 106, 1340, 714, 497, 644, 430, 1130, 383, 360, 946, 587, 548, 301, 681, 1223, 257, 631, 60, 217, 663, 189 },
                    ["9"] = new[] { 51, 174, 181, 212, 227, 259, 279, 395, 424, 475, 525, 584, 640, 662, 691, 718, 803, 838, 887, 917, 943, 975, 1144, 1170, 1206, 1222, 1346, 1422 },
                    ["39"] = new[] { 98, 186, 287, 487, 519, 553, 598, 613, 686, 702, 815, 897, 955, 966, 1048, 1151, 1234 },
                    ["8"] = new[] { 9, 73, 121, 280, 305, 507, 541, 583, 601, 674, 690, 708, 845, 886, 913, 942, 1088, 1339 },
                    ["33"] = new[] { 11, 213, 354, 423, 481, 500, 536, 627, 649, 696, 719, 847, 893, 1096, 1133, 1163, 1354, 1436 },
                    ["13"] = new[] { 83, 192, 216, 264, 308, 398, 428, 478, 494, 546, 629, 647, 661, 807, 842, 972, 981, 1038, 1147, 1185, 1434 },
                    ["10"] = new[] { 154, 178, 218, 260, 288, 429, 477, 492, 529, 604, 626, 659, 723, 835, 904, 919, 1092, 1127, 1146, 1184 },
                    ["23"] = new[] { 810, 846, 888, 915, 949, 974, 986, 1137, 1180, 1231, 1344 },
                    ["24"] = new[] { 131, 193, 281, 362, 392, 436, 488, 556, 615, 652, 672, 688, 704, 802, 851, 916, 990, 1049, 1157, 1175, 1194, 1236, 1351, 1426 },
                    ["19"] = new[] { 20, 67, 127, 156, 182, 228, 283, 311, 335, 359, 486, 516, 593, 611, 636, 669, 717, 849, 911, 969, 977, 1154, 1190, 1233, 1419 },
                    ["29"] = new[] { 83, 256, 390, 405, 434, 517, 552, 596, 638, 655, 673, 720, 814, 953, 1140, 1155, 1160, 1427 },
                    ["27"] = new[] { 177, 291, 385, 431, 499, 535, 608, 633, 666, 703, 909, 948, 961, 1089, 1132, 1188, 1220, 1355 },
                    ["26"] = new[] { 13, 224, 267, 306, 349, 508, 526, 542, 594, 641, 676, 692, 884, 973, 1099, 1125, 1418 },
                    ["25"] = new[] { 314, 320, 393, 407, 505, 521, 557, 616, 654, 689, 706, 850, 970, 1046, 1103, 1135, 1174, 1182 },
                    ["35"] = new[] { 62, 191, 214, 225, 263, 286, 356, 484, 537, 590, 634, 699, 716, 809, 890, 987, 1051, 1162, 1192, 1350 },
                    ["14"] = new[] { 120, 266, 401, 496, 547, 648, 900, 902, 983, 1042, 1148, 1435 },
                    ["28"] = new[] { 285, 317, 355, 432, 483, 514, 610, 698, 950, 958, 1043, 1152 },
                    ["36"] = new[] { 125, 130, 162, 219, 230, 258, 271, 358, 388, 404, 426, 501, 551, 592, 650, 668, 678, 813, 848, 907, 968, 982, 1044, 1153, 1230, 1345, 1420 },
                    ["63"] = new[] { 12, 218, 268, 269, 270, 315, 334, 350, 435, 476, 543, 602, 622, 643, 687, 709, 944, 976, 1036 },
                    ["30"] = new[] { 216, 272, 289, 303, 520, 539, 555, 599, 614, 671, 684, 722, 816, 839, 889, 905, 964, 1159, 1235 },
                    ["32"] = new[] { 184, 211, 275, 338, 357, 389, 485, 515, 550, 591, 635, 667, 700, 894, 951, 960, 1138, 1181, 1224, 1342 },
                    ["34"] = new[] { 61, 262, 386, 403, 482, 549, 609, 630, 679, 697, 715, 804, 910, 1037, 1094, 1134, 1193, 1225, 1341, 1423 },
                    ["17"] = new[] { 98, 126, 188, 284, 310, 337, 402, 433, 498, 534, 589, 651, 665, 682, 812, 840, 898, 908, 947, 965, 1045, 1098, 1131, 1150, 1164, 1229, 1349 },
                    ["38"] = new[] { 117, 232, 312, 391, 406, 502, 518, 597, 612, 642, 685, 896, 914, 954, 1139, 1226, 1343 },
                    ["40"] = new[] { 60, 128, 222, 304, 361, 503, 538, 554, 624, 670, 899, 956, 967, 989, 1101, 1161, 1187 },
                    ["64"] = new[] { 12, 123, 522, 595, 683, 701, 721, 843, 892, 952, 1047, 1145, 1232 },
                    ["11"] = new[] { 195, 229, 382, 493, 511, 545, 606, 628, 677, 712, 806, 891, 980, 1095, 1129 },
                }
            },
            Souvenir = new SouvenirConfig
            {
                Enabled = true,
                Chance = 0.6,
            }            
        };
    }

    private static string GetCsgoDirectory()
    {
        string gameDir = Server.GameDirectory;
        if (Path.GetFileName(gameDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .Equals("csgo", StringComparison.OrdinalIgnoreCase))
        {
            return gameDir;
        }
        string csgoDir = Path.Combine(gameDir, "csgo");
        if (Directory.Exists(csgoDir))
        {
            return csgoDir;
        }
        return gameDir;
    }

    private static string GetBotInventoryConfigDirectory()
    {
        return Path.Combine(
            GetCsgoDirectory(),
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            "Botinventory"
        );
    }

    private static string GetBotInventoryConfigPath()
    {
        return Path.Combine(GetBotInventoryConfigDirectory(), BOT_INVENTORY_CONFIG_FILE);
    }

    private static void EnsureBotInventoryConfigLoaded()
    {
        if (_botInventoryConfigLoaded)
            return;
        LoadBotInventoryConfig();
        _botInventoryConfigLoaded = true;
    }

    public static void InitBotInventoryConfig()
    {
        EnsureBotInventoryConfigLoaded();
    }

    public static void ReloadBotInventoryConfig()
    {
        LoadBotInventoryConfig();
        _botInventoryConfigLoaded = true;
    }

    private static void LoadBotInventoryConfig()
    {
        try
        {
            string dir = GetBotInventoryConfigDirectory();
            Directory.CreateDirectory(dir);
            string path = GetBotInventoryConfigPath();
            if (!File.Exists(path))
            {
                _botInventoryConfig = CreateDefaultBotInventoryConfig();
                File.WriteAllText(
                    path,
                    SerializeBotInventoryConfigForFile(_botInventoryConfig)
                );
                CSS.Plugin.Logger.LogInformation($"BotInventory config created: {path}");
            }
            else
            {
                string json = File.ReadAllText(path);
                _botInventoryConfig =
                    JsonSerializer.Deserialize<BotInventoryConfig>(json, _botInventoryJsonOptions)
                    ?? CreateDefaultBotInventoryConfig();
            }
            NormalizeBotInventoryConfig();
            File.WriteAllText(path, SerializeBotInventoryConfigForFile(_botInventoryConfig));
            CSS.Plugin.Logger.LogInformation(
                $"BotInventory config loaded. Weapons={_botInventoryConfig.Weapons.PaintKits.Count}, Knives={_botInventoryConfig.Knives.Items.Count}, Gloves={_botInventoryConfig.Gloves.Items.Count}, MusicKits={_botInventoryConfig.MusicKits.Ids.Length}, StatTrakChance={_botInventoryConfig.StatTrak.Chance}"
            );
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError($"Failed to load BotInventory.json: {ex.Message}");
            _botInventoryConfig = CreateDefaultBotInventoryConfig();
            NormalizeBotInventoryConfig();
        }
    }

    private static string SerializeBotInventoryConfigForFile(BotInventoryConfig config)
    {
        string json = JsonSerializer.Serialize(config, _botInventoryJsonOptions);
        using var document = JsonDocument.Parse(json);
        var builder = new StringBuilder();
        WriteJsonElement(document.RootElement, builder, 0);
        builder.AppendLine();
        return builder.ToString();
    }

    private static void WriteJsonElement(JsonElement element, StringBuilder builder, int indentLevel)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteJsonObject(element, builder, indentLevel);
                break;
            case JsonValueKind.Array:
                WriteJsonArray(element, builder, indentLevel);
                break;
            case JsonValueKind.String:
                builder.Append(JsonSerializer.Serialize(element.GetString()));
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                builder.Append(element.GetRawText());
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    private static void WriteJsonObject(JsonElement element, StringBuilder builder, int indentLevel)
    {
        builder.Append('{');
        int count = 0;
        foreach (var _ in element.EnumerateObject())
            count++;
        if (count == 0)
        {
            builder.Append('}');
            return;
        }
        builder.AppendLine();
        int propertyIndex = 0;
        foreach (var property in element.EnumerateObject())
        {
            AppendIndent(builder, indentLevel + 1);
            builder.Append(JsonSerializer.Serialize(property.Name));
            builder.Append(": ");
            WriteJsonElement(property.Value, builder, indentLevel + 1);
            propertyIndex++;
            if (propertyIndex < count)
                builder.Append(',');
            builder.AppendLine();
        }
        AppendIndent(builder, indentLevel);
        builder.Append('}');
    }

    private static void WriteJsonArray(JsonElement element, StringBuilder builder, int indentLevel)
    {
        if (IsSimpleArray(element))
        {
            builder.Append('[');
            int simpleArrayIndex = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (simpleArrayIndex > 0)
                    builder.Append(", ");
                WriteJsonElement(item, builder, indentLevel);
                simpleArrayIndex++;
            }
            builder.Append(']');
            return;
        }
        builder.Append('[');
        int count = 0;
        foreach (var _ in element.EnumerateArray())
            count++;
        if (count == 0)
        {
            builder.Append(']');
            return;
        }
        builder.AppendLine();
        int arrayIndex = 0;
        foreach (var item in element.EnumerateArray())
        {
            AppendIndent(builder, indentLevel + 1);
            WriteJsonElement(item, builder, indentLevel + 1);
            arrayIndex++;
            if (arrayIndex < count)
                builder.Append(',');
            builder.AppendLine();
        }
        AppendIndent(builder, indentLevel);
        builder.Append(']');
    }

    private static bool IsSimpleArray(JsonElement element)
    {
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                return false;
        }
        return true;
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }

    private static void NormalizeBotInventoryConfig()
    {
        _botInventoryConfig.Weapons ??= new WeaponSkinConfig();
        _botInventoryConfig.Weapons.PaintKits ??= new Dictionary<string, int[]>();
        _botInventoryConfig.Knives ??= new KnifeSkinSection();
        _botInventoryConfig.Knives.Items ??= new List<KnifeSkinConfig>();
        _botInventoryConfig.Gloves ??= new GloveSkinSection();
        _botInventoryConfig.Gloves.Items ??= new List<GloveSkinConfig>();
        _botInventoryConfig.MusicKits ??= new MusicKitConfig();
        _botInventoryConfig.MusicKits.Ids ??= Array.Empty<int>();
        _botInventoryConfig.Models ??= new ModelConfig();
        _botInventoryConfig.Models.CtModels ??= Array.Empty<string>();
        _botInventoryConfig.Models.TModels ??= Array.Empty<string>();
        _botInventoryConfig.Agents ??= new AgentSkinConfig();
        _botInventoryConfig.Agents.CtDefIndexes ??= Array.Empty<int>();
        _botInventoryConfig.Agents.TDefIndexes ??= Array.Empty<int>();

        // dev1.4.4 test instructions temporarily used one fixed CT and one fixed T agent.
        // Automatically restore the normal full random pools only for that exact known test pair,
        // so users upgrading from the test build do not keep seeing every T as Number K.
        bool isOldFixedVoiceTest = _botInventoryConfig.Agents.CtDefIndexes.SequenceEqual(new[] { 5308 })
            && (_botInventoryConfig.Agents.TDefIndexes.SequenceEqual(new[] { 4726 })
                || _botInventoryConfig.Agents.TDefIndexes.SequenceEqual(new[] { 4732 }));

        if (isOldFixedVoiceTest)
        {
            var defaults = new AgentSkinConfig();
            _botInventoryConfig.Agents.CtDefIndexes = defaults.CtDefIndexes;
            _botInventoryConfig.Agents.TDefIndexes = defaults.TDefIndexes;
            CSS.Plugin.Logger.LogInformation(
                "BotInventory replaced the old fixed Ava/T-agent voice-test pools with the normal random pools."
            );
        }

        _botInventoryConfig.Stickers ??= new StickerSkinConfig();
        _botInventoryConfig.StatTrak ??= new StatTrakSkinConfig();
        _botInventoryConfig.StatTrak.WeaponPaints ??= new Dictionary<string, int[]>();
    }

    private static float RandomWear(float maxWear)
    {
        maxWear = Math.Clamp(maxWear, 0.0f, 1.0f);
        return (float)_random.NextDouble() * maxWear;
    }

    private static bool ContainsPaintKit(Dictionary<string, int[]> source, ushort defIndex, int paintKit)
    {
        if (!source.TryGetValue(defIndex.ToString(), out int[]? paintKits))
            return false;
        return paintKits != null && Array.IndexOf(paintKits, paintKit) >= 0;
    }

    private static int? CreateStatTrakValue(ushort defIndex, int paintKit)
    {
        EnsureBotInventoryConfigLoaded();
        var statTrak = _botInventoryConfig.StatTrak;

        if (!statTrak.Enabled)
            return null;

        if (!ContainsPaintKit(statTrak.WeaponPaints, defIndex, paintKit))
            return null;

        double chance = Math.Clamp(statTrak.Chance, 0.0, 1.0);

        if (_random.NextDouble() > chance)
            return null;

        int min = Math.Min(statTrak.CounterMin, statTrak.CounterMax);
        int max = Math.Max(statTrak.CounterMin, statTrak.CounterMax);

        return _random.Next(min, max + 1);
    }

    private static bool ShouldUseSouvenir()
    {
        var souvenir = _botInventoryConfig.Souvenir;

        if (!souvenir.Enabled)
            return false;

        double chance = Math.Clamp(souvenir.Chance, 0.0, 1.0);

        return _random.NextDouble() < chance;
    }

    public static bool ShouldApplyToPlayers()
    {
        // Disabled: inventories are applied to bots only.
        //return false;

        EnsureBotInventoryConfigLoaded();
        return _botInventoryConfig.ApplyToPlayers;
    }

    public static bool TryGetRandomBotGlove(Random rng, out BotGloveSkin glove)
    {
        EnsureBotInventoryConfigLoaded();
        glove = default;
        var gloves = _botInventoryConfig.Gloves;
        if (!gloves.Enabled)
            return false;
        var enabledGloves = gloves.Items
            .Where(g => g.Enabled && g.Def > 0 && g.Paint >= 0)
            .ToArray();
        if (enabledGloves.Length == 0)
            return false;
        var selected = enabledGloves[rng.Next(enabledGloves.Length)];
        string name = string.IsNullOrWhiteSpace(selected.Name)
            ? $"Glove {selected.Def} | {selected.Paint}"
            : selected.Name;
        glove = new BotGloveSkin((ushort)selected.Def, selected.Paint, name);
        return true;
    }

    public static int CreateBotGloveSeed(Random rng)
    {
        EnsureBotInventoryConfigLoaded();
        int minSeed = Math.Min(_botInventoryConfig.Gloves.MinSeed, _botInventoryConfig.Gloves.MaxSeed);
        int maxSeed = Math.Max(_botInventoryConfig.Gloves.MinSeed, _botInventoryConfig.Gloves.MaxSeed);
        return rng.Next(minSeed, maxSeed + 1);
    }

    public static float CreateBotGloveWear(Random rng)
    {
        EnsureBotInventoryConfigLoaded();
        float maxWear = Math.Clamp(_botInventoryConfig.Gloves.MaxWear, 0.0f, 1.0f);
        return (float)(rng.NextDouble() * maxWear);
    }

    public static bool TryGetRandomBotMusicKit(Random rng, out int kitId)
    {
        EnsureBotInventoryConfigLoaded();
        kitId = 0;
        var musicKits = _botInventoryConfig.MusicKits;
        if (!musicKits.Enabled || musicKits.Ids.Length == 0)
            return false;
        kitId = musicKits.Ids[rng.Next(musicKits.Ids.Length)];
        return true;
    }

    public static string[] GetBotCtModels()
    {
        EnsureBotInventoryConfigLoaded();
        if (!_botInventoryConfig.Models.Enabled)
            return Array.Empty<string>();
        return _botInventoryConfig.Models.CtModels
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();
    }

    public static string[] GetBotTModels()
    {
        EnsureBotInventoryConfigLoaded();
        if (!_botInventoryConfig.Models.Enabled)
            return Array.Empty<string>();
        return _botInventoryConfig.Models.TModels
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();
    }

    public static bool AreBotAgentsEnabled()
    {
        EnsureBotInventoryConfigLoaded();
        return _botInventoryConfig.Agents.Enabled;
    }

    public static void ReleaseAgentAssignment(ulong ownerKey)
    {
        lock (_randomLock)
            _agentAssignments.Remove(ownerKey);
    }

    public static void ClearAgentAssignments()
    {
        lock (_randomLock)
            _agentAssignments.Clear();
    }

    // loadup
    public static bool Load(string filename)
    {
        try
        {
            EnsureBotInventoryConfigLoaded();
            var candidates = new[]
            {
                Path.Combine(Server.GameDirectory, _inventoryFileDir, filename),
                Path.Combine(Server.GameDirectory, "csgo", filename),
            };
            var path = candidates.FirstOrDefault(File.Exists) ??
                       (Path.IsPathRooted(filename) && File.Exists(filename) ? filename : null);
            if (path == null)
                return false;
            string json = File.ReadAllText(path);
            var inventories = JsonSerializer.Deserialize<Dictionary<ulong, PlayerInventory>>(json);
            if (inventories != null)
            {
                _loadedInventories.Clear();
                foreach (var pair in inventories)
                    _loadedInventories.TryAdd(pair.Key, pair.Value);
            }
            return true;
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError($"Error loading inventory file \"{filename}\": {ex.Message}");
            return false;
        }
    }

    // randomly hand out
    private static bool IsHost(ulong steamId)
    {
        return _hostSteamId.HasValue && steamId == _hostSteamId.Value;
    }

    public static bool Has(ulong steamId)
    {
        return false;
    }

    private static ulong GetInventoryOwnerKey(ulong steamId, CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return steamId;

        if (player.IsBot)
            return 0xB070000000000000UL | player.Index;

        return steamId != 0
            ? steamId
            : 0xA110000000000000UL | player.Index;
    }

public static bool TryGet(ulong steamId, [MaybeNullWhen(false)] out PlayerInventory inventory, CCSPlayerController? player = null)
{
    inventory = null;
    if (IsHost(steamId))
        return false;
    if (!IsBotPlayer(player))
        return false;

    try
    {
        ulong ownerKey = GetInventoryOwnerKey(steamId, player);
        inventory = GenerateHighTierRandomInventory(steamId, ownerKey);
        return true;
    }
    catch (Exception ex)
    {
        CSS.Plugin.Logger.LogError($"Failed to generate random inventory for {steamId}: {ex.Message}");
        return false;
    }
}

    public static PlayerInventory? Get(ulong steamId)
    {
        try
        {
            return GenerateHighTierRandomInventory(steamId, steamId);
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError($"Failed to get random inventory for {steamId}: {ex.Message}");
            return null;
        }
    }

    // stickers hand out
    private static uint GetRandomStickerId()
    {
        EnsureBotInventoryConfigLoaded();
        var stickerConfig = _botInventoryConfig.Stickers;
        double roll = _rng.NextDouble();
        if (roll < stickerConfig.Kato14Rate)
        {
            return (uint)_rng.Next(stickerConfig.Kato14Min, stickerConfig.Kato14Max + 1);
        }
        else
        {
            return (uint)_rng.Next(stickerConfig.NormalMin, stickerConfig.NormalMax + 1);
        }
    }

    // posibilities
    private static List<StickerItem> GenerateFullStickers()
    {
        if (!_botInventoryConfig.Stickers.Enabled)
        {
            return new List<StickerItem>();
        }        
        var stickers = new List<StickerItem>();
        // slots
        uint tripleId = GetRandomStickerId();
        for (int i = 0; i < 3; i++)
        {
            stickers.Add(new StickerItem
            {
                Def = tripleId,
                Slot = (uint)i,
                Wear = 0.0f,
                Rotation = 0,
                X = null,
                Y = null
            });
        }
        // random slots
        for (int i = 3; i < 5; i++)
        {
            if (_rng.NextDouble() < 0.5)
            {
                stickers.Add(new StickerItem
                {
                    Def = GetRandomStickerId(),
                    Slot = (uint)i,
                    Wear = 0.0f,
                    Rotation = 0,
                    X = null,
                    Y = null
                });
            }
        }
        return stickers;
    }

    private static AgentAssignment GetOrCreateAgentAssignment(
        ulong ownerKey,
        double agentChance,
        int[] ctAgentDefs,
        int[] tAgentDefs
    )
    {
        if (_agentAssignments.TryGetValue(ownerKey, out var existing))
            return existing;

        bool useSpecialAgent = _random.NextDouble() < agentChance;
        var assignment = new AgentAssignment
        {
            CtDef = useSpecialAgent
                ? SelectUnusedAgentDef(ctAgentDefs, TEAM_CT, 5037)
                : (ushort)5037,
            TDef = useSpecialAgent
                ? SelectUnusedAgentDef(tAgentDefs, TEAM_T, 5036)
                : (ushort)5036
        };

        _agentAssignments[ownerKey] = assignment;
        return assignment;
    }

    private static ushort SelectUnusedAgentDef(int[] pool, byte team, ushort fallback)
    {
        if (pool.Length == 0)
            return fallback;

        var used = new HashSet<ushort>();
        foreach (var assignment in _agentAssignments.Values)
            used.Add(team == TEAM_CT ? assignment.CtDef : assignment.TDef);

        int[] available = pool
            .Where(def => !used.Contains(checked((ushort)def)))
            .ToArray();
        int[] source = available.Length > 0 ? available : pool;
        return checked((ushort)source[_random.Next(source.Length)]);
    }

    private static PlayerInventory GenerateHighTierRandomInventory(ulong steamId, ulong ownerKey)
    {
        lock (_randomLock)
        {
            var data = new EquippedV4Response();
            data.CTWeapons = new Dictionary<ushort, InventoryItem>();
            data.TWeapons = new Dictionary<ushort, InventoryItem>();
            data.Knives = new Dictionary<byte, InventoryItem>();
            data.Gloves = new Dictionary<byte, InventoryItem>();
            data.Agents = new Dictionary<byte, InventoryItem>();
            data.MusicKit = null;
            data.Graffiti = null;
            
            EnsureBotInventoryConfigLoaded();
            if (_botInventoryConfig.Weapons.Enabled)
            {
                foreach (var weapon in _botInventoryConfig.Weapons.PaintKits)
                {
                    if (!ushort.TryParse(weapon.Key, out ushort defIndex))
                        continue;
                    int[] paintIds = weapon.Value ?? Array.Empty<int>();
                    if (paintIds.Length == 0)
                        continue;
                    int selectedPaint = paintIds[_random.Next(paintIds.Length)];
                    bool isStatTrakPool = ContainsPaintKit(_botInventoryConfig.StatTrak.WeaponPaints,defIndex,selectedPaint);

                    int? statTrakValue = null;
                    bool souvenir = false;

                    if (isStatTrakPool)
                    {
                        statTrakValue = CreateStatTrakValue(defIndex, selectedPaint);
                    }

                    // 只要没有生成ST，都继续参与Souvenir抽奖
                    if (statTrakValue == null)
                    {
                        souvenir = ShouldUseSouvenir();
                    }

                    var randomItem = new InventoryItem
                    {
                        Def = defIndex,
                        Paint = selectedPaint,
                        Wear = RandomWear(_botInventoryConfig.Weapons.MaxWear),
                        Seed = _random.Next(_botInventoryConfig.Weapons.MinSeed, _botInventoryConfig.Weapons.MaxSeed + 1),
                        Stickers = GenerateFullStickers(),
                        Stattrak = statTrakValue,
                        Souvenir = souvenir
                    };
                    data.CTWeapons[defIndex] = randomItem;
                    data.TWeapons[defIndex] = randomItem;
                }
            }
            if (_botInventoryConfig.Knives.Enabled)
            {
                var enabledKnives = _botInventoryConfig.Knives.Items
                    .Where(k => k.Enabled && k.PaintKits != null && k.PaintKits.Length > 0)
                    .ToArray();
                if (enabledKnives.Length > 0)
                {
                    var selectedKnifeCT = enabledKnives[_random.Next(enabledKnives.Length)];
                    int knifePaintCT = selectedKnifeCT.PaintKits[_random.Next(selectedKnifeCT.PaintKits.Length)];

                    var ctKnifeItem = new InventoryItem
                    {
                        Def = (ushort)selectedKnifeCT.Def,
                        Paint = knifePaintCT,
                        Wear = RandomWear(_botInventoryConfig.Knives.MaxWear),
                        Seed = _random.Next(1, 10000),
                    };

                    var selectedKnifeT = enabledKnives[_random.Next(enabledKnives.Length)];
                    int knifePaintT = selectedKnifeT.PaintKits[_random.Next(selectedKnifeT.PaintKits.Length)];

                    var tKnifeItem = new InventoryItem
                    {
                        Def = (ushort)selectedKnifeT.Def,
                        Paint = knifePaintT,
                        Wear = RandomWear(_botInventoryConfig.Knives.MaxWear),
                        Seed = _random.Next(1, 10000),
                    };

                    data.Knives[TEAM_CT] = ctKnifeItem;
                    data.Knives[TEAM_T] = tKnifeItem;
                }
            }
            if (_botInventoryConfig.Gloves.Enabled)
            {
                var enabledGloves = _botInventoryConfig.Gloves.Items
                    .Where(g => g.Enabled && g.Def > 0 && g.Paint >= 0)
                    .ToArray();

                if (enabledGloves.Length > 0)
                {
                    var selectedGlove = enabledGloves[_random.Next(enabledGloves.Length)];
                    var gloveItem = new InventoryItem
                    {
                        Def = (ushort)selectedGlove.Def,
                        Paint = selectedGlove.Paint,
                        Wear = RandomWear(_botInventoryConfig.Gloves.MaxWear),
                        Seed = _random.Next(
                            _botInventoryConfig.Gloves.MinSeed,
                            _botInventoryConfig.Gloves.MaxSeed + 1
                        )
                    };
                    data.Gloves[TEAM_CT] = gloveItem;
                    data.Gloves[TEAM_T] = gloveItem;
                }
            }                                  
            if (_botInventoryConfig.Agents.Enabled)
            {
                double agentChance = Math.Clamp(_botInventoryConfig.Agents.Chance, 0.0, 1.0);
                int[] ctAgentDefs = _botInventoryConfig.Agents.CtDefIndexes
                    .Where(def => def > 0
                        && def <= ushort.MaxValue
                        && AgentCatalog.IsValidForTeam((ushort)def, TEAM_CT))
                    .Distinct()
                    .ToArray();
                int[] tAgentDefs = _botInventoryConfig.Agents.TDefIndexes
                    .Where(def => def > 0
                        && def <= ushort.MaxValue
                        && AgentCatalog.IsValidForTeam((ushort)def, TEAM_T))
                    .Distinct()
                    .ToArray();

                AgentAssignment assignment = GetOrCreateAgentAssignment(
                    ownerKey,
                    agentChance,
                    ctAgentDefs,
                    tAgentDefs
                );

                data.Agents[TEAM_CT] = new InventoryItem { Def = assignment.CtDef };
                data.Agents[TEAM_T] = new InventoryItem { Def = assignment.TDef };
            }
            return new PlayerInventory(data);
        }
    }
}