using BotHiderApi;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace InventorySimulator;

public static class Inventories
{
    private static readonly PluginCapability<IBotHiderApi> _botHiderCapability =
        new("bothider:api");
    private static IBotHiderApi? _botHiderApi;
    private static readonly Dictionary<ulong, PlayerInventory> _loadedInventories = [];
    private static readonly string _inventoryFileDir =
        "csgo/addons/counterstrikesharp/configs/plugins/InventorySimulator";
    private static readonly Random _rng = new Random();


    
    private const byte TEAM_CT = 3;
    private const byte TEAM_T = 2;

    private static readonly Random _random = new();
    private static readonly object _randomLock = new();
    private static ulong _hostSteamId = null
    private const int KATO14_MIN = 48;
    private const int KATO14_MAX = 172;
    private const double KATO14_RATE = 0.6;
     //stickers
    private const int NORMAL_MIN = 3000;
    private const int NORMAL_MAX = 5000;


   
    private static bool IsBotPlayer(CCSPlayerController? c)
        => c != null && (c.IsBot || (_botHiderApi?.IsManagedBot(c.Slot) ?? false));
      //weapon paint
    private static readonly (int def, int[] paintIds)[] _weapons = new[]
{
    (61, new int[] { 504, 1142, 332, 1065, 318, 443, 454, 489, 504, 540, 637, 653, 657, 25, 60, 115, 183, 217, 221, 236, 277, 290, 313, 318, 332, 339, 364, 443, 454, 489, 504, 540, 637, 653, 657, 705, 796, 817, 818, 830, 922, 991, 1027, 1031, 1040, 1065, 1102, 1136, 1142, 1173, 1186, 1217, 1253, 1284, 1323, 1377 }),

    (4, new int[] { 38, 694, 799, 48, 1119, 1120, 129, 152, 159, 208, 230, 278, 293, 353, 367, 381, 399, 437, 479, 495, 532, 586, 607, 623, 680, 694, 713, 732, 789, 2, 3, 38, 40, 48, 84, 129, 152, 159, 208, 230, 278, 293, 353, 367, 381, 399, 437, 479, 495, 532, 586, 607, 623, 680, 694, 713, 732, 789, 799, 808, 832, 918, 957, 963, 988, 1016, 1039, 1079, 1100, 1119, 1120, 1121, 1122, 1123, 1158, 1167, 1200, 1208, 1227, 1240, 1265, 1282, 1312, 1348, 1357 }),

    (1, new int[] { 37, 962, 328, 757, 17, 37, 40, 61, 90, 114, 138, 185, 231, 232, 237, 273, 296, 328, 347, 351, 397, 425, 468, 469, 470, 509, 527, 603, 645, 711, 757, 764, 805, 841, 938, 945, 962, 992, 1006, 1050, 1054, 1056, 1090, 1189, 1257, 1318, 1360 }),

    (2, new int[] { 28, 43, 46, 47, 112, 139, 153, 190, 220, 249, 261, 276, 307, 330, 396, 447, 450, 453, 491, 528, 544, 625, 658, 710, 747, 824, 860, 895, 903, 978, 998, 1005, 1086, 1091, 1126, 1156, 1169, 1263, 1290, 1335, 1347, 1373 }),

    (3, new int[] { 3, 44, 46, 78, 141, 151, 210, 223, 252, 254, 265, 274, 352, 377, 387, 427, 464, 510, 530, 585, 605, 646, 660, 693, 729, 784, 831, 837, 906, 932, 979, 1002, 1062, 1082, 1093, 1128, 1168, 1262, 1336, 1380 }),

    (7, new int[] { 1171, 456, 44, 941, 959, 801, 282, 302, 490, 724, 707, 180, 639, 524, 14, 44, 72, 113, 122, 142, 170, 172, 180, 226, 282, 300, 302, 316, 340, 341, 380, 394, 422, 456, 474, 490, 506, 524, 600, 639, 656, 675, 707, 724, 745, 795, 801, 836, 885, 912, 921, 941, 959, 1004, 1018, 1035, 1070, 1087, 1141, 1143, 1171, 1179, 1207, 1218, 1221, 1238, 1283, 1288, 1309, 1352, 1358, 1397 }),

    (16, new int[] { 255, 309, 1228, 1255, 8, 16, 17, 101, 118, 155, 164, 167, 176, 187, 215, 255, 309, 336, 384, 400, 449, 471, 480, 512, 533, 588, 632, 664, 695, 730, 780, 793, 811, 844, 874, 926, 971, 985, 993, 1041, 1063, 1097, 1149, 1165, 1209, 1210, 1228, 1255, 1266, 1281, 1313, 1353, 1364 }),

    (60, new int[] { 1177, 946, 714, 440, 984, 189, 1340, 60, 77, 106, 160, 189, 217, 235, 254, 257, 301, 321, 326, 360, 383, 430, 440, 445, 497, 548, 587, 631, 644, 663, 681, 714, 792, 862, 946, 984, 1001, 1017, 1059, 1073, 1130, 1166, 1177, 1216, 1223, 1243, 1311, 1319, 1338, 1340, 1376 }),

    (9, new int[] { 1026, 1206, 279, 1144, 259, 803, 838, 917, 344, 22, 47, 60, 92, 154, 178, 194, 218, 240, 244, 260, 288, 371, 429, 461, 477, 492, 529, 604, 626, 659, 723, 835, 863, 869, 882, 904, 919, 999, 1053, 1066, 1092, 1127, 1146, 1184, 1202, 1219, 1241, 1302, 1321, 1365, 1393 }),

    (39, new int[] { 487, 955, 901, 28, 39, 61, 98, 101, 136, 186, 243, 247, 287, 298, 363, 378, 487, 519, 553, 598, 613, 686, 702, 750, 765, 815, 861, 864, 897, 901, 934, 955, 966, 1022, 1048, 1084, 1151, 1234, 1270, 1320, 1394 }),

    (8, new int[] { 1033, 758, 9, 10, 33, 46, 47, 73, 100, 110, 121, 134, 173, 197, 246, 280, 305, 375, 444, 455, 507, 541, 583, 601, 674, 690, 708, 727, 740, 758, 779, 794, 823, 845, 886, 913, 927, 942, 995, 1033, 1088, 1198, 1249, 1308, 1339, 1362 }),

    (33, new int[] { 5, 11, 15, 28, 102, 141, 175, 209, 213, 245, 250, 354, 365, 423, 442, 481, 500, 536, 627, 649, 696, 719, 728, 752, 782, 847, 893, 935, 940, 1007, 1023, 1096, 1133, 1163, 1246, 1326, 1354, 1386 }),

    (13, new int[] { 246, 76, 83, 101, 119, 192, 216, 235, 237, 239, 241, 246, 264, 294, 297, 308, 379, 398, 428, 460, 478, 494, 546, 629, 647, 661, 790, 807, 842, 939, 972, 981, 1013, 1032, 1038, 1071, 1147, 1178, 1185, 1264, 1275, 1296, 1314, 1383 }),

    (10, new int[] { 22, 47, 60, 92, 154, 178, 194, 218, 240, 244, 260, 288, 371, 429, 461, 477, 492, 529, 604, 626, 659, 723, 835, 863, 869, 882, 904, 919, 999, 1053, 1066, 1092, 1127, 1146, 1184, 1202, 1219, 1241, 1302, 1321, 1365, 1393 }),

    (23, new int[] { 161, 753, 768, 781, 798, 800, 810, 846, 872, 888, 915, 923, 949, 974, 986, 1061, 1137, 1180, 1231, 1274, 1294, 1344, 1366, 1385 }),

    (24, new int[] { 15, 17, 37, 70, 90, 93, 131, 169, 175, 193, 250, 281, 333, 362, 392, 412, 436, 441, 488, 556, 615, 652, 672, 688, 704, 725, 778, 802, 851, 879, 916, 990, 1003, 1008, 1049, 1085, 1157, 1175, 1194, 1203, 1236, 1303, 1351, 1387 }),

    (19, new int[] { 20, 67, 100, 111, 124, 127, 133, 156, 169, 175, 182, 228, 234, 244, 283, 311, 335, 342, 359, 486, 516, 593, 611, 636, 669, 717, 726, 744, 759, 776, 828, 849, 911, 925, 936, 969, 977, 1000, 1015, 1020, 1074, 1154, 1190, 1199, 1233, 1250, 1256, 1277, 1291, 1332, 1361 }),

    (29, new int[] { 5, 30, 41, 83, 119, 171, 204, 246, 250, 256, 323, 345, 390, 405, 434, 458, 517, 552, 596, 638, 655, 673, 720, 797, 814, 870, 880, 953, 1014, 1140, 1155, 1160, 1272, 1391 }),

    (27, new int[] { 32, 34, 39, 70, 99, 100, 171, 177, 198, 291, 327, 385, 431, 462, 473, 499, 535, 608, 633, 666, 703, 737, 754, 773, 787, 822, 909, 948, 961, 1072, 1089, 1132, 1188, 1220, 1245, 1306, 1355 }),

    (26, new int[] { 3, 13, 25, 70, 148, 149, 159, 164, 171, 203, 224, 236, 267, 293, 306, 349, 376, 457, 508, 526, 542, 594, 641, 676, 692, 770, 775, 829, 873, 884, 973, 1083, 1099, 1125, 1325, 1374, 1392 }),

    (25, new int[] { 42, 95, 96, 135, 146, 166, 169, 205, 238, 240, 314, 320, 348, 370, 393, 407, 505, 521, 557, 616, 654, 689, 706, 731, 760, 821, 834, 850, 970, 994, 1021, 1046, 1078, 1103, 1135, 1174, 1182, 1201, 1215, 1254, 1267, 1287, 1333, 1381 }),

    (35, new int[] { 3, 25, 62, 99, 107, 145, 158, 164, 166, 170, 191, 214, 225, 248, 263, 286, 294, 298, 299, 323, 324, 356, 450, 484, 537, 590, 634, 699, 716, 746, 785, 809, 890, 929, 987, 1051, 1077, 1162, 1192, 1247, 1261, 1331, 1337, 1350, 1368 }),

    (14, new int[] { 22, 75, 120, 151, 170, 202, 243, 266, 401, 452, 472, 496, 547, 648, 827, 875, 900, 902, 933, 983, 1042, 1148, 1242, 1298, 1370 }),

    (28, new int[] { 28, 144, 201, 240, 285, 298, 317, 355, 369, 432, 483, 514, 610, 698, 763, 783, 920, 950, 958, 1012, 1043, 1080, 1152, 1260, 1300 }),

    (36, new int[] { 15, 27, 34, 77, 78, 99, 102, 125, 130, 162, 164, 168, 207, 219, 230, 258, 271, 295, 358, 373, 388, 404, 426, 466, 467, 501, 551, 592, 650, 668, 678, 741, 749, 774, 777, 786, 813, 825, 848, 907, 928, 968, 982, 1030, 1044, 1081, 1153, 1212, 1230, 1248, 1273, 1307, 1315, 1317, 1345, 1369 }),

    (63, new int[] { 12, 32, 147, 218, 268, 269, 270, 297, 298, 315, 322, 325, 333, 334, 350, 366, 435, 453, 476, 543, 602, 622, 643, 687, 709, 859, 933, 937, 944, 976, 1036, 1064, 1076, 1195, 1329, 1390 }),

    (30, new int[] { 2, 17, 36, 159, 179, 206, 216, 235, 242, 248, 272, 289, 303, 374, 439, 459, 463, 520, 539, 555, 599, 614, 671, 684, 722, 733, 738, 766, 791, 795, 816, 839, 889, 905, 964, 1010, 1024, 1159, 1214, 1235, 1252, 1279, 1286, 1299, 1322, 1384 }),

    (32, new int[] { 21, 32, 71, 95, 104, 184, 211, 246, 275, 327, 338, 346, 357, 389, 443, 485, 515, 550, 591, 635, 667, 700, 878, 894, 951, 960, 997, 1019, 1055, 1138, 1181, 1224, 1259, 1292, 1342, 1359 }),
};

