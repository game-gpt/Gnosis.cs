namespace Gnosis.Runtime.Sandbox;

[Flags]
public enum ScriptPermission : ulong
{
    None = 0,

    NativeCall = 1UL << 0,
    FileSystemRead = 1UL << 1,
    FileSystemWrite = 1UL << 2,
    NetworkAccess = 1UL << 3,
    EntityAccess = 1UL << 4,
    ComponentCreate = 1UL << 5,
    ComponentDestroy = 1UL << 6,
    QueryExecution = 1UL << 7,
    GlobalVariableRead = 1UL << 8,
    GlobalVariableWrite = 1UL << 9,
    Reflection = 1UL << 10,
    DynamicCodeEval = 1UL << 11,
    CoroutineCreate = 1UL << 12,
    HotReloadTrigger = 1UL << 13,
    DebugAccess = 1UL << 14,

    All = ~0UL
}
