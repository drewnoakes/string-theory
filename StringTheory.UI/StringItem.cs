using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.UI
{
    public sealed class StringItem
    {
        public string FirstLine { get; }
        public string Content { get; }
        public uint Count { get; }
        public uint Length { get; }
        public ulong InstanceSize { get; }
        public List<ulong> ValueAddresses { get; }
        public ulong[] CountBySegmentType { get; }
        public ulong[] CountByGeneration { get; } // offset by zero so -1 becomes 0

        public double Gen0Percent => (double)CountByGeneration[1] / Count;
        public double Gen1Percent => (double)CountByGeneration[2] / Count;
        public double Gen2Percent => (double)CountByGeneration[3] / Count;
        public double LohPercent  => (double)CountBySegmentType[(int)GCSegmentType.LargeObject] / Count;

        public ulong WastedBytes { get; }

        public StringItem(string content, uint count, uint length, ulong instanceSize, List<ulong> valueAddresses, ulong[] countBySegmentType, ulong[] countByGeneration)
        {
            Content = content;
            Count = count;
            Length = length;
            InstanceSize = instanceSize;
            ValueAddresses = valueAddresses;
            CountBySegmentType = countBySegmentType;
            CountByGeneration = countByGeneration;

            var newLineIndex = content.IndexOfAny(new[] {'\r', '\n'});
            FirstLine = newLineIndex != -1 ? content.Substring(0, newLineIndex) : content;

            WastedBytes = Count == 0 ? 0 : (Count - 1) * InstanceSize;
        }
    }
}