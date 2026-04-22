namespace Gnosis.Security.Moderation;

public sealed class SensitiveWordFilter : ITextFilter
{
    #region 字段

    private readonly TrieNode _root = new();
    private char _maskChar = '*';

    #endregion

    #region 属性

    public int WordCount => _root.Count;
    public char MaskChar { get => _maskChar; set => _maskChar = value; }

    #endregion

    #region 公开方法

    public string Filter(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var matchLength = FindMatch(chars, i);

            if (matchLength > 0)
            {
                for (var j = i; j < i + matchLength; j++)
                {
                    chars[j] = _maskChar;
                }

                i += matchLength - 1;
            }
        }

        return new string(chars);
    }

    public bool ContainsSensitiveWord(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (FindMatch(chars, i) > 0) return true;
        }

        return false;
    }

    public void AddWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        var node = _root;

        foreach (var c in word)
        {
            if (!node.Children.TryGetValue(c, out var child))
            {
                child = new TrieNode();
                node.Children[c] = child;
            }

            node = child;
        }

        node.IsEnd = true;
    }

    public void RemoveWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;
        RemoveWordHelper(_root, word, 0);
    }

    #endregion

    #region 私有方法

    private int FindMatch(char[] chars, int startIndex)
    {
        var node = _root;
        var matchLength = 0;

        for (var i = startIndex; i < chars.Length; i++)
        {
            if (!node.Children.TryGetValue(chars[i], out var child)) break;

            node = child;
            matchLength++;

            if (node.IsEnd) return matchLength;
        }

        return 0;
    }

    private bool RemoveWordHelper(TrieNode node, string word, int index)
    {
        if (index == word.Length)
        {
            if (!node.IsEnd) return false;
            node.IsEnd = false;
            return node.Children.Count == 0;
        }

        if (!node.Children.TryGetValue(word[index], out var child)) return false;

        var shouldRemove = RemoveWordHelper(child, word, index + 1);

        if (shouldRemove)
        {
            node.Children.Remove(word[index]);
            return !node.IsEnd && node.Children.Count == 0;
        }

        return false;
    }

    #endregion

    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public bool IsEnd { get; set; }

        public int Count
        {
            get
            {
                var count = IsEnd ? 1 : 0;
                foreach (var child in Children.Values) count += child.Count;
                return count;
            }
        }
    }
}
