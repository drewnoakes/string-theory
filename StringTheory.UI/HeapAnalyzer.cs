using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace StringTheory.UI
{
    public static class HeapAnalyzer
    {
        public static StringSummary GetStringSummary(string dumpFilePath)
        {
            using (DataTarget dataTarget = DataTarget.LoadCrashDump(dumpFilePath))
            {
                ClrInfo runtimeInfo = dataTarget.ClrVersions.First();

                ClrRuntime runtime = runtimeInfo.CreateRuntime();

                var heap = runtime.Heap;

                var tallyByString = new Dictionary<string, ObjectTally>();

                ulong stringCount = 0;
                ulong stringByteCount = 0;
                ulong totalManagedObjectCount = 0;
                ulong totalManagedObjectByteCount = 0;
                long charCount = 0;

                foreach (var instance in heap.EnumerateObjects())
                {
                    totalManagedObjectCount++;
                    totalManagedObjectByteCount += instance.Size;

                    var type = heap.GetObjectType(instance.Address);

                    if (type != null)
                    {
                        if (type.IsString)
                        {
                            var value = (string)type.GetValue(instance.Address);

                            charCount += value.Length;
                            stringCount++;

                            if (tallyByString.TryGetValue(value, out var existingTally))
                            {
                                stringByteCount += existingTally.InstanceSize;
                                existingTally.Add(instance);
                            }
                            else
                            {
                                var newTally = new ObjectTally(instance);
                                stringByteCount += newTally.InstanceSize;
                                tallyByString[value] = newTally;
                            }
                        }
                    }
                }

                var uniqueStringCount = tallyByString.Count;
                var stringCharCount = tallyByString.Sum(s => s.Key.Length * (long)s.Value.Count);
                var uniqueStringCharCount = tallyByString.Keys.Sum(s => s.Length);
                var wastedBytes = tallyByString.Values.Sum(t => (long)t.WastedBytes);
                var stringOverhead = ((double)stringByteCount - (charCount * 2)) / stringCount;

                return new StringSummary(
                    tallyByString.OrderByDescending(p => p.Value.WastedBytes).Select(p => new StringItem(p.Key, (uint) p.Value.Count, (uint) p.Key.Length, p.Value.InstanceSize, p.Value.Addresses)).ToList(),
                    totalManagedObjectByteCount,
                    stringByteCount,
                    (ulong) stringCharCount,
                    (ulong) uniqueStringCharCount,
                    stringCount,
                    (ulong) uniqueStringCount,
                    totalManagedObjectCount,
                    (ulong) wastedBytes,
                    (uint) Math.Round(stringOverhead));
            }
        }

        private sealed class ObjectTally
        {
            public ulong WastedBytes => (Count - 1) * InstanceSize;
            public ulong Count => (ulong) Addresses.Count;
            public ulong InstanceSize { get; }
            public List<ulong> Addresses { get; } = new List<ulong>(capacity: 2);

            public ObjectTally(ClrObject instance)
            {
                InstanceSize = instance.Size;
                Add(instance);
            }

            public void Add(ClrObject instance)
            {
                Addresses.Add(instance.Address);
            }
        }
    }
}