# NoSQL 存储引擎 (NoSQL Storage Engine)

## 概述

Genesis NoSQL 存储引擎是专为 Genesis 引擎设计的高性能键值存储系统，采用 B+ 树索引、WAL（Write-Ahead Logging）预写日志和 SHM（Shared Memory）共享内存等现代化优化技术，为引擎的时空树、因果锚点、波函数坍缩等核心系统提供低延迟、高吞吐的持久化能力。

> 存储不是目的，而是世界状态的投影。每一次写入都是一次因果锚定，每一次读取都是一次时空回溯。

## 核心设计目标

| 目标 | 指标 | 说明 |
|:---|:---|:---|
| 写入延迟 | < 1μs（SHM 命中） | 通过共享内存实现纳秒级读取 |
| 吞吐量 | > 1M ops/s | WAL 批量提交 + 并发 B+ 树 |
| 崩溃恢复 | 零数据丢失 | WAL 保证持久性语义 |
| 内存效率 | < 50% 堆占用 | SHM 零拷贝 + 页面缓存 |
| 接口贴合度 | 100% C# 惯用接口 | 异步优先、强类型、泛型约束 |

## 架构总览

```
┌─────────────────────────────────────────────────┐
│              应用层 (Application)                │
│  ISpacetimeTree  IHistoryStore  IRepository<T>  │
└───────────┬──────────────┬──────────────┬───────┘
            │              │              │
┌───────────▼──────────────▼──────────────▼───────┐
│           数据库抽象层 (Database Abstraction)     │
│  IKvDatabase  ITransaction  ICursor  ISnapshot   │
└───────────┬──────────────┬──────────────┬───────┘
            │              │              │
┌───────────▼──────────────▼──────────────▼───────┐
│             存储引擎层 (Storage Engine)           │
│  ┌──────────┐  ┌──────────┐  ┌──────────────┐  │
│  │ B+ Tree  │  │   WAL    │  │  SHM Cache   │  │
│  │ Index    │  │ Manager  │  │  Manager     │  │
│  └────┬─────┘  └────┬─────┘  └──────┬───────┘  │
│       │             │               │           │
│  ┌────▼─────────────▼───────────────▼───────┐   │
│  │          Page Manager (页面管理器)        │   │
│  └───────────────────┬─────────────────────┘   │
└──────────────────────┼─────────────────────────┘
                       │
┌──────────────────────▼─────────────────────────┐
│             文件系统层 (File System)             │
│  .genesis/db     .genesis/wal     .genesis/shm  │
└─────────────────────────────────────────────────┘
```

## 模块划分

```
Database/
├── Core/                    # 核心基础类型
│   ├── Enums/
│   │   ├── StorageEngineType.cs
│   │   ├── IsolationLevel.cs
│   │   ├── CompactionMode.cs
│   │   └── PageType.cs
│   ├── Interfaces/
│   │   ├── IKvDatabase.cs
│   │   ├── ITransaction.cs
│   │   ├── ISnapshot.cs
│   │   └── ICursor.cs
│   └── ValueObjects/
│       ├── DatabaseKey.cs
│       ├── DatabaseValue.cs
│       ├── DatabaseEntry.cs
│       ├── DatabaseOptions.cs
│       ├── TransactionId.cs
│       ├── SequenceNumber.cs
│       └── PageId.cs
├── BTree/                   # B+ 树索引引擎
│   ├── Interfaces/
│   │   ├── IBTree.cs
│   │   ├── IBTreeNode.cs
│   │   └── IBTreeCursor.cs
│   └── ValueObjects/
│       ├── BTreeNode.cs
│       ├── BTreeSplitResult.cs
│       └── BTreeMergeResult.cs
├── WAL/                     # 预写日志系统
│   ├── Interfaces/
│   │   ├── IWriteAheadLog.cs
│   │   ├── IWalEntry.cs
│   │   └── IWalReplayer.cs
│   └── ValueObjects/
│       ├── WalEntry.cs
│       ├── WalCheckpoint.cs
│       └── WalOptions.cs
├── SHM/                     # 共享内存缓存
│   ├── Interfaces/
│   │   ├── ISharedMemory.cs
│   │   ├── IShmSegment.cs
│   │   └── IShmAllocator.cs
│   └── ValueObjects/
│       ├── ShmSegmentHeader.cs
│       ├── ShmPageEntry.cs
│       └── ShmOptions.cs
├── PageManager/             # 页面管理器
│   ├── Interfaces/
│   │   ├── IPageManager.cs
│   │   ├── IPage.cs
│   │   └── IFreePageList.cs
│   └── ValueObjects/
│       ├── Page.cs
│       ├── PageHeader.cs
│       └── FreePageList.cs
├── Storage/                 # 存储后端
│   ├── Interfaces/
│   │   ├── IStorageEngine.cs
│   │   └── IFileProvider.cs
│   └── ValueObjects/
│       ├── StorageOptions.cs
│       └── FileMetadata.cs
└── Implementations/         # 具体实现
    ├── GenesisKvDatabase.cs
    ├── BTreeIndex.cs
    ├── WalManager.cs
    ├── ShmCacheManager.cs
    ├── PagedStorageEngine.cs
    └── MemoryMappedFileProvider.cs
```

