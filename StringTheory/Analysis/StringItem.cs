using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.Analysis
{
    public sealed class StringItem
    {
        private static readonly char[] s_newLineCharacters = {'\r', '\n'};

        public string FirstLine { get; }
        public string Content { get; }
        public uint Length { get; }
        public ulong InstanceSize { get; }
        public HashSet<ulong> ValueAddresses { get; }
        public ulong[] CountBySegmentType { get; }
        public ulong[] CountByGeneration { get; } // offset by zero so -1 becomes 0

        public int Count => ValueAddresses.Count;

        public double Gen0Percent => (double)CountByGeneration[1] / Count;
        public double Gen1Percent => (double)CountByGeneration[2] / Count;
        public double Gen2Percent => (double)CountByGeneration[3] / Count;
        public double LohPercent  => (double)CountBySegmentType[(int)GCSegmentType.LargeObject] / Count;

        public ulong WastedBytes { get; }

        public StringItem(string content, uint length, ulong instanceSize, HashSet<ulong> valueAddresses, ulong[] countBySegmentType, ulong[] countByGeneration)
        {
            Content = content;
            Length = length;
            InstanceSize = instanceSize;
            ValueAddresses = valueAddresses;
            CountBySegmentType = countBySegmentType;
            CountByGeneration = countByGeneration;

            var newLineIndex = content.IndexOfAny(s_newLineCharacters);
            FirstLine = newLineIndex != -1 ? content.Substring(0, newLineIndex) : content;

            WastedBytes = Count == 0 ? 0 : ((ulong)Count - 1) * InstanceSize;
        }
    }
}