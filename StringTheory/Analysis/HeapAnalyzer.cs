using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.Analysis;

public sealed class HeapAnalyzer
{
    private readonly DataTarget _dataTarget;
    private readonly ClrHeap _heap;

    private int _leaseCount;

    public HeapAnalyzer(string dumpFilePath)
        : this(DataTarget.LoadDump(dumpFilePath))
    {
    }

    public HeapAnalyzer(int pid)
        : this(DataTarget.AttachToProcess(pid, suspend: false))
    {
    }

    private HeapAnalyzer(DataTarget dataTarget)
    {
        _dataTarget = dataTarget;

        var clrInfo = _dataTarget.ClrVersions.FirstOrDefault();

        if (clrInfo == null)
            throw new Exception("Could not find any CLR version in the target.");

        _heap = clrInfo.CreateRuntime().Heap;

        // TODO if !_heap.CanWalkHeap then the process/dump may be in a state where walking is unreliable (e.g. middle of GC)
    }

    public StringSummary GetStringSummary(Action<double>? progressCallback = null, CancellationToken token = default)
    {
        var tallyByString = new Dictionary<string, ObjectTally>();

        ulong stringCount = 0;
        ulong stringByteCount = 0;
        ulong totalManagedObjectCount = 0;
        ulong totalManagedObjectByteCount = 0;
        long charCount = 0;

        for (var i = 0; i < _heap.Segments.Length; i++)
        {
            progressCallback?.Invoke((double)i / _heap.Segments.Length);

            var seg = _heap.Segments[i];
            var segKind = seg.Kind;

            foreach (var clrObj in seg.EnumerateObjects())
            {
                if (!clrObj.IsValid)
                    continue;

                var type = clrObj.Type;
                if (type == null)
                    continue;

                token.ThrowIfCancellationRequested();

                var generation = seg.GetGeneration(clrObj);

                var size = clrObj.Size;

                totalManagedObjectCount++;
                totalManagedObjectByteCount += size;

                if (type.IsString)
                {
                    var value = clrObj.AsString(int.MaxValue);
                    if (value == null)
                        continue;

                    charCount += value.Length;
                    stringCount++;

                    ref var tally = ref CollectionsMarshal.GetValueRefOrAddDefault(tallyByString, value, out bool exists);
                    if (!exists)
                    {
                        tally = new ObjectTally(size);
                    }

                    stringByteCount += tally!.InstanceSize;
                    tally.Add(clrObj.Address, segKind, generation);
                }
            }
        }

        progressCallback?.Invoke(1.0);

        var uniqueStringCount = tallyByString.Count;
        var stringCharCount = tallyByString.Sum(s => s.Key.Length*(long)s.Value.Count);
        var uniqueStringCharCount = tallyByString.Keys.Sum(s => s.Length);
        var wastedBytes = tallyByString.Values.Sum(t => (long)t.WastedBytes);
        var stringOverhead = ((double)stringByteCount - (charCount*2))/stringCount;

        return new StringSummary(
            tallyByString.OrderByDescending(p => p.Value.WastedBytes)
                .Select(
                    p => new StringItem(
                        p.Key,
                        (uint)p.Key.Length,
                        p.Value.InstanceSize,
                        p.Value.Addresses,
                        p.Value.CountBySegmentType,
                        p.Value.CountByGeneration)).ToList(),
            totalManagedObjectByteCount,
            stringByteCount,
            (ulong)stringCharCount,
            (ulong)uniqueStringCharCount,
            stringCount,
            (ulong)uniqueStringCount,
            totalManagedObjectCount,
            (ulong)wastedBytes,
            (uint)Math.Round(stringOverhead));
    }