## 核心接口设计

### IKvDatabase — 键值数据库

```csharp
namespace Genesis.Database.Core.Interfaces;

public interface IKvDatabase : IDisposable, IAsyncDisposable
{
    ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot);
    ISnapshot CreateSnapshot();
    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ICursor Seek(DatabaseKey key);
    DatabaseOptions Options { get; }
    DatabaseStatistics Statistics { get; }
}
```

### ITransaction — 事务

```csharp
namespace Genesis.Database.Core.Interfaces;

public interface ITransaction : IDisposable
{
    TransactionId Id { get; }
    IsolationLevel IsolationLevel { get; }
    Timestamp StartTime { get; }
    bool IsReadOnly { get; }
    bool IsCommitted { get; }
    bool IsRolledBack { get; }
    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ValueTask CommitAsync(CancellationToken cancellationToken = default);
    void Rollback();
}
```

### ISnapshot — 快照

```csharp
namespace Genesis.Database.Core.Interfaces;

public interface ISnapshot : IDisposable
{
    SequenceNumber Sequence { get; }
    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ICursor Seek(DatabaseKey key);
    ISnapshot CreateChild();
}
```

### ICursor — 游标

```csharp
namespace Genesis.Database.Core.Interfaces;

public interface ICursor : IDisposable
{
    DatabaseEntry Current { get; }
    bool IsValid { get; }
    bool MoveNext();
    bool MovePrev();
    bool SeekToFirst();
    bool SeekToLast();
    bool Seek(DatabaseKey key);
    IReadOnlyList<DatabaseEntry> GetRange(DatabaseKey start, DatabaseKey end, int limit = 1000);
}
```

### IWriteAheadLog — 预写日志

```csharp
namespace Genesis.Database.WAL.Interfaces;

public interface IWriteAheadLog : IDisposable, IAsyncDisposable
{
    ValueTask<SequenceNumber> AppendAsync(WalEntry entry, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<WalEntry>> ReadFromAsync(SequenceNumber sequence, CancellationToken cancellationToken = default);
    ValueTask TruncateAsync(SequenceNumber sequence, CancellationToken cancellationToken = default);
    ValueTask CheckpointAsync(WalCheckpoint checkpoint, CancellationToken cancellationToken = default);
    ValueTask<WalCheckpoint?> GetLatestCheckpointAsync(CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
    WalOptions Options { get; }
}
```

### ISharedMemory — 共享内存

```csharp
namespace Genesis.Database.SHM.Interfaces;

public interface ISharedMemory : IDisposable
{
    nint BaseAddress { get; }
    long Size { get; }
    string Name { get; }
    ValueTask<bool> InitializeAsync(long size, CancellationToken cancellationToken = default);
    ValueTask<IShmSegment> AllocateSegmentAsync(int size, CancellationToken cancellationToken = default);
    ValueTask FreeSegmentAsync(IShmSegment segment, CancellationToken cancellationToken = default);
    void Write(long offset, ReadOnlySpan<byte> data);
    void Read(long offset, Span<byte> destination);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
    ShmOptions Options { get; }
}
```

### IBTree — B+ 树索引

