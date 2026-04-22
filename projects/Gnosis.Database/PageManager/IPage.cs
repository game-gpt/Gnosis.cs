using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public interface IPage
{
    PageId Id { get; }

    PageType Type { get; }

    int DataLength { get; }

    ReadOnlyMemory<byte> Data { get; }

    SequenceNumber LastModified { get; }
}
