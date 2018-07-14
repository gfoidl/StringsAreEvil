using System;
using System.Buffers;
using System.Threading.Tasks;
using StringsAreEvil.Internal;

namespace StringsAreEvil
{
    public sealed class ViaPipeReader : Variant
    {
        public ViaPipeReader(ILineParser lineParser) : base(lineParser) { }

        public override async Task ParseAsync(string fileName)
        {
            var reader = new FilePipeReader(fileName);

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

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
                var span = reader.UnreadSegment;
                var index = span.IndexOf(newLine);
                var length = 0;

                if (index != -1)
                {
                    length = index;
                    _lineParser.ParseLine(span.Slice(0, index));
                }
                else
                {
                    // We didn't find the new line in the current segment, see if it's 
                    // another segment
                    var current = reader.Position;
                    var linePos = buffer.Slice(current).PositionOf(newLine);

                    if (linePos == null)
                    {
                        // Nope
                        break;
                    }

                    // We found one, so get the line and parse it
                    var line = buffer.Slice(current, linePos.Value);
                    ParseLine(line);

                    length = (int)line.Length;
                }

                // Advance past the line + the \n
                reader.Advance(length + 1);
            }

            // Update the buffer
            buffer = buffer.Slice(reader.Position);
        }

        private void ParseLine(in ReadOnlySequence<byte> line)
        {
            // Lines are always small so we incur a small copy if we happen to cross a buffer boundary
            if (line.IsSingleSegment)
            {
                _lineParser.ParseLine(line.First.Span);
            }
            else if (line.Length < 256)
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
                line.CopyTo(buffer);
                _lineParser.ParseLine(buffer.AsSpan(0, length));
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
