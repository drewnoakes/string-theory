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

        public ulong WastedBytes { get; }

        public StringItem(string content, uint count, uint length, ulong instanceSize, List<ulong> valueAddresses)
        {
            Content = content;
            Count = count;
            Length = length;
            InstanceSize = instanceSize;

            var newLineIndex = content.IndexOfAny(new[] {'\r', '\n'});
            FirstLine = newLineIndex != -1 ? content.Substring(0, newLineIndex) : content;

            WastedBytes = Count == 0 ? 0 : (Count - 1) * InstanceSize;
        }
    }
}