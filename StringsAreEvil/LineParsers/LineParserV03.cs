namespace StringsAreEvil
{
    /// <summary>
    /// Stats:-
    ///     Took: 8,313 ms
    ///     Allocated: 4,284,799 kb
    ///     Peak Working Set: 16,532 kb
    ///
    /// Change:-
    ///     We only want to process the lines we care about. Therefore we
    ///     do not need to do any string processing unless the line begins
    ///     with MNO
    /// </summary>
    public sealed class LineParserV03 : LineParser<ValueHolder>
    {
        public override void ParseLine(string line)
        {
            if (line.StartsWith("MNO"))
            {
                var valueHolder = new ValueHolder(line);

                AddItem(valueHolder);
            }
        }
    }
}