    public StringSummary GetTypeReferenceStringSummary(ClrType referrerType, int fieldOffset, CancellationToken token = default)
    {
        var tallyByString = new Dictionary<string, ObjectTally>();

        ulong stringCount = 0;
        ulong stringByteCount = 0;
        ulong totalManagedObjectByteCount = 0;
        long charCount = 0;

        ClrInstanceField? stringField = null;
        foreach (var f in referrerType.Fields)
        {
            if (f.Offset == fieldOffset && f.IsObjectReference)
            {
                stringField = f;
                break;
            }
        }

        foreach (var seg in _heap.Segments)
        {
            var segKind = seg.Kind;

            foreach (var clrObj in seg.EnumerateObjects())
            {
                if (!clrObj.IsValid)
                    continue;

                var referringType = clrObj.Type;
                if (referringType == null)
                    continue;

                totalManagedObjectByteCount += clrObj.Size;

                if (!ReferenceEquals(referringType, referrerType))
                    continue;

                token.ThrowIfCancellationRequested();

                ClrObject strObj;
                if (stringField != null)
                {
                    strObj = stringField.ReadObject(clrObj.Address, false);
                }
                else
                {
                    continue;
                }

                if (strObj.IsNull || !strObj.IsValid)
                    continue;

                var value = strObj.AsString(int.MaxValue);
                if (value == null)
                    continue;

                charCount += value.Length;
                stringCount++;

                ref var tally = ref CollectionsMarshal.GetValueRefOrAddDefault(tallyByString, value, out bool exists);
                if (!exists)
                {
                    tally = new ObjectTally(strObj.Size);
                }

                var strSeg = _heap.GetSegmentByAddress(strObj.Address);
                if (strSeg == null)
                    continue;
                var generation = strSeg.GetGeneration(strObj.Address);
                if (tally!.Add(strObj.Address, segKind, generation))
                {
                    stringByteCount += tally.InstanceSize;
                }
            }
        }

        var uniqueStringCount = tallyByString.Count;
        var stringCharCount = tallyByString.Sum(s => s.Key.Length * (long)s.Value.Count);
        var uniqueStringCharCount = tallyByString.Keys.Sum(s => s.Length);
        var wastedBytes = tallyByString.Values.Sum(t => (long)t.WastedBytes);
        var stringOverhead = ((double)stringByteCount - (charCount * 2)) / stringCount;

        return new StringSummary(
            tallyByString.OrderByDescending(p => p.Value.WastedBytes)
                .Select(
                    p => new StringItem(
                        p.Key,
                        (uint)p.Key.Length,
                        p.Value.InstanceSize,
                        p.Value.Addresses,
                        p.Value.CountBySegmentType,
                        p.Value.CountByGeneration)).ToList(),
            totalManagedObjectByteCount,
            stringByteCount,
            (ulong)stringCharCount,
            (ulong)uniqueStringCharCount,
            stringCount,
            (ulong)uniqueStringCount,
            ulong.MaxValue, // TODO review
            (ulong)wastedBytes,
            (uint)Math.Round(stringOverhead));
    }

    public IDisposable GetLease()
    {
        Interlocked.Increment(ref _leaseCount);

        return new DisposableAction(
            () =>
            {
                if (Interlocked.Decrement(ref _leaseCount) == 0)
                {
                    _dataTarget?.Dispose();
                }
            });
    }

    private sealed class ObjectTally
    {
        public ulong[] CountBySegmentType { get; } = new ulong[7];
        public ulong[] CountByGeneration { get; } = new ulong[7];
        public ulong WastedBytes => Count == 0 ? 0ul : (Count - 1) * InstanceSize;
        public ulong Count => (ulong) Addresses.Count;
        public ulong InstanceSize { get; }
        public HashSet<ulong> Addresses { get; } = new HashSet<ulong>(capacity: 2);

        public ObjectTally(ulong size)
        {
            if (size == 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Cannot be zero.");
            InstanceSize = size;
        }

        public bool Add(ulong address, GCSegmentKind segmentKind, Generation generation)
        {
            if (Addresses.Add(address))
            {
                CountBySegmentType[(int) segmentKind]++;
                CountByGeneration[(int) generation]++;
                return true;
            }

            return false;
        }
    }

    public ReferenceGraph GetReferenceGraph(HashSet<ulong> targetAddresses, CancellationToken token = default)
    {
        return ReferenceGraphBuilder.Build(_heap, targetAddresses, token);
    }
}