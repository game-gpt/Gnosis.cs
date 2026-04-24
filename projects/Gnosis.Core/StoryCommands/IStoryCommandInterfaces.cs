namespace Gnosis.Core.StoryCommands;

public interface IStoryDialogueCommands
{
    void ShowDialogue(string? speaker, string text, string? emotion);

    void HideDialogue();
}

public interface IStorySceneCommands
{
    void ChangeScene(string scenePath, string transition);

    void SceneTransition(string type, float duration);
}

public interface IStoryCharacterCommands
{
    void ShowCharacter(string name, string position, string? emotion);

    void HideCharacter(string? name);

    void MoveCharacter(string name, string targetPosition, float duration);
}

public interface IStoryEffectCommands
{
    void Flash(float r, float g, float b, float duration);

    void Shake(float duration, float intensity);

    void FadeOut(float duration, float r, float g, float b);

    void Lightning(float duration);
}

public interface IStoryAudioCommands
{
    void PlayBgm(string path, float volume);

    void StopBgm();

    void PlaySe(string path, float volume);

    void FadeOutBgm(float duration);
}

public interface IStoryQuestCommands
{
    void StartQuest(string questId, string description);

    void CompleteQuest(string questId);

    void UpdateQuest(string questId, int progress);
}
