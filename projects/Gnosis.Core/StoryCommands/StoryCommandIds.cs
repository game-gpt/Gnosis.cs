namespace Gnosis.Core.StoryCommands;

public static class StoryCommandIds
{
    #region Audio 命令 (1000-1099)

    public const int AudioPlay = 1000;
    public const int AudioStop = 1001;
    public const int AudioPlaySe = 1002;
    public const int AudioFadeOut = 1003;

    #endregion

    #region Scene 命令 (1100-1199)

    public const int SceneChange = 1100;
    public const int SceneTransition = 1101;

    #endregion

    #region Character 命令 (1200-1299)

    public const int CharacterShow = 1200;
    public const int CharacterHide = 1201;
    public const int CharacterMove = 1202;

    #endregion

    #region Effect 命令 (1300-1399)

    public const int EffectFlash = 1300;
    public const int EffectShake = 1301;
    public const int EffectFadeOut = 1302;
    public const int EffectLightning = 1303;

    #endregion

    #region Quest 命令 (1400-1499)

    public const int QuestStart = 1400;
    public const int QuestComplete = 1401;
    public const int QuestUpdate = 1402;

    #endregion

    #region Dialogue 命令 (1500-1599)

    public const int DialogueShow = 1500;
    public const int DialogueHide = 1501;

    #endregion
}

public static class StoryCommandNames
{
    #region Audio 命令

    public const string AudioPlay = "story_audio_play";
    public const string AudioStop = "story_audio_stop";
    public const string AudioPlaySe = "story_audio_play_se";
    public const string AudioFadeOut = "story_audio_fade_out";

    #endregion

    #region Scene 命令

    public const string SceneChange = "story_scene_change";
    public const string SceneTransition = "story_scene_transition";

    #endregion

    #region Character 命令

    public const string CharacterShow = "story_character_show";
    public const string CharacterHide = "story_character_hide";
    public const string CharacterMove = "story_character_move";

    #endregion

    #region Effect 命令

    public const string EffectFlash = "story_effect_flash";
    public const string EffectShake = "story_effect_shake";
    public const string EffectFadeOut = "story_effect_fade_out";
    public const string EffectLightning = "story_effect_lightning";

    #endregion

    #region Quest 命令

    public const string QuestStart = "story_quest_start";
    public const string QuestComplete = "story_quest_complete";
    public const string QuestUpdate = "story_quest_update";

    #endregion

    #region Dialogue 命令

    public const string DialogueShow = "story_dialogue_show";
    public const string DialogueHide = "story_dialogue_hide";

    #endregion
}