    // knifes
    private static readonly (int def, string name, int[] paintIds)[] _knives = new[]
    {
        (500, "Bayonet", new int[] { 413, 420, 38, 417, 568 }),      
        (507, "Karambit", new int[] { 415, 572, 44, 42, 568 }),    
        (508, "M9 Bayonet", new int[] { 0, 417, 413, 418, 568 }),  
        (515, "Butterfly", new int[] { 619, 44, 572, 413, 415 }),   
        (522, "Stiletto", new int[] { 417, 420, 0, 418, 419 }),    
        (523, "Talon", new int[] { 0, 855, 38, 854, 853 }),       
        (525, "Skeleton", new int[] { 0, 413, 44, 415, 416 }),    
    };

    // gloves
    private static readonly (int def, int paint)[] _gloves = new[]
{
    (5030, 10037),  // Sport Gloves | Pandora's Box
    (5030, 10038),  // Specialist Gloves | Crimson Kimono
    (5030, 10048),  // Sport Gloves | Vice
    (5030, 10047),  // Driver Gloves | King Snake
    (5030, 10045),  // Moto Gloves | Smoke Out
};
   
    // loadup

    public static bool Load(string filename)
    {
        try
        {
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
    public static void ResolveBotHiderApi()
    {
        try { _botHiderApi = _botHiderCapability.Get(); }
        catch { _botHiderApi = null; }
    }
    // randomly hand out
    private static bool IsHost(ulong steamId)
    {
        return steamId == _hostSteamId;
    }


    public static bool Has(ulong steamId)
    {
        return false;
    }

    public static bool TryGet(ulong steamId, [MaybeNullWhen(false)] out PlayerInventory inventory, CCSPlayerController? player = null)
    {
        if (IsHost(steamId))
        {
            inventory = null;
            return false;
        }
        try
        {
            inventory = GenerateHighTierRandomInventory(steamId);
            return true;
        }
        catch (Exception ex)
        {
            CSS.Plugin.Logger.LogError($"Failed to generate random inventory for {steamId}: {ex.Message}");
            inventory = null;
            return false;
        }
    }

    public static PlayerInventory? Get(ulong steamId)
    {
        try
        {
            return GenerateHighTierRandomInventory(steamId);
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
        double roll = _rng.NextDouble();

        if (roll < KATO14_RATE)  // 60% 14katowince
        {
            return (uint)_rng.Next(KATO14_MIN, KATO14_MAX + 1);
        }
        else  // 40% normal
        {
            return (uint)_rng.Next(NORMAL_MIN, NORMAL_MAX + 1);
        }
    }

    // posibilities
    private static List<StickerItem> GenerateFullStickers()
    {
        var stickers = new List<StickerItem>();

        // slots
        uint tripleId = GetRandomStickerId();
        for (int i = 0; i < 3; i++)
        {
            stickers.Add(new StickerItem
            {
                Def = tripleId,
                Slot = (uint)i,      // 
                Wear = 0.0f,
                Rotation = 0,
                X = null,            // 
                Y = null             // 
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




    private static PlayerInventory GenerateHighTierRandomInventory(ulong steamId)
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

            
            foreach (var weapon in _weapons)
            {
                
                if (_random.NextDouble() > 0.95)
                    continue;

                
                int selectedPaint = weapon.paintIds[_random.Next(weapon.paintIds.Length)];

                var randomItem = new InventoryItem
                {
                    Def = (ushort)weapon.def,
                    Paint = selectedPaint,
                    Wear = (float)_random.NextDouble() * 0.5f,  
                    Seed = _random.Next(1, 10000),
                    Stickers = GenerateFullStickers()
                };

                data.CTWeapons[(ushort)weapon.def] = randomItem;
                data.TWeapons[(ushort)weapon.def] = randomItem;
            }

            
            var selectedKnife = _knives[_random.Next(_knives.Length)];
            int knifePaint = selectedKnife.paintIds[_random.Next(selectedKnife.paintIds.Length)];

            var knifeItem = new InventoryItem
            {
                Def = (ushort)selectedKnife.def,
                Paint = knifePaint,
                Wear = (float)_random.NextDouble() * 0.3f,
                Seed = _random.Next(1, 10000),
            };

            data.Knives[TEAM_CT] = knifeItem;
            data.Knives[TEAM_T] = knifeItem;

            CSS.Plugin.Logger.LogInformation($" 生成高品质刀: {selectedKnife.name}");

            // 
            if (_gloves.Length > 0)
            {
                var selectedGlove = _gloves[_random.Next(_gloves.Length)];

                var gloveItem = new InventoryItem
                {
                    Def = (ushort)selectedGlove.def,
                    Paint = selectedGlove.paint,
                    Wear = (float)_random.NextDouble() * 0.4f,
                };

                data.Gloves[TEAM_CT] = gloveItem;
                data.Gloves[TEAM_T] = gloveItem;
            }

            
            if (_random.NextDouble() < 0.9)
            {
                var agentItem = new InventoryItem
                {
                    Paint = _random.Next(1000, 1200),
                };
                data.Agents[TEAM_CT] = agentItem;
                data.Agents[TEAM_T] = agentItem;
            }

            return new PlayerInventory(data);
        }
    }
}