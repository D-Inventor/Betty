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
            if (!symbols.EndOfInput && symbols.Current.Equals(Target))
            {
                // return success and shift the position of the input symbols
                yield return new Result<InSymbolType>(true, symbols[symbols.Index++], symbols);
            }

            // return failure
            yield return new Result<InSymbolType>(false, null, symbols);
        }
    }
}
