using System.Collections.Generic;

namespace StringTheory.UI
{
    public sealed class StringItem
    {
        public string FirstLine { get; }
        public string Content { get; }
        public uint Count { get; }
        public uint Length { get; }
        public ulong InstanceSize { get; }
        public ulong[] CountBySegmentType { get; }
        public ulong[] CountByGeneration { get; } // offset by zero so -1 becomes 0

        public ulong WastedBytes { get; }

        public StringItem(string content, uint count, uint length, ulong instanceSize, List<ulong> valueAddresses, ulong[] countBySegmentType, ulong[] countByGeneration)
        {
            Content = content;
            Count = count;
            Length = length;
            InstanceSize = instanceSize;
            CountBySegmentType = countBySegmentType;
            CountByGeneration = countByGeneration;

            var newLineIndex = content.IndexOfAny(new[] {'\r', '\n'});
            FirstLine = newLineIndex != -1 ? content.Substring(0, newLineIndex) : content;

            WastedBytes = Count == 0 ? 0 : (Count - 1) * InstanceSize;
        }
    }
}