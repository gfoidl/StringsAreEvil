using System;
using System.Text;

namespace StringsAreEvil
{
    public interface ILineParser
    {
        void ParseLine(string line);
        void ParseLine(char[] line);
        void ParseLine(StringBuilder line);
        void ParseLine(ReadOnlySpan<byte> line);
        void Dump();
    }
}
