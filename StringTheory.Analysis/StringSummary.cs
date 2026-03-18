using System.Collections.Generic;

namespace StringTheory.Analysis;

public sealed class StringSummary(
    IReadOnlyList<StringItem> strings,
    ulong heapByteCount,
    ulong stringByteCount,
    ulong stringCount,
    ulong uniqueStringCount,
    ulong wastedBytes)
{
    public IReadOnlyList<StringItem> Strings { get; } = strings;
    public ulong HeapByteCount { get; } = heapByteCount;
    public ulong StringByteCount { get; } = stringByteCount;
    public ulong StringCount { get; } = stringCount;
    public ulong UniqueStringCount { get; } = uniqueStringCount;
    public ulong WastedBytes { get; } = wastedBytes;
}