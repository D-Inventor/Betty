using System;
using System.Collections.Generic;
using System.Text;

namespace DsSimpleParser
{
    /// <summary>
    /// This class can convert a string into a list of tokens that's usable by the parser
    /// </summary>
    public class Tokenizer
    {
        List<Token> tokens;

        /// <summary>
        /// Convert a given string to a list of tokens
        /// </summary>
        /// <param name="input">the string to be converted</param>
        /// <returns>The result of the conversion</returns>
        TokenMatch[] Convert(string input)
        {
            Stack<(int, TokenMatch)> tokenStack = new Stack<(int, TokenMatch)>();
            int pointer = 0;
            throw new NotImplementedException();
        }
    }
}
