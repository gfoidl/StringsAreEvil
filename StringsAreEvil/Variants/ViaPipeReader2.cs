using System;
using System.Buffers;
using System.Threading.Tasks;
using StringsAreEvil.Internal;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace StringsAreEvil
{
#if NETCOREAPP2_1
    public sealed class ViaPipeReader2 : Variant
    {
        public ViaPipeReader2(ILineParser lineParser) : base(lineParser) { }

        public override async Task ParseAsync(string fileName)
        {
            var reader = new FilePipeReader2(fileName, 1024 << 2);

            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                ParseLines(ref buffer);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private void ParseLines(ref ReadOnlySequence<byte> buffer)
        {
            const byte newLine = (byte)'\n';

            var reader = new BufferReader(buffer);

            while (!reader.End)
            {
                ReadOnlySpan<byte> span = reader.UnreadSegment;
                int index = span.IndexOf(newLine);
                int length = 0;

                if (index != -1)
                {
                    length = index;
                    _lineParser.ParseLine(span.Slice(0, index));
                }
                else
                {
                    // We didn't find the new line in the current segment, see if it's 
                    // another segment
                    SequencePosition current = reader.Position;
                    ReadOnlySequence<byte> currentSequence = buffer.Slice(current);
                    SequencePosition? linePos = currentSequence.PositionOf(newLine);

                    if (linePos == null)
                    {
                        // Nope
                        break;
                    }

                    // We found one, so get the line and parse it
                    ReadOnlySequence<byte> line = currentSequence.Slice(0, linePos.Value);
                    ParseLine(line);

                    length = (int)line.Length;
                }

                // Advance past the line + the \n
                reader.Advance(length + 1);
            }

            // Update the buffer
            buffer = buffer.Slice(reader.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseLine(in ReadOnlySequence<byte> line)
        {
            if (line.IsSingleSegment)
            {
                _lineParser.ParseLine(line.First.Span);
            }
            else
            {
                // Lines are always small so we incur a small copy if we happen to cross a buffer boundary
                ParseLineMultiSegment(line);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ParseLineMultiSegment(in ReadOnlySequence<byte> line)
        {
            if (line.Length < 256)
            {
                // Small lines we copy to the stack
                Span<byte> stackLine = stackalloc byte[(int)line.Length];
                line.CopyTo(stackLine);
                _lineParser.ParseLine(stackLine);
            }
            else
            {
                // Should be extremely rare
                var length = (int)line.Length;
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    line.CopyTo(buffer);
                    _lineParser.ParseLine(buffer.AsSpan(0, length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
#endif
}
