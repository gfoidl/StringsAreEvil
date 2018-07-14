using System;
using System.IO;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    public sealed class ViaStreamReader : Variant
    {
        public ViaStreamReader(ILineParser lineParser) : base(lineParser) { }

        public override Task ParseAsync(string fileName)
        {
            using (StreamReader reader = File.OpenText(fileName))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        _lineParser.ParseLine(reader.ReadLine());
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
