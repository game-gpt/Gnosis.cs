namespace Gnosis.Security.Moderation;

public interface ITextFilter
{
    string Filter(string text);
    bool ContainsSensitiveWord(string text);
    void AddWord(string word);
    void RemoveWord(string word);
}
