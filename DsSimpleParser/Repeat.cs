using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DsSimpleParser
{
    public class Repeat<InSymbolType> : Parser<InSymbolType>
    {
        public Parser<InSymbolType> Parser { get; }
        public int Min { get; }
        public int Max { get; }

        public Repeat(Parser<InSymbolType> parser, int min = 0, int max = -1)
        {
            if (min < 0) { throw new ArgumentOutOfRangeException(nameof(min), "The minimum repetition of this parser can not be negative."); }
            if (max > 0 && max < min) { throw new ArgumentOutOfRangeException(nameof(max), "The maximum repetition of this parser has to be higher than the minimum or negative for no limit."); }

            Parser = parser;
            Min = min;
            Max = max;
        }

        public override IEnumerable<Result<InSymbolType>> Parse(InputSymbols<InSymbolType> symbols)
        {
            Stack<IEnumerator<Result<InSymbolType>>> parses = new Stack<IEnumerator<Result<InSymbolType>>>();
            Result<InSymbolType> result = new Result<InSymbolType>(true, new object[0], symbols);
            IEnumerator<Result<InSymbolType>> current = Parser.Parse(symbols).GetEnumerator();
            while (true)
            {
                // if the current result is valid, return it.
                if(parses.Count >= Min) { yield return result; }

                if(current.MoveNext() && current.Current)
                {
                    // if the current enumerator returns a valid new result, put it on the stack and continue
                    parses.Push(current);
                    result = new Result<InSymbolType>(true, parses.Select(n => n.Current.Value).Reverse().ToArray(), current.Current.Rest);
                    current = (Max < 0 || parses.Count < Max) ? Parser.Parse(current.Current.Rest).GetEnumerator() : parses.Peek();
                }
                else
                {
                    // otherwise pop iterators from the stack until one gives a new valid result.
                    do
                    {
                        if(parses.Count == 1)
                        {
                            // stop searching if the stack has become empty
                            yield return new Result<InSymbolType>(false, null, symbols);
                            yield break;
                        }
                        parses.Pop();
                        current = parses.Peek();
                    } while (!current.MoveNext() || !current.Current);
                }
            }
        }
    }
}
