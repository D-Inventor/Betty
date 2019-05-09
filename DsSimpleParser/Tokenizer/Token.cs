using System;
using System.Text.RegularExpressions;

namespace DsSimpleParser
{
    /// <summary>
    /// A token is an identifier for pieces of an input string. To be used by a tokeniser which is basically a parser that parses a string into a list of tokens
    /// </summary>
    public class Token
    {
        readonly Regex expression;
        readonly string name;

        public Token(string name, string expression)
        {
            this.name = name;
            this.expression = new Regex(@"\G" + expression, RegexOptions.Multiline | RegexOptions.Compiled);
        }

        public TokenMatch? Match(string input, int index = 0)
        {
            // match this token to the string
            Match m = expression.Match(input, index);

            // the match has to be at the given index
            if (!m.Success || m.Index != index) return null;

            // return the match
            return new TokenMatch(name, m.Value, index + m.Length);
        }
    }

    public struct TokenMatch : IEquatable<TokenMatch>
    {
        public TokenMatch(string name, string value, int followIndex)
        {
            Name = name;
            Value = value;
            FollowIndex = followIndex;
        }

        public string Name { get; }
        public string Value { get; }
        public int FollowIndex { get; }

        public bool Equals(TokenMatch other)
        {
            return Name == other.Name && Value == other.Value;
        }

        public override string ToString()
        {
            return $"({Name}, {Value})";
        }
    }
}
