using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.MonitoringIsEnabled = true;

            var dict = new Dictionary<string, Action>
            {
                ["1"] = () =>
                {
                    Console.WriteLine("#1 ViaStreamReader");
                    ViaStreamReader(new LineParserV01());
                },
                ["2"] = () =>
                {
                    Console.WriteLine("#2 ViaStreamReader");
                    ViaStreamReader(new LineParserV02());
                },
                ["3"] = () =>
                {
                    Console.WriteLine("#3 ViaStreamReader");
                    ViaStreamReader(new LineParserV03());
                },
                ["4"] = () =>
                {
                    Console.WriteLine("#4 ViaStreamReader");
                    ViaStreamReader(new LineParserV04());
                },
                ["5"] = () =>
                {
                    Console.WriteLine("#5 ViaStreamReader");
                    ViaStreamReader(new LineParserV05());
                },
                ["6"] = () =>
                {
                    Console.WriteLine("#6 ViaStreamReader");
                    ViaStreamReader(new LineParserV06());
                },
                ["7"] = () =>
                {
                    Console.WriteLine("#7 ViaStreamReader");
                    ViaStreamReader(new LineParserV07());
                },
                ["8"] = () =>
                {
                    Console.WriteLine("#8 ViaStreamReader");
                    ViaStreamReader(new LineParserV08());
                },
                ["9"] = () =>
                {
                    Console.WriteLine("#9 ViaStreamReader");
                    ViaStreamReader(new LineParserV09());
                },
                ["10"] = () =>
                {
                    Console.WriteLine("#10 ViaStreamReader");
                    ViaStreamReader(new LineParserV10());
                },
                ["11"] = () =>
                {
                    Console.WriteLine("#11 ViaRawStream");
                    ViaRawStream(new LineParserV11());
                },
                ["12"] = () =>
                {
                    Console.WriteLine("#12 ViaRawStream2");
                    ViaRawStream2(new LineParserV12());
                },
                ["13"] = () =>
                {
                    Console.WriteLine("#13 ViaPipeReader");
                    ViaPipeReader(new LineParserPipelinesAndSpan()).Wait();
                },
            };


#if DEBUG
            dict["13"]();
            Environment.Exit(0);
