﻿using System.Threading.Tasks;

namespace StringsAreEvil
{
    public abstract class Variant
    {
        protected ILineParser _lineParser;

        protected Variant(ILineParser lineParser) => _lineParser = lineParser;

        public ILineParser LineParser => _lineParser;

        public abstract Task ParseAsync(string fileName);
    }
}
