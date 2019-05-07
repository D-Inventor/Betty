using System;
using System.Threading.Tasks;

namespace DsSimpleParser
{
    /// <summary>
    /// A parser is a class that can convert a sequence of symbols to an object
    /// </summary>
    public class Parser<InSymbolType>
    {
        public virtual Result<InSymbolType> Parse(InputSymbols<InSymbolType> symbols)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public virtual async Task<Result<InSymbolType>> ParseAsync(InputSymbols<InSymbolType> symbols)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return Parse(symbols);
        }
    }
}
