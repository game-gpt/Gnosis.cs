namespace Gnosis.Asset.Import;

public interface IImportLogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
}
