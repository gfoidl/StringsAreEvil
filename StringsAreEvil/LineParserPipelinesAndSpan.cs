using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsAreEvil
{
    class LineParserPipelinesAndSpan : ILineParser
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

            if (line[0] == 'M' && line[1] == 'N' && line[2] == 'O')
            {
                // SKIP MNO
                var commaAt = line.IndexOf(comma);
                line = line.Slice(commaAt + 1);

                // Parse the line
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
                var valueHolder = new ValueHolderAsStruct(elementId, vehicleId, term, mileage, value);
            }
        }
    }
}
