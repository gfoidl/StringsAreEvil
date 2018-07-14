using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public sealed class ViaRawStream : Variant
    {
        public ViaRawStream(ILineParser lineParser) : base(lineParser) { }

        public override Task ParseAsync(string fileName)
        {
            var sb = new StringBuilder();

            var charPool = ArrayPool<char>.Shared;

            using (var reader = File.OpenRead(fileName))
            {
                try
                {
                    bool endOfFile = false;
                    while (reader.CanRead)
                    {
                        sb.Clear();

                        while (!endOfFile)
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

                            _lineParser.ParseLine(rentedCharBuffer);
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

            return Task.CompletedTask;
        }
    }
}
