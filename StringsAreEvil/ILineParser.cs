using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StringsAreEvil
{
    public interface ILineParser
    {
        int Count { get; }

        void ParseLine(string line);
        void ParseLine(char[] line);
        void ParseLine(StringBuilder line);
        void ParseLine(ReadOnlySpan<byte> line);

#if DEBUG
        void Dump();
#endif
    }

    public abstract class LineParser<T> : ILineParser
    {
        protected List<T> _parsedValues = new List<T>();

        public int Count { get; protected set; }

        public virtual void ParseLine(string line) { }
        public virtual void ParseLine(char[] line) { }
        public virtual void ParseLine(StringBuilder line) { }
        public virtual void ParseLine(ReadOnlySpan<byte> line) { }

#if DEBUG
        public virtual void Dump()
        {
            string fileName = $"{this.GetType().Name}.txt";
            File.WriteAllLines(fileName, _parsedValues.Select(v => v.ToString()));
        }
#endif

        protected void AddItem(T item)
        {
            this.Count++;

#if DEBUG
            _parsedValues.Add(item);
#endif
        }
    }
}
