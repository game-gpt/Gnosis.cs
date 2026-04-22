using Gnosis.Asset.Format;
using Gnosis.Asset.VFS;

namespace Gnosis.Asset.Import;

public sealed class ImportContext
{
    public IVirtualFileSystem VFS { get; }
    public IFormatRegistry FormatRegistry { get; }
    public string OutputDirectory { get; }
    public IImportLogger? Logger { get; init; }

    public ImportContext(
        IVirtualFileSystem vfs,
        IFormatRegistry formatRegistry,
        string outputDirectory)
    {
        VFS = vfs ?? throw new ArgumentNullException(nameof(vfs));
        FormatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
        OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    }
}
