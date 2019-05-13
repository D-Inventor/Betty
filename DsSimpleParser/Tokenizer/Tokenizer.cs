using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DsSimpleParser
{
    /// <summary>
    /// This class can convert a string into a list of tokens that's usable by the parser
    /// </summary>
    public class Tokenizer
    {
        readonly List<Token> tokens;

        public Tokenizer()
        {
            tokens = new List<Token>();
        }

        /// <summary>
        /// Convert a given string to a list of tokens
        /// </summary>
        /// <param name="input">the string to be converted</param>
        /// <returns>The result of the conversion</returns>
        public TokenMatch[] Convert(string input)
        {
            // keep track of pointer position, token index and token value
            Stack<(int, int, TokenMatch)> tokenStack = new Stack<(int, int, TokenMatch)>();

            int pointer = 0;
            int tokenIndex = 0;

            while (true)
            {
                TokenMatch? result = tokens[tokenIndex].Match(input, pointer);
                if(result != null)
                {
                    // if there was a match, increase the stack and go for the next match
                    tokenStack.Push((pointer, tokenIndex, result.Value));
                    pointer = result.Value.FollowIndex;
                    tokenIndex = 0;

                    // if the end of the input was reached, then the input was succesfully parsed
                    if(pointer == input.Length) { return tokenStack.Select(x => x.Item3).Reverse().ToArray(); }
                }
                else
                {
                    // otherwise backtrack to the first new trial opportunity
                    while (true)
                    {
                        // try next token
                        tokenIndex++;
                        if (tokenIndex < tokens.Count) { break; }
                        else
                        {
                            // try changing previous match
                            if (tokenStack.Count == 0) { return null; }
                            (pointer, tokenIndex, _) = tokenStack.Pop();
                        }
                    }
                }
            }

            // code should never reach this
            throw new NotImplementedException();
        }

        public Tokenizer Add(Token token)
        {
            tokens.Add(token);
            return this;
        }
    }
}
