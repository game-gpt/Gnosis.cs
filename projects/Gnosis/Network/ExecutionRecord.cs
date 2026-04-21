namespace Gnosis.Network;

/// <summary>
/// 确定性执行记录，用于调试和回放
/// </summary>
public readonly record struct ExecutionRecord(
    string FunctionName,
    int Frame,
    byte[] Input,
    byte[] Result
);