```csharp
namespace Genesis.Database.BTree.Interfaces;

public interface IBTree : IDisposable
{
    int Order { get; }
    int Height { get; }
    long Count { get; }
    ValueTask<bool> InsertAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);
    ValueTask<DatabaseValue?> SearchAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);
    IBTreeCursor CreateCursor();
    ValueTask<bool> TrySplitAsync(DatabaseKey key, out BTreeSplitResult result);
    ValueTask<bool> TryMergeAsync(DatabaseKey key, out BTreeMergeResult result);
}
```

### IPageManager — 页面管理器

```csharp
namespace Genesis.Database.PageManager.Interfaces;

public interface IPageManager : IDisposable, IAsyncDisposable
{
    int PageSize { get; }
    long TotalPages { get; }
    ValueTask<PageId> AllocatePageAsync(CancellationToken cancellationToken = default);
    ValueTask<Page> ReadPageAsync(PageId pageId, CancellationToken cancellationToken = default);
    ValueTask WritePageAsync(PageId pageId, Page page, CancellationToken cancellationToken = default);
    ValueTask FreePageAsync(PageId pageId, CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
    IFreePageList FreeList { get; }
}
```

### IStorageEngine — 存储引擎

```csharp
namespace Genesis.Database.Storage.Interfaces;

public interface IStorageEngine : IDisposable, IAsyncDisposable
{
    StorageEngineType Type { get; }
    ValueTask InitializeAsync(StorageOptions options, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(long offset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
    ValueTask CompactAsync(CompactionMode mode, CancellationToken cancellationToken = default);
    long Length { get; }
}
```

## 核心值对象设计

### DatabaseKey — 数据库键

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct DatabaseKey(ReadOnlyMemory<byte> Bytes)
{
    public int Length => Bytes.Length;
    public static DatabaseKey Empty => new(ReadOnlyMemory<byte>.Empty);
    public static DatabaseKey FromString(string value) => new(System.Text.Encoding.UTF8.GetBytes(value));
    public static DatabaseKey FromUInt64(ulong value) => new(BitConverter.GetBytes(value));
    public static DatabaseKey FromGuid(Guid value) => new(value.ToByteArray());
    public bool StartsWith(DatabaseKey prefix);
    public int CompareTo(DatabaseKey other);
}
```

### DatabaseValue — 数据库值

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct DatabaseValue(ReadOnlyMemory<byte> Bytes)
{
    public int Length => Bytes.Length;
    public bool IsEmpty => Bytes.IsEmpty;
    public static DatabaseValue Empty => new(ReadOnlyMemory<byte>.Empty);
    public static DatabaseValue FromString(string value) => new(System.Text.Encoding.UTF8.GetBytes(value));
    public static DatabaseValue FromInt32(int value) => new(BitConverter.GetBytes(value));
    public static DatabaseValue FromInt64(long value) => new(BitConverter.GetBytes(value));
    public static DatabaseValue FromDouble(double value) => new(BitConverter.GetBytes(value));
}
```

### DatabaseEntry — 键值对

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct DatabaseEntry(DatabaseKey Key, DatabaseValue Value)
{
    public static DatabaseEntry Empty => new(DatabaseKey.Empty, DatabaseValue.Empty);
    public bool IsEmpty => Key.IsEmpty && Value.IsEmpty;
}
```

### TransactionId — 事务标识

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct TransactionId(ulong Value)
{
    public static TransactionId New() => new(Interlocked.Increment(ref _counter));
    public static readonly TransactionId Min = new(0);
    private static ulong _counter;
}
```

### SequenceNumber — WAL 序列号

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct SequenceNumber(ulong Value)
{
    public SequenceNumber Next => new(Value + 1);
    public static readonly SequenceNumber Zero = new(0);
    public static readonly SequenceNumber Invalid = new(ulong.MaxValue);
}
```

### PageId — 页面标识

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct PageId(long Value)
{
    public static readonly PageId Invalid = new(-1);
    public static readonly PageId First = new(0);
}
```

## 枚举设计

### StorageEngineType

```csharp
namespace Genesis.Database.Core.Enums;

public enum StorageEngineType
{
    MemoryMappedFile,
    DirectIO,
    InMemory
}
```

### IsolationLevel

```csharp
namespace Genesis.Database.Core.Enums;

public enum IsolationLevel
{
    ReadUncommitted,
    ReadCommitted,
    Snapshot,
    Serializable
}
```

### CompactionMode

