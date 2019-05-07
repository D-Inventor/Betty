using System;
using System.Collections.Generic;
using System.Text;

namespace DsSimpleParser
{
    public class Symbol<InSymbolType> : Parser<InSymbolType> where InSymbolType : IEquatable<InSymbolType>
    {
        public InSymbolType Target { get; }

        public Symbol(InSymbolType target)
        {
            Target = target;
        }

        public override Result<InSymbolType> Parse(InputSymbols<InSymbolType> symbols)
        {
            if (!symbols.Current.Equals(Target))
                return new Result<InSymbolType>(false, null, symbols);

            return new Result<InSymbolType>(true, symbols.Symbols[symbols.Index++], symbols);
        }
    }
}
