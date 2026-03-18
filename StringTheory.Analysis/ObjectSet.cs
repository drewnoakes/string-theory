using Microsoft.Diagnostics.Runtime;
using System.Collections;
using System.Collections.Generic;

namespace StringTheory.Analysis;

/// <summary>
/// A memory-efficient set of heap object addresses, using one bit per potential object slot
/// within each GC segment. This is significantly cheaper than <see cref="HashSet{T}"/>
/// for large sets of heap addresses, as it uses fixed memory proportional to heap size rather
/// than growing with the number of entries.
/// </summary>
/// <remarks>
/// Based on the ObjectSet that was in ClrMD v2 (removed in v3).
/// </remarks>
internal sealed class ObjectSet
{
    private static readonly int MinObjSize = nint.Size * 3;

    private readonly Segment[] _segments;

    public ObjectSet(ClrHeap heap)
    {
        var segments = new List<Segment>(heap.Segments.Length);

        foreach (var seg in heap.Segments)
        {
            ulong start = seg.Start;
            ulong end = seg.End;

            if (start < end)
            {
                segments.Add(new Segment(start, end));
            }
        }

        _segments = segments.ToArray();
    }

    public bool Contains(ulong obj)
    {
        if (TryGetSegment(obj, out var seg))
        {
            int offset = GetOffset(obj, seg);
            return seg.Objects[offset];
        }

        return false;
    }

    public bool Add(ulong obj)
    {
        if (TryGetSegment(obj, out var seg))
        {
            int offset = GetOffset(obj, seg);
            if (seg.Objects[offset])
                return false;

            seg.Objects.Set(offset, true);
            return true;
        }

        return false;
    }

    private static int GetOffset(ulong obj, Segment seg)
    {
        return checked((int)((uint)(obj - seg.StartAddress) / MinObjSize));
    }

    private bool TryGetSegment(ulong obj, out Segment seg)
    {
        if (obj != 0)
        {
            int lower = 0;
            int upper = _segments.Length - 1;

            while (lower <= upper)
            {
                int mid = (lower + upper) >> 1;

                if (obj < _segments[mid].StartAddress)
                    upper = mid - 1;
                else if (obj >= _segments[mid].EndAddress)
                    lower = mid + 1;
                else
                {
                    seg = _segments[mid];
                    return true;
                }
            }
        }

        seg = default;
        return false;
    }

    private readonly struct Segment(ulong startAddress, ulong endAddress)
    {
        public readonly ulong StartAddress = startAddress;
        public readonly ulong EndAddress = endAddress;
        public readonly BitArray Objects = new((int)((uint)(endAddress - startAddress) / MinObjSize), false);
    }
}
