//#define ASYNC_IO

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public sealed class ViaPipeReaderSequenceReader : Variant
    {
        private const byte NewLine = (byte)'\n';

        public ViaPipeReaderSequenceReader(ILineParser lineParser) : base(lineParser) { }

        public override async Task ParseAsync(string fileName)
        {
#if ASYNC_IO
            const bool useAsync = true;
#else
            const bool useAsync = false;
#endif
#if DEBUG
            const int bufferSize = 32;
#else
            const int bufferSize = 4096;
#endif

            //using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync);
            //var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: bufferSize));
            var reader = new Internal.FilePipeReader2(fileName, bufferSize);

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

            await reader.CompleteAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseLines(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                ParseLinesFast(buffer.FirstSpan, out int consumed);
                buffer = buffer.Slice(consumed);
                return;
            }

            ParseLinesSlow(ref buffer);
        }

        private void ParseLinesFast(ReadOnlySpan<byte> span, out int consumed)
        {
            int consumedLocal = 0;

            while (!span.IsEmpty)
            {
                int index = span.IndexOf(NewLine);

                if (index != -1)
                {
                    _lineParser.ParseLine(span.Slice(0, index));
                    consumedLocal += index + 1;
                    span = span.Slice(index + 1);
                }
                else
                {
                    break;
                }
            }

            consumed = consumedLocal;
        }

        private void ParseLinesSlow(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            while (reader.TryReadTo(out ReadOnlySequence<byte> line, NewLine, advancePastDelimiter: true))
            {
                ParseLine(line);
            }

            buffer = buffer.Slice(reader.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseLine(in ReadOnlySequence<byte> line)
        {
            if (line.IsSingleSegment)
            {
                _lineParser.ParseLine(line.FirstSpan);
            }
            else
            {
                // Lines are always small so we incur a small copy if we happen to cross a buffer boundary
                ParseLineMultiSegment(line);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [InitLocals(false)]
        private void ParseLineMultiSegment(in ReadOnlySequence<byte> line)
        {
            long lineLength = line.Length;

            if (lineLength <= 256)
            {
                // Small lines we copy to the stack
                Span<byte> stackLine = stackalloc byte[256];
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
}