#endif

            if (args.Length == 1 && dict.ContainsKey(args[0]))
            {
                dict[args[0]]();
            }
            else
            {
                Console.WriteLine("Incorrect parameters");
                Environment.Exit(1);
            }

            Console.WriteLine($"Took: {AppDomain.CurrentDomain.MonitoringTotalProcessorTime.TotalMilliseconds:#,###} ms");
            Console.WriteLine($"Allocated: {AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize / 1024:#,#} kb");
            Console.WriteLine($"Peak Working Set: {Process.GetCurrentProcess().PeakWorkingSet64 / 1024:#,#} kb");

            for (var index = 0; index <= GC.MaxGeneration; index++)
            {
                Console.WriteLine($"Gen {index} collections: {GC.CollectionCount(index)}");
            }

            Console.WriteLine(Environment.NewLine);
            Console.ReadLine();
        }

        private static async Task ViaPipeReader(LineParserPipelinesAndSpan lineParser)
        {
            var reader = new FilePipeReader(@"..\..\example-input.csv");

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                ParseLines(lineParser, ref buffer);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private static void ParseLines(ILineParser lineParser, ref ReadOnlySequence<byte> buffer)
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
                    lineParser.ParseLine(span.Slice(0, index));
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
                    ParseLine(lineParser, line);

                    length = (int)line.Length;
                }

                // Advance past the line + the \n
                reader.Advance(length + 1);
            }

            // Update the buffer
            buffer = buffer.Slice(reader.Position);
        }

        private static void ParseLine(ILineParser lineParser, in ReadOnlySequence<byte> line)
        {
            // Lines are always small so we incur a small copy if we happen to cross a buffer boundary
            if (line.IsSingleSegment)
            {
                lineParser.ParseLine(line.First.Span);
            }
            else if (line.Length < 256)
            {
                // Small lines we copy to the stack
                Span<byte> stackLine = stackalloc byte[(int)line.Length];
                line.CopyTo(stackLine);
                lineParser.ParseLine(stackLine);
            }
            else
            {
                // Should be extremely rare
                var length = (int)line.Length;
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                line.CopyTo(buffer);
                lineParser.ParseLine(buffer.AsSpan(0, length));
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void ViaStreamReader(ILineParser lineParser)
        {
            using (StreamReader reader = File.OpenText(@"..\..\example-input.csv"))
            {
                try
                {
                    while (reader.EndOfStream == false)
                    {
                        lineParser.ParseLine(reader.ReadLine());
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("File could not be parsed", exception);
                }
            }
        }

        private static void ViaRawStream(ILineParser lineParser)
        {
            var sb = new StringBuilder();

            var charPool = ArrayPool<char>.Shared;

            using (var reader = File.OpenRead(@"..\..\example-input.csv"))
            {
                try
                {
                    bool endOfFile = false;
                    while (reader.CanRead)
                    {
                        sb.Clear();

                        while (endOfFile == false)
                        {
                            var readByte = reader.ReadByte();

                            if (readByte == -1)
                            {
                                endOfFile = true;
                                break;
                            }

                            var character = (char)readByte;

                            if (character == '\r')
                            {
                                continue;
                            }

                            if (character == '\n')
                            {
                                break;
                            }

                            sb.Append(character);
                        }

                        if (endOfFile)
                        {
                            break;
                        }

                        char[] rentedCharBuffer = charPool.Rent(sb.Length);

                        try
                        {
                            for (int index = 0; index < sb.Length; index++)
                            {
                                rentedCharBuffer[index] = sb[index];
                            }

                            lineParser.ParseLine(rentedCharBuffer);
                        }
                        finally
                        {
                            charPool.Return(rentedCharBuffer, true);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("File could not be parsed", exception);
                }
            }
        }

        private static void ViaRawStream2(ILineParser lineParser)
        {
            var sb = new StringBuilder();

            using (var reader = File.OpenRead(@"..\..\example-input.csv"))
            {
                try
                {
                    bool endOfFile = false;
                    while (reader.CanRead)
                    {
                        sb.Clear();

                        while (endOfFile == false)
                        {
                            var readByte = reader.ReadByte();

                            if (readByte == -1)
                            {
                                endOfFile = true;
                                break;
                            }

                            var character = (char)readByte;

                            if (character == '\r')
                            {
                                continue;
                            }

                            if (character == '\n')
                            {
                                break;
                            }

                            sb.Append(character);
                        }

                        if (endOfFile)
                        {
                            break;
                        }

                        lineParser.ParseLine(sb);
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("File could not be parsed", exception);
                }
            }
        }

        // Simple and incomplete implementation of a pipe reader over a file
        private class FilePipeReader : PipeReader
        {
            private readonly FileStream _stream;
            private int _unconsumedBytes;

            private readonly byte[] _buffer;
            private ReadOnlySequence<byte> _currentSequence;

            public FilePipeReader(string path, int bufferSize = 4096)
            {
                _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1);
                _buffer = new byte[bufferSize];
            }

            public override void AdvanceTo(SequencePosition consumed)
            {
                AdvanceTo(consumed, consumed);
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                var unconsumedBuffer = _currentSequence.Slice(consumed);
                var examinedBuffer = _currentSequence.Slice(examined);

                if (examinedBuffer.Length == 0)
                {
                    // If we didn't consume everything, copy to the front of the buffer
                    if (unconsumedBuffer.Length > 0)
                    {
                        _unconsumedBytes = (int)unconsumedBuffer.Length;

                        unconsumedBuffer.CopyTo(_buffer);
                    }
                }
                else
                {
                    // We didn't examine everything so don't yield the awaiter
                    _currentSequence = unconsumedBuffer;
                }
            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                _stream.Dispose();
            }

            public override void OnWriterCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                // Blocking reads, because we're synchronous
                var read = _stream.Read(_buffer, _unconsumedBytes, _buffer.Length - _unconsumedBytes);

                _currentSequence = new ReadOnlySequence<byte>(_buffer, 0, _unconsumedBytes + read);

                var result = new ReadResult(_currentSequence, isCanceled: false, isCompleted: read == 0);

                return new ValueTask<ReadResult>(result);
            }

            public override bool TryRead(out ReadResult result)
            {
                throw new NotImplementedException();
            }
        }
    }
}
