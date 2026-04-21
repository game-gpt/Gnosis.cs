namespace Gnosis.Assets.Formats;

public interface IFormatRegistry
{
    void RegisterHandler(IFormatHandler handler);
    void UnregisterHandler(FormatType formatType);
    IFormatHandler? GetHandler(FormatType formatType);
    IFormatHandler? GetHandler(string path);
    IEnumerable<IFormatHandler> GetAllHandlers();
    bool HasHandler(FormatType formatType);
}
