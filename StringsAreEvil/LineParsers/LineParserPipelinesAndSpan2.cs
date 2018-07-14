using System;
using System.Buffers.Text;
using System.Text;

namespace StringsAreEvil
{
    class LineParserPipelinesAndSpan2 : ILineParser
    {
        public void Dump()
        {
            throw new NotImplementedException();
        }

        public void ParseLine(string line)
        {
            throw new NotImplementedException();
        }

        public void ParseLine(char[] line)
        {
            throw new NotImplementedException();
        }

        public void ParseLine(StringBuilder line)
        {
            throw new NotImplementedException();
        }

        public void ParseLine(ReadOnlySpan<byte> line)
        {
            const byte comma = (byte)',';

            // SKIP MNO
            if (line[0] == 'M' && line[1] == 'N' && line[2] == 'O')
            {
                // Parse the line

                var commaAt = line.IndexOf(comma);

                line = line.Slice(commaAt + 1);
                commaAt = line.IndexOf(comma);
                Utf8Parser.TryParse(line.Slice(0, commaAt), out int elementId, out _);

                line = line.Slice(commaAt + 1);
                commaAt = line.IndexOf(comma);
                Utf8Parser.TryParse(line.Slice(0, commaAt), out int vehicleId, out _);

                line = line.Slice(commaAt + 1);
                commaAt = line.IndexOf(comma);
                Utf8Parser.TryParse(line.Slice(0, commaAt), out int term, out _);

                line = line.Slice(commaAt + 1);
                commaAt = line.IndexOf(comma);
                Utf8Parser.TryParse(line.Slice(0, commaAt), out int mileage, out _);

                line = line.Slice(commaAt + 1);
                commaAt = line.IndexOf(comma);
                Utf8Parser.TryParse(line.Slice(0, commaAt), out decimal value, out _);

                var valueHolder = new ValueHolderAsReadOnlyStruct(elementId, vehicleId, term, mileage, value);
            }
        }
    }
}