```csharp
namespace Genesis.Database.Core.Enums;

public enum CompactionMode
{
    Incremental,
    Full,
    Background
}
```

### PageType

```csharp
namespace Genesis.Database.Core.Enums;

public enum PageType
{
    Free,
    Internal,
    Leaf,
    Overflow,
    Wal,
    Metadata
}
```

## WAL 预写日志系统

### 设计原理

WAL（Write-Ahead Logging）是数据库崩溃恢复的核心机制。所有数据修改在写入数据文件之前，必须先将日志记录持久化到 WAL 文件，确保即使在系统崩溃后也能通过重放日志恢复数据。

### WAL 记录格式

```
┌──────────┬──────────┬──────────┬──────────┬──────────┬──────────┐
│ Sequence │  TxnId   │ EntryType│  KeyLen  │ ValueLen │  CRC32   │
│ (8 bytes)│ (8 bytes)│ (1 byte) │ (4 bytes)│ (4 bytes)│ (4 bytes)│
├──────────┴──────────┴──────────┴──────────┴──────────┴──────────┤
│                          Payload                                │
│                    Key + Value (variable)                        │
└─────────────────────────────────────────────────────────────────┘
```

### WalEntry 值对象

```csharp
namespace Genesis.Database.WAL.ValueObjects;

public readonly record struct WalEntry(
    SequenceNumber Sequence,
    TransactionId TransactionId,
    WalEntryType EntryType,
    DatabaseKey Key,
    DatabaseValue Value,
    uint Checksum)
{
    public uint ComputeChecksum();
}

public enum WalEntryType : byte
{
    Put = 1,
    Delete = 2,
    Commit = 3,
    Rollback = 4,
    Checkpoint = 5
}
```

### WAL 写入流程

```
客户端写入请求
      │
      ▼
┌───────────┐
│ 构造日志  │ ← 序列化为 WalEntry
│ 记录      │
└─────┬─────┘
      │
      ▼
┌───────────┐
│ 追加到    │ ← 顺序写入 WAL 文件
│ WAL 缓冲  │
└─────┬─────┘
      │
      ▼
┌───────────┐
│ fsync     │ ← 确保持久化（可配置延迟刷盘）
│ 持久化    │
└─────┬─────┘
      │
      ▼
┌───────────┐
│ 更新内存  │ ← 写入 B+ 树和 SHM 缓存
│ 数据结构  │
└─────┬─────┘
      │
      ▼
 返回写入确认
```

### WAL 检查点机制

| 参数 | 推荐值 | 说明 |
|:---|:---|:---|
| 检查点间隔 | 1000 次事务 | 触发增量检查点 |
| WAL 文件大小上限 | 64 MB | 超过后滚动到新文件 |
| 检查点超时 | 30 秒 | 确保定期检查点 |
| 刷盘策略 | 每次提交 | 可降级为周期性刷盘 |

### WalCheckpoint 值对象

```csharp
namespace Genesis.Database.WAL.ValueObjects;

public readonly record struct WalCheckpoint(
    SequenceNumber Sequence,
    Timestamp Timestamp,
    long DataFileOffset,
    int DirtyPageCount)
{
    public static readonly WalCheckpoint Zero = new(
        SequenceNumber.Zero,
        Timestamp.Now,
        0,
        0);
}
```

### WalOptions 值对象

```csharp
namespace Genesis.Database.WAL.ValueObjects;

public readonly record struct WalOptions(
    string Directory,
    long MaxFileSize,
    bool SyncOnCommit,
    bool CompressionEnabled,
    int BufferSize)
{
    public static readonly WalOptions Default = new(
        Directory: ".genesis/wal",
        MaxFileSize: 64 * 1024 * 1024,
        SyncOnCommit: true,
        CompressionEnabled: false,
        BufferSize: 64 * 1024);
}
```

## SHM 共享内存缓存

### 设计原理

SHM（Shared Memory）缓存利用操作系统的共享内存机制，实现进程间零拷贝数据共享和纳秒级读取延迟。SHM 作为数据库的热数据层，缓存最近访问的 B+ 树页面和热点键值对。

### SHM 内存布局

