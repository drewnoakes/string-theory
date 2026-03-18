using System.Collections.Generic;

namespace StringTheory.Analysis;

public sealed class StringSummary(
    IReadOnlyList<StringItem> strings,
    ulong heapByteCount,
    ulong stringByteCount,
    ulong stringCharacterCount,
    ulong uniqueStringCharCount,
    ulong stringCount,
    ulong uniqueStringCount,
    ulong managedObjectCount,
    ulong wastedBytes,
    uint stringOverhead)
{
    public IReadOnlyList<StringItem> Strings { get; } = strings;
    public ulong HeapByteCount { get; } = heapByteCount;
    public ulong StringByteCount { get; } = stringByteCount;
    public ulong StringCharacterCount { get; } = stringCharacterCount;
    public ulong UniqueStringCharCount { get; } = uniqueStringCharCount;
    public ulong StringCount { get; } = stringCount;
    public ulong UniqueStringCount { get; } = uniqueStringCount;
    public ulong ManagedObjectCount { get; } = managedObjectCount;
    public ulong WastedBytes { get; } = wastedBytes;
    public uint StringOverhead { get; } = stringOverhead;
}