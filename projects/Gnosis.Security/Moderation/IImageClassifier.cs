namespace Gnosis.Security.Moderation;

public interface IImageClassifier
{
    List<ImageClassificationResult> Classify(byte[] imageData);
}
