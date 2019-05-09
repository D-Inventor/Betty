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

        public override IEnumerable<Result<InSymbolType>> Parse(InputSymbols<InSymbolType> symbols)
        {
            if (!symbols.Current.Equals(Target))
            {
                yield return new Result<InSymbolType>(false, null, symbols);
                yield break;
            }

            yield return new Result<InSymbolType>(true, symbols.Symbols[symbols.Index++], symbols);
        }
    }
}