```
┌─────────────────────────────────────────────────┐
│              SHM Segment Header (4KB)            │
│  Magic | Version | SegmentCount | FreeListHead  │
├─────────────────────────────────────────────────┤
│              Page Cache Area                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐        │
│  │ Page 0   │ │ Page 1   │ │ Page 2   │ ...    │
│  │ (4KB)    │ │ (4KB)    │ │ (4KB)    │        │
│  └──────────┘ └──────────┘ └──────────┘        │
├─────────────────────────────────────────────────┤
│              Key-Value Cache Area                │
│  ┌──────────────────────────────────────┐       │
│  │  Hash Table (Open Addressing)        │       │
│  │  Slot 0: Key | Value | Timestamp     │       │
│  │  Slot 1: Key | Value | Timestamp     │       │
│  │  ...                                 │       │
│  └──────────────────────────────────────┘       │
├─────────────────────────────────────────────────┤
│              Free Area                           │
│  ┌──────────────────────────────────────┐       │
│  │  Available for allocation            │       │
│  └──────────────────────────────────────┘       │
└─────────────────────────────────────────────────┘
```

### ShmSegmentHeader 值对象

```csharp
namespace Genesis.Database.SHM.ValueObjects;

public readonly record struct ShmSegmentHeader(
    uint Magic,
    uint Version,
    int SegmentCount,
    int FreeListHead,
    long TotalSize,
    long UsedSize,
    Timestamp CreatedAt)
{
    public static readonly uint ExpectedMagic = 0x47454E53; // "GENS"
    public static readonly uint CurrentVersion = 1;
    public double UsageRatio => TotalSize > 0 ? (double)UsedSize / TotalSize : 0;
}
```

### ShmPageEntry 值对象

```csharp
namespace Genesis.Database.SHM.ValueObjects;

public readonly record struct ShmPageEntry(
    PageId PageId,
    int Offset,
    int Size,
    SequenceNumber Sequence,
    Timestamp LastAccess,
    int HitCount)
{
    public ShmPageEntry Touch() => this with
    {
        LastAccess = Timestamp.Now,
        HitCount = HitCount + 1
    };
}
```

### ShmOptions 值对象

```csharp
namespace Genesis.Database.SHM.ValueObjects;

public readonly record struct ShmOptions(
    string Name,
    long MaxSize,
    int PageSize,
    int MaxPageCount,
    double EvictionThreshold,
    bool EnableCrossProcess)
{
    public static readonly ShmOptions Default = new(
        Name: "genesis_db_shm",
        MaxSize: 256 * 1024 * 1024,
        PageSize: 4096,
        MaxPageCount: 65536,
        EvictionThreshold: 0.85,
        EnableCrossProcess: true);
}
```

### SHM 淘汰策略

采用 LRU-K2（最近最少使用 - K=2）淘汰算法，比传统 LRU 更能抵抗顺序扫描污染：

| 参数 | 推荐值 | 说明 |
|:---|:---|:---|
| K 值 | 2 | 记录最近 2 次访问时间 |
| 淘汰阈值 | 85% | 使用率超过阈值触发淘汰 |
| 淘汰批次 | 16 页 | 每次淘汰 16 个页面 |
| 保护窗口 | 100ms | 新页面保护期，避免误淘汰 |

## B+ 树索引引擎

### 设计原理

B+ 树是数据库索引的经典数据结构，所有数据存储在叶子节点，内部节点仅存储键和子指针。B+ 树支持高效的范围查询和顺序扫描，与 Genesis 引擎的时空树查询模式高度契合。

### B+ 树参数

| 参数 | 推荐值 | 说明 |
|:---|:---|:---|
| 阶数（Order） | 128 | 每个节点最多 128 个键 |
| 页面大小 | 4 KB | 与操作系统页面大小对齐 |
| 最小填充率 | 50% | 节点分裂/合并阈值 |
| 键压缩 | 前缀压缩 | 减少内部节点占用 |

### BTreeNode 值对象

```csharp
namespace Genesis.Database.BTree.ValueObjects;

public readonly record struct BTreeNode(
    PageId PageId,
    bool IsLeaf,
    int KeyCount,
    DatabaseKey[] Keys,
    PageId[] Children,
    DatabaseValue[] Values,
    PageId NextLeaf)
{
    public bool IsFull => KeyCount >= Keys.Length;
    public bool IsUnderflow => KeyCount < Keys.Length / 2;
}
```

### BTreeSplitResult / BTreeMergeResult

