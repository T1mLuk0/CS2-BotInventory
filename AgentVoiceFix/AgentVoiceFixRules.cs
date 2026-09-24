namespace AgentVoiceFix;

internal static class AgentVoiceFixRules
{
    public static Dictionary<string, string[]> CreateDefaultExactFallbacks() =>
        new(StringComparer.Ordinal)
        {
            ["balkan_epic.planted_b_04"] = [ "balkan_epic.planted_b_01", "balkan_epic.planted_b_02", "balkan_epic.planted_b_03" ],
            ["gendarmerie_fem_epic.ff2_defusing_05"] = [ "gendarmerie_fem_epic.ff2_defusing_01", "gendarmerie_fem_epic.ff2_defusing_02", "gendarmerie_fem_epic.ff2_defusing_03", "gendarmerie_fem_epic.ff2_defusing_04" ],
            ["gendarmerie_fem_epic.ff2_defusing_06"] = [ "gendarmerie_fem_epic.ff2_defusing_01", "gendarmerie_fem_epic.ff2_defusing_02", "gendarmerie_fem_epic.ff2_defusing_03", "gendarmerie_fem_epic.ff2_defusing_04" ],
            ["gendarmerie_fem_epic.ff2_defusing_07"] = [ "gendarmerie_fem_epic.ff2_defusing_01", "gendarmerie_fem_epic.ff2_defusing_02", "gendarmerie_fem_epic.ff2_defusing_03", "gendarmerie_fem_epic.ff2_defusing_04" ],
            ["gendarmerie_fem_epic.ff2_defusing_08"] = [ "gendarmerie_fem_epic.ff2_defusing_01", "gendarmerie_fem_epic.ff2_defusing_02", "gendarmerie_fem_epic.ff2_defusing_03", "gendarmerie_fem_epic.ff2_defusing_04" ],
            ["gendarmerie_fem.ff1_request_stick_together_01"] = [ "gendarmerie_fem.ff1_request_follow_me_01", "gendarmerie_fem.ff1_request_follow_me_02", "gendarmerie_fem.ff1_request_follow_me_03", "gendarmerie_fem.ff1_request_follow_me_04", "gendarmerie_fem.ff1_request_follow_me_05" ],
            ["gendarmerie_fem_epic.ff2_heard_sound_04"] = [ "gendarmerie_fem_epic.ff2_heard_sound_01", "gendarmerie_fem_epic.ff2_heard_sound_02", "gendarmerie_fem_epic.ff2_heard_sound_03" ],
            ["gendarmerie_fem_epic.ff2_heard_sound_05"] = [ "gendarmerie_fem_epic.ff2_heard_sound_01", "gendarmerie_fem_epic.ff2_heard_sound_02", "gendarmerie_fem_epic.ff2_heard_sound_03" ],
            ["gendarmerie_fem_epic.ff2_negative_08"] = [ "gendarmerie_fem_epic.ff2_negative_01", "gendarmerie_fem_epic.ff2_negative_02", "gendarmerie_fem_epic.ff2_negative_03", "gendarmerie_fem_epic.ff2_negative_04", "gendarmerie_fem_epic.ff2_negative_05", "gendarmerie_fem_epic.ff2_negative_06", "gendarmerie_fem_epic.ff2_negative_07" ],
            ["gendarmerie_fem.ff1_request_stick_together_02"] = [ "gendarmerie_fem.ff1_request_follow_me_01", "gendarmerie_fem.ff1_request_follow_me_02", "gendarmerie_fem.ff1_request_follow_me_03", "gendarmerie_fem.ff1_request_follow_me_04", "gendarmerie_fem.ff1_request_follow_me_05" ],
            ["gendarmerie_fem_epic.ff2_request_move_10"] = [ "gendarmerie_fem_epic.ff2_request_move_01", "gendarmerie_fem_epic.ff2_request_move_02", "gendarmerie_fem_epic.ff2_request_move_03", "gendarmerie_fem_epic.ff2_request_move_04", "gendarmerie_fem_epic.ff2_request_move_05", "gendarmerie_fem_epic.ff2_request_move_06", "gendarmerie_fem_epic.ff2_request_move_07", "gendarmerie_fem_epic.ff2_request_move_08", "gendarmerie_fem_epic.ff2_request_move_09" ],
            ["gendarmerie_male.fm1_compliment_08"] = [ "gendarmerie_male.fm1_compliment_01", "gendarmerie_male.fm1_compliment_02", "gendarmerie_male.fm1_compliment_03", "gendarmerie_male.fm1_compliment_04", "gendarmerie_male.fm1_compliment_05", "gendarmerie_male.fm1_compliment_06", "gendarmerie_male.fm1_compliment_07" ],
            ["gendarmerie_male.fm1_defusing_05"] = [ "gendarmerie_male.fm1_defusing_01", "gendarmerie_male.fm1_defusing_02", "gendarmerie_male.fm1_defusing_03" ],
            ["gendarmerie_male.fm1_defusing_06"] = [ "gendarmerie_male.fm1_defusing_01", "gendarmerie_male.fm1_defusing_02", "gendarmerie_male.fm1_defusing_03" ],
            ["gendarmerie_male.fm1_following_friend_05"] = [ "gendarmerie_male.fm1_following_friend_01", "gendarmerie_male.fm1_following_friend_02", "gendarmerie_male.fm1_following_friend_03", "gendarmerie_male.fm1_following_friend_04" ],
            ["gendarmerie_male.fm1_following_friend_06"] = [ "gendarmerie_male.fm1_following_friend_01", "gendarmerie_male.fm1_following_friend_02", "gendarmerie_male.fm1_following_friend_03", "gendarmerie_male.fm1_following_friend_04" ],
            ["gendarmerie_male.fm1_negative_08"] = [ "gendarmerie_male.fm1_negative_01", "gendarmerie_male.fm1_negative_02", "gendarmerie_male.fm1_negative_03", "gendarmerie_male.fm1_negative_04", "gendarmerie_male.fm1_negative_05", "gendarmerie_male.fm1_negative_07" ],
            ["gendarmerie_male.fm1_negative_09"] = [ "gendarmerie_male.fm1_negative_01", "gendarmerie_male.fm1_negative_02", "gendarmerie_male.fm1_negative_03", "gendarmerie_male.fm1_negative_04", "gendarmerie_male.fm1_negative_05", "gendarmerie_male.fm1_negative_07" ],
            ["gendarmerie_male.fm1_request_move_08"] = [ "gendarmerie_male.fm1_request_move_01", "gendarmerie_male.fm1_request_move_02", "gendarmerie_male.fm1_request_move_03", "gendarmerie_male.fm1_request_move_04", "gendarmerie_male.fm1_request_move_05", "gendarmerie_male.fm1_request_move_06" ],
            ["gendarmerie_male.fm1_request_move_09"] = [ "gendarmerie_male.fm1_request_move_01", "gendarmerie_male.fm1_request_move_02", "gendarmerie_male.fm1_request_move_03", "gendarmerie_male.fm1_request_move_04", "gendarmerie_male.fm1_request_move_05", "gendarmerie_male.fm1_request_move_06" ],
            ["gendarmerie_male.fm1_request_move_10"] = [ "gendarmerie_male.fm1_request_move_01", "gendarmerie_male.fm1_request_move_02", "gendarmerie_male.fm1_request_move_03", "gendarmerie_male.fm1_request_move_04", "gendarmerie_male.fm1_request_move_05", "gendarmerie_male.fm1_request_move_06" ],
            ["gendarmerie_male.fm1_request_move_11"] = [ "gendarmerie_male.fm1_request_move_01", "gendarmerie_male.fm1_request_move_02", "gendarmerie_male.fm1_request_move_03", "gendarmerie_male.fm1_request_move_04", "gendarmerie_male.fm1_request_move_05", "gendarmerie_male.fm1_request_move_06" ],
            ["jungle_fem.aff1_bomb_planted_04"] = [ "jungle_fem.aff1_bomb_planted_01", "jungle_fem.aff1_bomb_planted_02", "jungle_fem.aff1_bomb_planted_03" ],
            ["jungle_fem.aff1_enemies_left_three_04"] = [ "jungle_fem.aff1_enemies_left_three_01", "jungle_fem.aff1_enemies_left_three_02", "jungle_fem.aff1_enemies_left_three_03" ],
            ["jungle_fem.aff1_heard_sound_04"] = [ "jungle_fem.aff1_heard_sound_01", "jungle_fem.aff1_heard_sound_02", "jungle_fem.aff1_heard_sound_03" ],
            ["jungle_fem.aff1_heard_sound_05"] = [ "jungle_fem.aff1_heard_sound_01", "jungle_fem.aff1_heard_sound_02", "jungle_fem.aff1_heard_sound_03" ],
            ["jungle_fem.aff1_heard_sound_06"] = [ "jungle_fem.aff1_heard_sound_01", "jungle_fem.aff1_heard_sound_02", "jungle_fem.aff1_heard_sound_03" ],
            ["jungle_fem.aff1_negative_07"] = [ "jungle_fem.aff1_negative_01", "jungle_fem.aff1_negative_02", "jungle_fem.aff1_negative_03", "jungle_fem.aff1_negative_04", "jungle_fem.aff1_negative_05", "jungle_fem.aff1_negative_06" ],
            ["jungle_fem.aff1_omw_to_plant_a_04"] = [ "jungle_fem.aff1_omw_to_plant_a_01", "jungle_fem.aff1_omw_to_plant_a_02", "jungle_fem.aff1_omw_to_plant_a_03" ],
            ["jungle_fem.aff1_omw_to_plant_b_05"] = [ "jungle_fem.aff1_omw_to_plant_b_01", "jungle_fem.aff1_omw_to_plant_b_02", "jungle_fem.aff1_omw_to_plant_b_03", "jungle_fem.aff1_omw_to_plant_b_04" ],
            ["jungle_fem.aff1_omw_to_plant_b_06"] = [ "jungle_fem.aff1_omw_to_plant_b_01", "jungle_fem.aff1_omw_to_plant_b_02", "jungle_fem.aff1_omw_to_plant_b_03", "jungle_fem.aff1_omw_to_plant_b_04" ],
            ["gendarmerie_fem.ff1_request_stick_together_03"] = [ "gendarmerie_fem.ff1_request_follow_me_01", "gendarmerie_fem.ff1_request_follow_me_02", "gendarmerie_fem.ff1_request_follow_me_03", "gendarmerie_fem.ff1_request_follow_me_04", "gendarmerie_fem.ff1_request_follow_me_05" ],
            ["jungle_fem.aff1_request_move_08"] = [ "jungle_fem.aff1_request_move_01", "jungle_fem.aff1_request_move_02", "jungle_fem.aff1_request_move_03", "jungle_fem.aff1_request_move_04", "jungle_fem.aff1_request_move_05", "jungle_fem.aff1_request_move_06", "jungle_fem.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_following_friend_07"] = [ "jungle_fem_epic.aff1_following_friend_01", "jungle_fem_epic.aff1_following_friend_02", "jungle_fem_epic.aff1_following_friend_03", "jungle_fem_epic.aff1_following_friend_04", "jungle_fem_epic.aff1_following_friend_05", "jungle_fem_epic.aff1_following_friend_06" ],
            ["jungle_fem_epic.aff1_following_friend_08"] = [ "jungle_fem_epic.aff1_following_friend_01", "jungle_fem_epic.aff1_following_friend_02", "jungle_fem_epic.aff1_following_friend_03", "jungle_fem_epic.aff1_following_friend_04", "jungle_fem_epic.aff1_following_friend_05", "jungle_fem_epic.aff1_following_friend_06" ],
            ["jungle_fem_epic.aff1_following_friend_09"] = [ "jungle_fem_epic.aff1_following_friend_01", "jungle_fem_epic.aff1_following_friend_02", "jungle_fem_epic.aff1_following_friend_03", "jungle_fem_epic.aff1_following_friend_04", "jungle_fem_epic.aff1_following_friend_05", "jungle_fem_epic.aff1_following_friend_06" ],
            ["jungle_fem_epic.aff1_heard_sound_04"] = [ "jungle_fem_epic.aff1_heard_sound_01", "jungle_fem_epic.aff1_heard_sound_02", "jungle_fem_epic.aff1_heard_sound_03" ],
            ["jungle_fem_epic.aff1_heard_sound_05"] = [ "jungle_fem_epic.aff1_heard_sound_01", "jungle_fem_epic.aff1_heard_sound_02", "jungle_fem_epic.aff1_heard_sound_03" ],
            ["jungle_fem_epic.aff1_heard_sound_06"] = [ "jungle_fem_epic.aff1_heard_sound_01", "jungle_fem_epic.aff1_heard_sound_02", "jungle_fem_epic.aff1_heard_sound_03" ],
            ["jungle_fem_epic.aff1_negative_07"] = [ "jungle_fem_epic.aff1_negative_01", "jungle_fem_epic.aff1_negative_02", "jungle_fem_epic.aff1_negative_03", "jungle_fem_epic.aff1_negative_04", "jungle_fem_epic.aff1_negative_05", "jungle_fem_epic.aff1_negative_06" ],
            ["jungle_fem_epic.aff1_negative_08"] = [ "jungle_fem_epic.aff1_negative_01", "jungle_fem_epic.aff1_negative_02", "jungle_fem_epic.aff1_negative_03", "jungle_fem_epic.aff1_negative_04", "jungle_fem_epic.aff1_negative_05", "jungle_fem_epic.aff1_negative_06" ],
            ["jungle_fem_epic.aff1_request_follow_me_06"] = [ "jungle_fem_epic.aff1_request_follow_me_01", "jungle_fem_epic.aff1_request_follow_me_02", "jungle_fem_epic.aff1_request_follow_me_03", "jungle_fem_epic.aff1_request_follow_me_04", "jungle_fem_epic.aff1_request_follow_me_05" ],
            ["jungle_fem_epic.aff1_request_move_08"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_request_move_09"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_request_move_10"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_request_move_11"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_request_move_12"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_fem_epic.aff1_request_move_13"] = [ "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02", "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04", "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06", "jungle_fem_epic.aff1_request_move_07" ],
            ["jungle_male.afm1_bomb_planted_04"] = [ "jungle_male.afm1_bomb_planted_01", "jungle_male.afm1_bomb_planted_02", "jungle_male.afm1_bomb_planted_03" ],
            ["jungle_male.afm1_enemies_left_three_04"] = [ "jungle_male.afm1_enemies_left_three_01", "jungle_male.afm1_enemies_left_three_02", "jungle_male.afm1_enemies_left_three_03" ],
            ["jungle_male.afm1_following_friend_05"] = [ "jungle_male.afm1_following_friend_01", "jungle_male.afm1_following_friend_02", "jungle_male.afm1_following_friend_03", "jungle_male.afm1_following_friend_04" ],
            ["jungle_male.afm1_following_friend_06"] = [ "jungle_male.afm1_following_friend_01", "jungle_male.afm1_following_friend_02", "jungle_male.afm1_following_friend_03", "jungle_male.afm1_following_friend_04" ],
            ["jungle_male.afm1_heard_sound_04"] = [ "jungle_male.afm1_heard_sound_01", "jungle_male.afm1_heard_sound_02", "jungle_male.afm1_heard_sound_03" ],
            ["jungle_male.afm1_heard_sound_05"] = [ "jungle_male.afm1_heard_sound_01", "jungle_male.afm1_heard_sound_02", "jungle_male.afm1_heard_sound_03" ],
            ["jungle_male.afm1_heard_sound_06"] = [ "jungle_male.afm1_heard_sound_01", "jungle_male.afm1_heard_sound_02", "jungle_male.afm1_heard_sound_03" ],
            ["jungle_male.afm1_i_plant_bomb_04"] = [ "jungle_male.afm1_i_plant_bomb_01", "jungle_male.afm1_i_plant_bomb_02", "jungle_male.afm1_i_plant_bomb_03" ],
            ["jungle_male.afm1_i_plant_bomb_05"] = [ "jungle_male.afm1_i_plant_bomb_01", "jungle_male.afm1_i_plant_bomb_02", "jungle_male.afm1_i_plant_bomb_03" ],
            ["jungle_male.afm1_negative_07"] = [ "jungle_male.afm1_negative_01", "jungle_male.afm1_negative_02", "jungle_male.afm1_negative_03", "jungle_male.afm1_negative_04", "jungle_male.afm1_negative_05", "jungle_male.afm1_negative_06" ],
            ["jungle_male.afm1_omw_to_plant_a_04"] = [ "jungle_male.afm1_omw_to_plant_a_01", "jungle_male.afm1_omw_to_plant_a_02", "jungle_male.afm1_omw_to_plant_a_03" ],
            ["jungle_male.afm1_omw_to_plant_b_04"] = [ "jungle_male.afm1_omw_to_plant_b_01", "jungle_male.afm1_omw_to_plant_b_02", "jungle_male.afm1_omw_to_plant_b_03" ],
            ["jungle_male.afm1_omw_to_plant_b_05"] = [ "jungle_male.afm1_omw_to_plant_b_01", "jungle_male.afm1_omw_to_plant_b_02", "jungle_male.afm1_omw_to_plant_b_03" ],
            ["jungle_male.afm1_omw_to_plant_b_06"] = [ "jungle_male.afm1_omw_to_plant_b_01", "jungle_male.afm1_omw_to_plant_b_02", "jungle_male.afm1_omw_to_plant_b_03" ],
            ["jungle_male.afm1_request_follow_me_06"] = [ "jungle_male.afm1_request_follow_me_01", "jungle_male.afm1_request_follow_me_02", "jungle_male.afm1_request_follow_me_03", "jungle_male.afm1_request_follow_me_04", "jungle_male.afm1_request_follow_me_05" ],
            ["jungle_male.afm1_request_move_08"] = [ "jungle_male.afm1_request_move_01", "jungle_male.afm1_request_move_02", "jungle_male.afm1_request_move_03", "jungle_male.afm1_request_move_04", "jungle_male.afm1_request_move_05", "jungle_male.afm1_request_move_06", "jungle_male.afm1_request_move_07" ],
            ["jungle_male_epic.afm2_following_friend_07"] = [ "jungle_male_epic.afm2_following_friend_01", "jungle_male_epic.afm2_following_friend_02", "jungle_male_epic.afm2_following_friend_03", "jungle_male_epic.afm2_following_friend_04", "jungle_male_epic.afm2_following_friend_05", "jungle_male_epic.afm2_following_friend_06" ],
            ["jungle_male_epic.afm2_following_friend_08"] = [ "jungle_male_epic.afm2_following_friend_01", "jungle_male_epic.afm2_following_friend_02", "jungle_male_epic.afm2_following_friend_03", "jungle_male_epic.afm2_following_friend_04", "jungle_male_epic.afm2_following_friend_05", "jungle_male_epic.afm2_following_friend_06" ],
            ["jungle_male_epic.afm2_following_friend_09"] = [ "jungle_male_epic.afm2_following_friend_01", "jungle_male_epic.afm2_following_friend_02", "jungle_male_epic.afm2_following_friend_03", "jungle_male_epic.afm2_following_friend_04", "jungle_male_epic.afm2_following_friend_05", "jungle_male_epic.afm2_following_friend_06" ],
            ["jungle_male_epic.afm2_heard_sound_04"] = [ "jungle_male_epic.afm2_heard_sound_01", "jungle_male_epic.afm2_heard_sound_02", "jungle_male_epic.afm2_heard_sound_03" ],
            ["jungle_male_epic.afm2_heard_sound_05"] = [ "jungle_male_epic.afm2_heard_sound_01", "jungle_male_epic.afm2_heard_sound_02", "jungle_male_epic.afm2_heard_sound_03" ],
            ["jungle_male_epic.afm2_heard_sound_06"] = [ "jungle_male_epic.afm2_heard_sound_01", "jungle_male_epic.afm2_heard_sound_02", "jungle_male_epic.afm2_heard_sound_03" ],
            ["jungle_male_epic.afm2_negative_07"] = [ "jungle_male_epic.afm2_negative_01", "jungle_male_epic.afm2_negative_02", "jungle_male_epic.afm2_negative_03", "jungle_male_epic.afm2_negative_04", "jungle_male_epic.afm2_negative_05", "jungle_male_epic.afm2_negative_06" ],
            ["jungle_male_epic.afm2_negative_08"] = [ "jungle_male_epic.afm2_negative_01", "jungle_male_epic.afm2_negative_02", "jungle_male_epic.afm2_negative_03", "jungle_male_epic.afm2_negative_04", "jungle_male_epic.afm2_negative_05", "jungle_male_epic.afm2_negative_06" ],
            ["jungle_male_epic.afm2_request_follow_me_06"] = [ "jungle_male_epic.afm2_request_follow_me_01", "jungle_male_epic.afm2_request_follow_me_02", "jungle_male_epic.afm2_request_follow_me_03", "jungle_male_epic.afm2_request_follow_me_04", "jungle_male_epic.afm2_request_follow_me_05" ],
            ["jungle_male_epic.afm2_request_move_08"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["jungle_male_epic.afm2_request_move_09"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["jungle_male_epic.afm2_request_move_10"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["jungle_male_epic.afm2_request_move_11"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["jungle_male_epic.afm2_request_move_12"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["jungle_male_epic.afm2_request_move_13"] = [ "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02", "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04", "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06", "jungle_male_epic.afm2_request_move_07" ],
            ["leet_epic.planted_b_04"] = [ "leet_epic.planted_b_01", "leet_epic.planted_b_02", "leet_epic.planted_b_03" ],
            ["professional_epic.omw_to_plant_a_04"] = [ "professional_epic.omw_to_plant_a_01", "professional_epic.omw_to_plant_a_02", "professional_epic.omw_to_plant_a_03" ],
            ["seal_diver_01.am1_defusing_05"] = [ "seal_diver_01.am1_defusing_01", "seal_diver_01.am1_defusing_02", "seal_diver_01.am1_defusing_03", "seal_diver_01.am1_defusing_04" ],
            ["seal_diver_01.am1_defusing_06"] = [ "seal_diver_01.am1_defusing_01", "seal_diver_01.am1_defusing_02", "seal_diver_01.am1_defusing_03", "seal_diver_01.am1_defusing_04" ],
            ["seal_diver_01.am1_following_friend_06"] = [ "seal_diver_01.am1_following_friend_01", "seal_diver_01.am1_following_friend_02", "seal_diver_01.am1_following_friend_03", "seal_diver_01.am1_following_friend_04", "seal_diver_01.am1_following_friend_05" ],
            ["seal_diver_01.am1_negative_08"] = [ "seal_diver_01.am1_negative_01", "seal_diver_01.am1_negative_02", "seal_diver_01.am1_negative_03", "seal_diver_01.am1_negative_04", "seal_diver_01.am1_negative_05", "seal_diver_01.am1_negative_06", "seal_diver_01.am1_negative_07" ],
            ["seal_diver_01.am1_negative_09"] = [ "seal_diver_01.am1_negative_01", "seal_diver_01.am1_negative_02", "seal_diver_01.am1_negative_03", "seal_diver_01.am1_negative_04", "seal_diver_01.am1_negative_05", "seal_diver_01.am1_negative_06", "seal_diver_01.am1_negative_07" ],
            ["seal_diver_01.am1_request_move_10"] = [ "seal_diver_01.am1_request_move_01", "seal_diver_01.am1_request_move_02", "seal_diver_01.am1_request_move_03", "seal_diver_01.am1_request_move_04", "seal_diver_01.am1_request_move_05", "seal_diver_01.am1_request_move_06", "seal_diver_01.am1_request_move_07", "seal_diver_01.am1_request_move_08", "seal_diver_01.am1_request_move_09" ],
            ["seal_diver_01.am1_request_move_11"] = [ "seal_diver_01.am1_request_move_01", "seal_diver_01.am1_request_move_02", "seal_diver_01.am1_request_move_03", "seal_diver_01.am1_request_move_04", "seal_diver_01.am1_request_move_05", "seal_diver_01.am1_request_move_06", "seal_diver_01.am1_request_move_07", "seal_diver_01.am1_request_move_08", "seal_diver_01.am1_request_move_09" ],
            ["seal_diver_02.am1_defusing_04"] = [ "seal_diver_02.am1_defusing_01", "seal_diver_02.am1_defusing_02", "seal_diver_02.am1_defusing_03" ],
            ["seal_diver_02.am1_defusing_05"] = [ "seal_diver_02.am1_defusing_01", "seal_diver_02.am1_defusing_02", "seal_diver_02.am1_defusing_03" ],
            ["seal_diver_02.am1_defusing_06"] = [ "seal_diver_02.am1_defusing_01", "seal_diver_02.am1_defusing_02", "seal_diver_02.am1_defusing_03" ],
            ["seal_diver_02.am1_following_friend_05"] = [ "seal_diver_02.am1_following_friend_01", "seal_diver_02.am1_following_friend_02", "seal_diver_02.am1_following_friend_03", "seal_diver_02.am1_following_friend_04" ],
            ["seal_diver_02.am1_following_friend_06"] = [ "seal_diver_02.am1_following_friend_01", "seal_diver_02.am1_following_friend_02", "seal_diver_02.am1_following_friend_03", "seal_diver_02.am1_following_friend_04" ],
            ["seal_diver_02.am1_negative_06"] = [ "seal_diver_02.am1_negative_01", "seal_diver_02.am1_negative_02", "seal_diver_02.am1_negative_03", "seal_diver_02.am1_negative_04", "seal_diver_02.am1_negative_05", "seal_diver_02.am1_negative_07" ],
            ["seal_diver_02.am1_negative_08"] = [ "seal_diver_02.am1_negative_01", "seal_diver_02.am1_negative_02", "seal_diver_02.am1_negative_03", "seal_diver_02.am1_negative_04", "seal_diver_02.am1_negative_05", "seal_diver_02.am1_negative_07" ],
            ["seal_diver_02.am1_negative_09"] = [ "seal_diver_02.am1_negative_01", "seal_diver_02.am1_negative_02", "seal_diver_02.am1_negative_03", "seal_diver_02.am1_negative_04", "seal_diver_02.am1_negative_05", "seal_diver_02.am1_negative_07" ],
            ["seal_diver_02.am1_request_move_07"] = [ "seal_diver_02.am1_request_move_01", "seal_diver_02.am1_request_move_02", "seal_diver_02.am1_request_move_03", "seal_diver_02.am1_request_move_04", "seal_diver_02.am1_request_move_05", "seal_diver_02.am1_request_move_06" ],
            ["seal_diver_02.am1_request_move_08"] = [ "seal_diver_02.am1_request_move_01", "seal_diver_02.am1_request_move_02", "seal_diver_02.am1_request_move_03", "seal_diver_02.am1_request_move_04", "seal_diver_02.am1_request_move_05", "seal_diver_02.am1_request_move_06" ],
            ["seal_diver_02.am1_request_move_09"] = [ "seal_diver_02.am1_request_move_01", "seal_diver_02.am1_request_move_02", "seal_diver_02.am1_request_move_03", "seal_diver_02.am1_request_move_04", "seal_diver_02.am1_request_move_05", "seal_diver_02.am1_request_move_06" ],
            ["seal_diver_02.am1_request_move_10"] = [ "seal_diver_02.am1_request_move_01", "seal_diver_02.am1_request_move_02", "seal_diver_02.am1_request_move_03", "seal_diver_02.am1_request_move_04", "seal_diver_02.am1_request_move_05", "seal_diver_02.am1_request_move_06" ],
            ["seal_diver_02.am1_request_move_11"] = [ "seal_diver_02.am1_request_move_01", "seal_diver_02.am1_request_move_02", "seal_diver_02.am1_request_move_03", "seal_diver_02.am1_request_move_04", "seal_diver_02.am1_request_move_05", "seal_diver_02.am1_request_move_06" ],
            ["seal_epic.planted_b_04"] = [ "seal_epic.planted_b_01", "seal_epic.planted_b_02", "seal_epic.planted_b_03" ],
            ["seal_fem.af1_defusing_05"] = [ "seal_fem.af1_defusing_01", "seal_fem.af1_defusing_02", "seal_fem.af1_defusing_03", "seal_fem.af1_defusing_04" ],
            ["seal_fem.af1_defusing_06"] = [ "seal_fem.af1_defusing_01", "seal_fem.af1_defusing_02", "seal_fem.af1_defusing_03", "seal_fem.af1_defusing_04" ],
            ["seal_fem.af1_following_friend_05"] = [ "seal_fem.af1_following_friend_01", "seal_fem.af1_following_friend_02", "seal_fem.af1_following_friend_03", "seal_fem.af1_following_friend_04" ],
            ["seal_fem.af1_following_friend_06"] = [ "seal_fem.af1_following_friend_01", "seal_fem.af1_following_friend_02", "seal_fem.af1_following_friend_03", "seal_fem.af1_following_friend_04" ],
            ["seal_fem.af1_negative_08"] = [ "seal_fem.af1_negative_01", "seal_fem.af1_negative_02", "seal_fem.af1_negative_03", "seal_fem.af1_negative_04", "seal_fem.af1_negative_05", "seal_fem.af1_negative_06", "seal_fem.af1_negative_07" ],
            ["seal_fem.af1_negative_09"] = [ "seal_fem.af1_negative_01", "seal_fem.af1_negative_02", "seal_fem.af1_negative_03", "seal_fem.af1_negative_04", "seal_fem.af1_negative_05", "seal_fem.af1_negative_06", "seal_fem.af1_negative_07" ],
            ["seal_fem.af1_request_move_08"] = [ "seal_fem.af1_request_move_01", "seal_fem.af1_request_move_02", "seal_fem.af1_request_move_03", "seal_fem.af1_request_move_04", "seal_fem.af1_request_move_05", "seal_fem.af1_request_move_06", "seal_fem.af1_request_move_07" ],
            ["seal_fem.af1_request_move_09"] = [ "seal_fem.af1_request_move_01", "seal_fem.af1_request_move_02", "seal_fem.af1_request_move_03", "seal_fem.af1_request_move_04", "seal_fem.af1_request_move_05", "seal_fem.af1_request_move_06", "seal_fem.af1_request_move_07" ],
            ["seal_fem.af1_request_move_10"] = [ "seal_fem.af1_request_move_01", "seal_fem.af1_request_move_02", "seal_fem.af1_request_move_03", "seal_fem.af1_request_move_04", "seal_fem.af1_request_move_05", "seal_fem.af1_request_move_06", "seal_fem.af1_request_move_07" ],
            ["seal_fem.af1_request_move_11"] = [ "seal_fem.af1_request_move_01", "seal_fem.af1_request_move_02", "seal_fem.af1_request_move_03", "seal_fem.af1_request_move_04", "seal_fem.af1_request_move_05", "seal_fem.af1_request_move_06", "seal_fem.af1_request_move_07" ],
            ["swat_epic.following_friend_05"] = [ "swat_epic.following_friend_01", "swat_epic.following_friend_02", "swat_epic.following_friend_03", "swat_epic.following_friend_04" ],
            ["swat_epic.following_friend_06"] = [ "swat_epic.following_friend_01", "swat_epic.following_friend_02", "swat_epic.following_friend_03", "swat_epic.following_friend_04" ],
            ["swat_fem.following_friend_06"] = [ "swat_fem.following_friend_01", "swat_fem.following_friend_02", "swat_fem.following_friend_03", "swat_fem.following_friend_04", "swat_fem.following_friend_05" ],
        };
    public static string[] CreateDefaultObservedBrokenNoFallback() =>
    [
    ];
    public static string[] CreateDefaultBlockedStems() =>
    [
        "balkan_epic.planted_a", "balkan_epic.radio_request_move", "jungle_fem.aff1_planting_a",
        "jungle_fem.aff1_planting_b", "jungle_fem_epic.aff1_planted_a", "jungle_fem_epic.aff1_planted_b",
        "jungle_fem_epic.aff1_planting_a", "jungle_fem_epic.aff1_planting_b",
        "jungle_fem_epic.aff1_radio_request_move", "jungle_male.afm1_planting_a", "jungle_male.afm1_planting_b",
        "jungle_male_epic.afm2_planted_a", "jungle_male_epic.afm2_planted_b", "jungle_male_epic.afm2_planting_a",
        "jungle_male_epic.afm2_planting_b", "jungle_male_epic.afm2_radio_request_move", "leet_epic.planted_a",
        "leet_epic.radio_request_move", "professional_epic.planting_a", "professional_epic.planting_b",
        "professional_fem.planting_a", "professional_fem.planting_b"
            ];

    public static Dictionary<string, string[]> CreateDefaultCategoryFallbacks() =>
        new(StringComparer.Ordinal)
        {
            ["balkan_epic.planted_a"] = [ "balkan_epic.planting_a_01", "balkan_epic.planting_a_02", "balkan_epic.planting_a_03" ],
            ["balkan_epic.radio_request_move"] =
            [
                "balkan_epic.request_move_01", "balkan_epic.request_move_02", "balkan_epic.request_move_03",
                "balkan_epic.request_move_04", "balkan_epic.request_move_05", "balkan_epic.request_move_06",
                "balkan_epic.request_move_07", "balkan_epic.request_move_08", "balkan_epic.request_move_09",
                "balkan_epic.request_move_10", "balkan_epic.request_move_11", "balkan_epic.request_move_12",
                "balkan_epic.request_move_13", "balkan_epic.request_move_14"
            ],
            ["jungle_fem.aff1_planting_a"] =
            [
                "jungle_fem.aff1_i_plant_bomb_01", "jungle_fem.aff1_i_plant_bomb_02", "jungle_fem.aff1_i_plant_bomb_03",
                "jungle_fem.aff1_i_plant_bomb_04", "jungle_fem.aff1_i_plant_bomb_05"
            ],
            ["jungle_fem.aff1_planting_b"] =
            [
                "jungle_fem.aff1_i_plant_bomb_01", "jungle_fem.aff1_i_plant_bomb_02", "jungle_fem.aff1_i_plant_bomb_03",
                "jungle_fem.aff1_i_plant_bomb_04", "jungle_fem.aff1_i_plant_bomb_05"
            ],
            ["jungle_fem_epic.aff1_planted_a"] =
            [
                "jungle_fem_epic.aff1_bomb_planted_01", "jungle_fem_epic.aff1_bomb_planted_02",
                "jungle_fem_epic.aff1_bomb_planted_03"
            ],
            ["jungle_fem_epic.aff1_planted_b"] =
            [
                "jungle_fem_epic.aff1_bomb_planted_01", "jungle_fem_epic.aff1_bomb_planted_02",
                "jungle_fem_epic.aff1_bomb_planted_03"
            ],
            ["jungle_fem_epic.aff1_planting_a"] =
            [
                "jungle_fem_epic.aff1_i_plant_bomb_01", "jungle_fem_epic.aff1_i_plant_bomb_02",
                "jungle_fem_epic.aff1_i_plant_bomb_03", "jungle_fem_epic.aff1_i_plant_bomb_04",
                "jungle_fem_epic.aff1_i_plant_bomb_05"
            ],
            ["jungle_fem_epic.aff1_planting_b"] =
            [
                "jungle_fem_epic.aff1_i_plant_bomb_01", "jungle_fem_epic.aff1_i_plant_bomb_02",
                "jungle_fem_epic.aff1_i_plant_bomb_03", "jungle_fem_epic.aff1_i_plant_bomb_04",
                "jungle_fem_epic.aff1_i_plant_bomb_05"
            ],
            ["jungle_fem_epic.aff1_radio_request_move"] =
            [
                "jungle_fem_epic.aff1_request_move_01", "jungle_fem_epic.aff1_request_move_02",
                "jungle_fem_epic.aff1_request_move_03", "jungle_fem_epic.aff1_request_move_04",
                "jungle_fem_epic.aff1_request_move_05", "jungle_fem_epic.aff1_request_move_06",
                "jungle_fem_epic.aff1_request_move_07"
            ],
            ["jungle_male.afm1_planting_a"] =
            [
                "jungle_male.afm1_i_plant_bomb_01", "jungle_male.afm1_i_plant_bomb_02",
                "jungle_male.afm1_i_plant_bomb_03"
            ],
            ["jungle_male.afm1_planting_b"] =
            [
                "jungle_male.afm1_i_plant_bomb_01", "jungle_male.afm1_i_plant_bomb_02",
                "jungle_male.afm1_i_plant_bomb_03"
            ],
            ["jungle_male_epic.afm2_planted_a"] =
            [
                "jungle_male_epic.afm2_bomb_planted_01", "jungle_male_epic.afm2_bomb_planted_02",
                "jungle_male_epic.afm2_bomb_planted_03"
            ],
            ["jungle_male_epic.afm2_planted_b"] =
            [
                "jungle_male_epic.afm2_bomb_planted_01", "jungle_male_epic.afm2_bomb_planted_02",
                "jungle_male_epic.afm2_bomb_planted_03"
            ],
            ["jungle_male_epic.afm2_planting_a"] =
            [
                "jungle_male_epic.afm2_i_plant_bomb_01", "jungle_male_epic.afm2_i_plant_bomb_02",
                "jungle_male_epic.afm2_i_plant_bomb_03", "jungle_male_epic.afm2_i_plant_bomb_04",
                "jungle_male_epic.afm2_i_plant_bomb_05"
            ],
            ["jungle_male_epic.afm2_planting_b"] =
            [
                "jungle_male_epic.afm2_i_plant_bomb_01", "jungle_male_epic.afm2_i_plant_bomb_02",
                "jungle_male_epic.afm2_i_plant_bomb_03", "jungle_male_epic.afm2_i_plant_bomb_04",
                "jungle_male_epic.afm2_i_plant_bomb_05"
            ],
            ["jungle_male_epic.afm2_radio_request_move"] =
            [
                "jungle_male_epic.afm2_request_move_01", "jungle_male_epic.afm2_request_move_02",
                "jungle_male_epic.afm2_request_move_03", "jungle_male_epic.afm2_request_move_04",
                "jungle_male_epic.afm2_request_move_05", "jungle_male_epic.afm2_request_move_06",
                "jungle_male_epic.afm2_request_move_07"
            ],
            ["leet_epic.planted_a"] = [ "leet_epic.planting_a_01", "leet_epic.planting_a_02", "leet_epic.planting_a_03" ],
            ["leet_epic.radio_request_move"] =
            [
                "leet_epic.request_move_01", "leet_epic.request_move_02", "leet_epic.request_move_03",
                "leet_epic.request_move_04", "leet_epic.request_move_05", "leet_epic.request_move_06",
                "leet_epic.request_move_07", "leet_epic.request_move_08", "leet_epic.request_move_09",
                "leet_epic.request_move_10", "leet_epic.request_move_11", "leet_epic.request_move_12",
                "leet_epic.request_move_13"
            ],
            ["professional_epic.planting_a"] =
            [
                "professional_epic.i_plant_bomb_01", "professional_epic.i_plant_bomb_02",
                "professional_epic.i_plant_bomb_03", "professional_epic.i_plant_bomb_04",
                "professional_epic.i_plant_bomb_05"
            ],
            ["professional_epic.planting_b"] =
            [
                "professional_epic.i_plant_bomb_01", "professional_epic.i_plant_bomb_02",
                "professional_epic.i_plant_bomb_03", "professional_epic.i_plant_bomb_04",
                "professional_epic.i_plant_bomb_05"
            ],
            ["professional_fem.planting_a"] =
            [
                "professional_fem.i_plant_bomb_01", "professional_fem.i_plant_bomb_02",
                "professional_fem.i_plant_bomb_03", "professional_fem.i_plant_bomb_04",
                "professional_fem.i_plant_bomb_05"
            ],
            ["professional_fem.planting_b"] =
            [
                "professional_fem.i_plant_bomb_01", "professional_fem.i_plant_bomb_02",
                "professional_fem.i_plant_bomb_03", "professional_fem.i_plant_bomb_04",
                "professional_fem.i_plant_bomb_05"
            ],
        };
}

public sealed class CurrentVoiceCatalogDocument
{
    public int SchemaVersion { get; set; } = 1;
    public string GeneratedAt { get; set; } = string.Empty;
    public string CurrentSource { get; set; } = string.Empty;
    public string PackageIndexSource { get; set; } = string.Empty;
    public string LegacyAuditSource { get; set; } = string.Empty;
    public string[] Prefixes { get; set; } = [];
    public int DefinedNumberedEventCount { get; set; }
    public int HealthyNumberedEventCount { get; set; }
    public int ExcludedBrokenEventCount { get; set; }
    public string[] ExcludedBrokenEvents { get; set; } = [];
    public Dictionary<string, string[]> NumberedStems { get; set; } = new(StringComparer.Ordinal);
}
