# Fmacias.TplQueue.Cache.Abstract

Cache abstraction module for `TplQueue`.  
It defines reusable cache orchestration behavior (`CacheAbstract`) plus internal domain models and factories used by concrete cache providers.

## Table of Contents

1. [Summary](#summary)
2. [Module Scope](#module-scope)
3. [Public Surface](#public-surface)
4. [How to Implement a Custom File-System Cache](#how-to-implement-a-custom-file-system-cache)
5. [Design Decisions and Patterns](#design-decisions-and-patterns)
6. [Notes for .NET Standard 2.0](#notes-for-net-standard-20)

## Summary

`Fmacias.TplQueue.Cache.Abstract` separates cache orchestration from persistence details:

- Graph dehydration/hydration is handled in a reusable base class.
- Persistence is delegated to `ICacheRepository`.
- Type resolution and node/job materialization are delegated to abstractions (`INodeTypeResolver`, `IPayloadJobFactory`, `ICacheEntryFactory`).

This keeps the module open for production providers like SQLite or file-system implementations without changing public APIs.

## Module Scope

- `CacheAbstract`: base orchestration for cache workflows.
- Internal domain models:
- `CacheEntry`
- `JobNodeDto`
- `DefaultNodeTypeResolver`
- Factory entry points:
- `CacheEntryFactory`
- `DefaultNodeTypeResolverFactory`

Concrete providers should inherit `CacheAbstract` and implement repository/storage behavior outside this module.

## Public Surface

- `CacheAbstract` (abstract): default implementation for `IPayloadJobCache`.
- `CacheEntryFactory`: creates `ICacheEntry` instances.
- `DefaultNodeTypeResolverFactory`: creates `INodeTypeResolver` instances.

All internal domain model objects are instantiated through static `Create(...)` factory methods to enforce invariants.

## How to Implement a Custom File-System Cache

Below is a minimal, step-by-step example that can later evolve to SQLite with the same contracts.

### Step 1: Implement `ICacheRepository`

Use one file per root graph, or a single append-only file with snapshots.

```csharp
public sealed class FileSystemCacheRepository : ICacheRepository
{
    private readonly string _basePath;
    private readonly object _sync = new object();
    private readonly Dictionary<Guid, ICacheEntry> _entries = new Dictionary<Guid, ICacheEntry>();

    public FileSystemCacheRepository(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath)) throw new ArgumentException("Base path is required.", nameof(basePath));
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public void Upsert(ICacheEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        lock (_sync)
        {
            _entries[entry.JobId] = entry;
            PersistSnapshot();
        }
    }

    public bool TryGet(Guid jobId, out ICacheEntry entry)
    {
        lock (_sync)
        {
            return _entries.TryGetValue(jobId, out entry);
        }
    }

    public ICacheEntry[] SnapshotAll()
    {
        lock (_sync) return _entries.Values.ToArray();
    }

    public void TryRemove(Guid jobId)
    {
        lock (_sync)
        {
            _entries.Remove(jobId);
            PersistSnapshot();
        }
    }

    public ICacheEntry SelectOldestPendingRoot()
    {
        lock (_sync)
        {
            return _entries.Values
                .Where(v => v.IsRoot && v.Status == EntryStatus.Pending && !v.Deleted)
                .OrderBy(v => v.CacheUtc)
                .FirstOrDefault();
        }
    }

    public IOrderedEnumerable<ICacheEntry> SelectPendingChildren(Guid parentJobId)
    {
        lock (_sync)
        {
            return _entries.Values
                .Where(v => v.ParentJobId == parentJobId && v.Status == EntryStatus.Pending && !v.Deleted)
                .OrderBy(v => v.CacheUtc);
        }
    }

    private void PersistSnapshot()
    {
        // Serialize only storage DTOs here; avoid serializing runtime interfaces directly.
    }
}
```

### Step 2: Derive from `CacheAbstract`

```csharp
public sealed class FileSystemCache : CacheAbstract
{
    public FileSystemCache(
        IUniversalPayloadSerializer serializer,
        ICacheRepository cacheRepository,
        INodeTypeResolver typeResolver,
        IPayloadJobFactory payloadJobFactory,
        ICacheEntryFactory cacheEntryFactory)
        : base(serializer, cacheRepository, typeResolver, payloadJobFactory, cacheEntryFactory)
    {
    }

    protected override Action<IJobNodeDto, Guid> OnDehydration =>
        (node, rootId) =>
        {
            var entry = CacheEntryFactory.CreateCacheEntry(
                leaseId: Guid.NewGuid(),
                jobRootId: rootId,
                jobNodeDto: node,
                cacheUtc: DateTime.UtcNow);

            CacheRepository.Upsert(entry);
        };
}
```

### Step 3: Wire it in your composition root

```csharp
var cache = new FileSystemCache(
    serializer,
    new FileSystemCacheRepository("C:\\tplqueue-cache"),
    DefaultNodeTypeResolverFactory.Create().CreateResolver(),
    payloadJobFactory,
    CacheEntryFactory.Create());
```

### Step 4: Evolve to SQLite without changing orchestration

- Keep `CacheAbstract` unchanged.
- Replace only repository implementation (`ICacheRepository`) with SQLite-backed persistence.
- Optionally replace `INodeTypeResolver` with a whitelist resolver for hardened environments.

## Design Decisions and Patterns

### Factory Pattern

- Public factories (`CacheEntryFactory`, `DefaultNodeTypeResolverFactory`) hide concrete internals.
- Benefits:
- Reduces coupling to implementation classes.
- Keeps the module DI-container agnostic.
- Simplifies testing via interface-driven setup.

### Static Factory Methods in Internal Domain Models

- `Create(...)` methods centralize validation and prevent invalid object states.
- Benefits:
- Enforces fail-fast behavior at construction time.
- Keeps constructors private/internal and controlled.

### Template Method Style in `CacheAbstract`

- `CacheAbstract` defines reusable workflow and delegates persistence callback through `OnDehydration`.
- Benefits:
- Shared algorithm remains stable across providers.
- Concrete providers customize only storage behavior.

### Dependency Inversion

- Core logic depends on abstractions (`ICacheRepository`, `IPayloadJobFactory`, `INodeTypeResolver`).
- Benefits:
- Allows alternative storage engines (memory, file system, SQLite) with minimal changes.

## Notes for .NET Standard 2.0

- The project target (`netstandard2.0`) is valid for the abstraction role.
- Keep implementation free of runtime-specific APIs to preserve compatibility.
- If nullable annotations are used (`string?`), ensure your build pipeline and consumers are aligned on nullable context settings.