```csharp
namespace Genesis.Database.BTree.ValueObjects;

public readonly record struct BTreeSplitResult(
    DatabaseKey MiddleKey,
    PageId LeftPageId,
    PageId RightPageId);

public readonly record struct BTreeMergeResult(
    PageId MergedPageId,
    DatabaseKey RemovedKey);
```

## 页面管理器

### 设计原理

页面管理器将文件抽象为固定大小的页面序列，负责页面的分配、回收和读写。所有上层组件（B+ 树、WAL、SHM）通过页面管理器访问底层存储。

### Page 值对象

```csharp
namespace Genesis.Database.PageManager.ValueObjects;

public readonly record struct Page(
    PageId Id,
    PageType Type,
    int DataLength,
    byte[] Data,
    SequenceNumber LastModified)
{
    public static Page Empty(PageId id) => new(id, PageType.Free, 0, [], SequenceNumber.Zero);
}
```

### PageHeader 值对象

```csharp
namespace Genesis.Database.PageManager.ValueObjects;

public readonly record struct PageHeader(
    PageType Type,
    int DataLength,
    SequenceNumber LastModified,
    uint Checksum)
{
    public static readonly int Size = 17;
}
```

### FreePageList 值对象

```csharp
namespace Genesis.Database.PageManager.ValueObjects;

public readonly record struct FreePageList(IReadOnlyList<PageId> FreePages)
{
    public int Count => FreePages.Count;
    public static readonly FreePageList Empty = new([]);
}
```

## 存储后端

### IFileProvider 接口

```csharp
namespace Genesis.Database.Storage.Interfaces;

public interface IFileProvider : IDisposable, IAsyncDisposable
{
    ValueTask<IStorageEngine> CreateEngineAsync(string path, StorageOptions options, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default);
    ValueTask<long> GetSizeAsync(string path, CancellationToken cancellationToken = default);
}
```

### StorageOptions 值对象

```csharp
namespace Genesis.Database.Storage.ValueObjects;

public readonly record struct StorageOptions(
    StorageEngineType EngineType,
    string BasePath,
    int PageSize,
    long InitialSize,
    long MaxSize,
    bool UseDirectIO,
    bool UseSparseFile)
{
    public static readonly StorageOptions Default = new(
        EngineType: StorageEngineType.MemoryMappedFile,
        BasePath: ".genesis/db",
        PageSize: 4096,
        InitialSize: 16 * 1024 * 1024,
        MaxSize: long.MaxValue,
        UseDirectIO: false,
        UseSparseFile: true);
}
```

### FileMetadata 值对象

```csharp
namespace Genesis.Database.Storage.ValueObjects;

public readonly record struct FileMetadata(
    string Path,
    long Size,
    Timestamp CreatedAt,
    Timestamp ModifiedAt,
    StorageEngineType EngineType);
```

## DatabaseOptions — 数据库配置

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct DatabaseOptions(
    string Path,
    StorageOptions Storage,
    WalOptions Wal,
    ShmOptions Shm,
    int BTreeOrder,
    bool ReadOnly,
    bool AutoCheckpoint,
    int CheckpointIntervalMs)
{
    public static readonly DatabaseOptions Default = new(
        Path: ".genesis/db",
        Storage: StorageOptions.Default,
        Wal: WalOptions.Default,
        Shm: ShmOptions.Default,
        BTreeOrder: 128,
        ReadOnly: false,
        AutoCheckpoint: true,
        CheckpointIntervalMs: 30000);
}
```

## DatabaseStatistics — 数据库统计

```csharp
namespace Genesis.Database.Core.ValueObjects;

public readonly record struct DatabaseStatistics(
    long TotalKeys,
    long TotalReads,
    long TotalWrites,
    long CacheHits,
    long CacheMisses,
    long WalEntries,
    long FreePages,
    long UsedPages)
{
    public double CacheHitRate => TotalReads > 0 ? (double)CacheHits / TotalReads : 0;
    public double PageUsageRatio => (UsedPages + FreePages) > 0 ? (double)UsedPages / (UsedPages + FreePages) : 0;
}
```

## 与现有系统的集成

### 与 Persistence 模块的关系

Database 模块是 Persistence 模块的底层引擎，提供高性能的存储原语：

```
Persistence (IHistoryStore, IRepository<T>, IIncrementalSave)
      │
      │  内部使用
      ▼
