using System;
using System.Collections.Generic;
using System.Text;

namespace DsSimpleParser
{
    /// <summary>
    /// The output of a parser is a result. This object indicates if parsing was successful and stores what the parser has produced.
    /// </summary>
    public struct Result<InSymbolType>
    {
        public Result(bool success, object value, InputSymbols<InSymbolType> rest)
        {
            Success = success;
            Value = value;
            Rest = rest;
        }

        public object Value { get; }
        public InputSymbols<InSymbolType> Rest { get; }
        public bool Success { get; }
        public T ValueAs<T>() { return (T)Value; }
    }
}
