using System;
using System.Collections.Generic;
using System.Text;

namespace DsSimpleParser
{
    public class Repeat<InSymbolType> : Parser<InSymbolType>
    {
        public Parser<InSymbolType> Parser { get; }
        public int Min { get; }
        public int Max { get; }

        private int iterations;

        public Repeat(Parser<InSymbolType> parser, int min = -1, int max = -1)
        {
            Parser = parser;
            Min = min;
            Max = max;
        }

        public override IEnumerable<Result<InSymbolType>> Parse(InputSymbols<InSymbolType> symbols)
        {
            throw new NotImplementedException();
        }
    }
}
