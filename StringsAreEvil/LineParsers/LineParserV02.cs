namespace StringsAreEvil
{
    /// <summary>
    /// Stats:-
    ///     Took: 6,969 ms
    ///     Allocated: 4,288,215 kb
    ///     Peak Working Set: 16,640 kb
    ///
    /// Change:-
    ///     Use the orginal parts array
    /// </summary>
    public sealed class LineParserV02 : LineParser<ValueHolder>
    {
        public override void ParseLine(string line)
        {
            var parts = line.Split(',');
            if (parts[0] == "MNO")
            {
                var valueHolder = new ValueHolder(parts);

                AddItem(valueHolder);
            }
        }
    }
}
