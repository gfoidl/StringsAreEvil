using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public sealed class ViaRawStream2 : Variant
    {
        public ViaRawStream2(ILineParser lineParser) : base(lineParser) { }

        public override Task ParseAsync(string fileName)
        {
            var sb = new StringBuilder();

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

                        _lineParser.ParseLine(sb);
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
