using System.Collections.Generic;

namespace StringTheory.UI
{
    public sealed class StringSummary
    {
        public IReadOnlyList<StringItem> Strings { get; }
        public ulong HeapByteCount { get; }
        public ulong StringByteCount { get; }
        public ulong StringCharacterCount { get; }
        public ulong UniqueStringCharCount { get; }
        public ulong StringCount { get; }
        public ulong UniqueStringCount { get; }
        public ulong ManagedObjectCount { get; }
        public ulong WastedBytes { get; }
        public uint StringOverhead { get; }

        public StringSummary(
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
            Strings = strings;
            HeapByteCount = heapByteCount;
            StringByteCount = stringByteCount;
            StringCharacterCount = stringCharacterCount;
            UniqueStringCharCount = uniqueStringCharCount;
            StringCount = stringCount;
            UniqueStringCount = uniqueStringCount;
            ManagedObjectCount = managedObjectCount;
            WastedBytes = wastedBytes;
            StringOverhead = stringOverhead;
        }
    }
}