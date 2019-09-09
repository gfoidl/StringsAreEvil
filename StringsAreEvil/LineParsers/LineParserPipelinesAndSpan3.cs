using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StringsAreEvil
{
    public sealed class LineParserPipelinesAndSpan3 : LineParser<ValueHolderAsReadOnlyStruct>
    {
        public override void ParseLine(ReadOnlySpan<byte> line)
        {
            const byte comma = (byte)',';

            // SKIP MNO
            if (ShouldProcessLine(line))
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

                AddItem(valueHolder);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldProcessLine(ReadOnlySpan<byte> line)
        {
            if (BitConverter.IsLittleEndian)
            {
#if VAR_A
                Debug.Assert(line.Length > 3);

                ref byte b = ref MemoryMarshal.GetReference(line);

                const short t0 = 'M' | ('N' << 8);
                bool b0 = Unsafe.As<byte, short>(ref b) == t0;
                bool b1 = Unsafe.Add(ref b, 2) == 'O';

                return b1 && b0;
#else
                Debug.Assert(line.Length > 4);

                ref byte b = ref MemoryMarshal.GetReference(line);
                const int t0 = 'M' | ('N' << 8) | ('O' << 16);
                int c0 = Unsafe.As<byte, int>(ref b) & 0x_FF_FF_FF;

                return c0 == t0;
                //return c0 - t0 == 0;
#endif
            }

            // reverse order for bound checks
            return line[2] == 'O' && line[1] == 'N' && line[0] == 'M';
        }
    }
}
