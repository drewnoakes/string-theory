using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.Analysis;

public sealed class StringItem
{
    private static readonly SearchValues<char> s_newLineCharacters = SearchValues.Create(['\r', '\n']);

    public string FirstLine { get; }
    public string Content { get; }
    public uint Length { get; }
    public ulong InstanceSize { get; }
    public HashSet<ulong> ValueAddresses { get; }
    public ulong[] CountBySegmentType { get; }
    public ulong[] CountByGeneration { get; } // offset by zero so -1 becomes 0

    public int Count => ValueAddresses.Count;

    public double Gen0Percent => (double)CountByGeneration[(int)Generation.Generation0] / Count;
    public double Gen1Percent => (double)CountByGeneration[(int)Generation.Generation1] / Count;
    public double Gen2Percent => (double)CountByGeneration[(int)Generation.Generation2] / Count;
    public double LohPercent  => (double)CountBySegmentType[(int)GCSegmentKind.Large] / Count;

    public ulong WastedBytes { get; }

    public StringItem(string content, uint length, ulong instanceSize, HashSet<ulong> valueAddresses, ulong[] countBySegmentType, ulong[] countByGeneration)
    {
        Content = content;
        Length = length;
        InstanceSize = instanceSize;
        ValueAddresses = valueAddresses;
        CountBySegmentType = countBySegmentType;
        CountByGeneration = countByGeneration;

        var newLineIndex = content.AsSpan().IndexOfAny(s_newLineCharacters);
        FirstLine = newLineIndex != -1 ? content[..newLineIndex] : content;

        WastedBytes = Count == 0 ? 0 : ((ulong)Count - 1) * InstanceSize;
    }
}