namespace Gnosis.Security.Moderation;

public interface IContentModerator
{
    ModerationResult ModerateText(string text);
    ModerationResult ModerateImage(byte[] imageData);
}