Database (IKvDatabase, ITransaction, ISnapshot)
      │
      │  内部使用
      ▼
BTree + WAL + SHM + PageManager
```

### 与 Spacetime 模块的集成

时空树节点的持久化通过 Database 模块实现：

| 时空树操作 | 数据库操作 | 说明 |
|:---|:---|:---|
| 节点创建 | Put(SpatialHash → NodeData) | 以空间哈希为键存储节点 |
| 节点查询 | Get(SpatialHash) | 通过 B+ 树快速定位 |
| 范围扫描 | Seek(MinHash).GetRange(Min, Max) | B+ 树范围查询 |
| 历史回溯 | Snapshot(Sequence).Get(HistoryHash) | 快照隔离读取 |
| 批量更新 | Transaction(Put × N).Commit() | 事务保证原子性 |

### 与 Causal 模块的集成

因果锚点的权重更新通过 WAL 保证持久性：

```
因果锚点权重更新
      │
      ▼
┌───────────────┐
│ 开启事务      │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ WAL 记录写入  │ ← 保证崩溃恢复
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ SHM 缓存更新  │ ← 保证读取性能
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ B+ 树索引更新 │ ← 保证查询效率
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ 提交事务      │
└───────────────┘
```

## 性能优化策略

| 策略 | 说明 | 预期收益 |
|:---|:---|:---|
| WAL 批量提交 | 多个事务合并一次 fsync | 写入吞吐提升 5-10x |
| SHM 零拷贝读取 | 直接从共享内存读取，无反序列化 | 读取延迟降低 90% |
| B+ 树前缀压缩 | 内部节点键前缀压缩 | 索引内存减少 40-60% |
| 页面预读 | 顺序访问时预读后续页面 | 范围扫描吞吐提升 2-3x |
| 延迟刷盘 | WAL 可配置延迟刷盘策略 | 写入延迟降低 50% |
| 后台压缩 | 后台线程执行增量压缩 | 减少空间碎片，不阻塞读写 |
| Memory-Mapped I/O | 使用 OS 页面缓存 | 减少内核态拷贝 |

## 崩溃恢复流程

```
系统启动
      │
      ▼
┌───────────────┐
│ 读取最新      │
│ WAL 检查点    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ 重放检查点后  │ ← 从检查点序列号开始
│ 的 WAL 记录   │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ 重建 B+ 树    │ ← 应用所有 Put/Delete 操作
│ 索引          │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ 初始化 SHM    │ ← 加载热点页面到共享内存
│ 缓存          │
└───────┬───────┘
        │
        ▼
 数据库就绪
```

## 文件格式

### 数据库文件 (.genesis/db)

```
┌─────────────────────────────────────────┐
│  文件头 (4KB)                            │
│  Magic | Version | PageSize | PageCount │
├─────────────────────────────────────────┤
│  Page 0: 元数据页                        │
├─────────────────────────────────────────┤
│  Page 1: B+ 树根节点                     │
├─────────────────────────────────────────┤
│  Page 2-N: B+ 树内部/叶子节点            │
├─────────────────────────────────────────┤
│  Page N+1-M: 溢出页                      │
├─────────────────────────────────────────┤
│  Page M+1-...: 空闲页                    │
└─────────────────────────────────────────┘
```

### WAL 文件 (.genesis/wal)

```
┌─────────────────────────────────────────┐
│  WAL 头 (4KB)                            │
│  Magic | Version | StartSequence         │
├─────────────────────────────────────────┤
│  Entry 1: [Header | Key | Value | CRC]  │
├─────────────────────────────────────────┤
│  Entry 2: [Header | Key | Value | CRC]  │
├─────────────────────────────────────────┤
│  ...                                    │
├─────────────────────────────────────────┤
│  Checkpoint: [Header | Metadata | CRC]  │
└─────────────────────────────────────────┘
```

### SHM 文件 (.genesis/shm)

```
┌─────────────────────────────────────────┐
│  SHM 头 (4KB)                            │
│  Magic | Version | Size | SegmentCount  │
├─────────────────────────────────────────┤
│  页面缓存区 (N × PageSize)              │
├─────────────────────────────────────────┤
│  键值缓存区 (Hash Table)                │
├─────────────────────────────────────────┤
│  空闲区域                                │
└─────────────────────────────────────────┘
```
