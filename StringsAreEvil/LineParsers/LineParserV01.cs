namespace StringsAreEvil
{
    /// <summary>
    /// Original implementation.
    /// 
    /// Stats:-
    ///     Took: 8,797 ms
    ///     Allocated: 7,412,234 kb
    ///     Peak Working Set: 16,524 kb
    /// </summary>
    public sealed class LineParserV01 : LineParser<ValueHolder>
    {
        public override void ParseLine(string line)
        {
            var parts = line.Split(',');
            if (parts[0] == "MNO")
            {
                var valueHolder = new ValueHolder(line);

                AddItem(valueHolder);
            }
        }
    }
}
