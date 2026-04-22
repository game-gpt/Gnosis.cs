namespace Gnosis.Security.Moderation;

public sealed class ContentModerator : IContentModerator
{
    #region 字段

    private readonly ITextFilter _textFilter;
    private readonly IImageClassifier? _imageClassifier;

    #endregion

    #region 属性

    public ITextFilter TextFilter => _textFilter;

    #endregion

    #region 构造函数

    public ContentModerator(ITextFilter textFilter, IImageClassifier? imageClassifier = null)
    {
        _textFilter = textFilter;
        _imageClassifier = imageClassifier;
    }

    #endregion

    #region 公开方法

    public ModerationResult ModerateText(string text)
    {
        if (string.IsNullOrEmpty(text)) return new ModerationResult(ModerationStatus.Approved);

        if (_textFilter.ContainsSensitiveWord(text))
        {
            return new ModerationResult(ModerationStatus.Rejected, "文本包含敏感词", 1.0f);
        }

        return new ModerationResult(ModerationStatus.Approved);
    }

    public ModerationResult ModerateImage(byte[] imageData)
    {
        if (_imageClassifier is null)
        {
            return new ModerationResult(ModerationStatus.NeedsReview, "图片审核服务不可用", 0.0f);
        }

        if (imageData is null || imageData.Length == 0)
        {
            return new ModerationResult(ModerationStatus.Rejected, "图片数据为空", 1.0f);
        }

        var classifications = _imageClassifier.Classify(imageData);

        foreach (var result in classifications)
        {
            if (result.Category == "unsafe" && result.Confidence > 0.8f)
            {
                return new ModerationResult(ModerationStatus.Rejected, "图片内容不安全", result.Confidence);
            }

            if (result.Category == "unsafe" && result.Confidence > 0.5f)
            {
                return new ModerationResult(ModerationStatus.NeedsReview, "图片内容可能不安全", result.Confidence);
            }
        }

        return new ModerationResult(ModerationStatus.Approved);
    }

    #endregion
}
