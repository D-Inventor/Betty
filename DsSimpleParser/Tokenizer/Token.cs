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
            this.expression = new Regex(expression, RegexOptions.Multiline | RegexOptions.Compiled);
        }

        public TokenMatch? Match(string input, int index = 0)
        {
            // match this token to the string
            Match m = expression.Match(input, index);

            // the match has to be at the given index
            if (!m.Success || m.Index != index) return null;

            // return the match
            return new TokenMatch(name, m.Value);
        }
    }

    public struct TokenMatch
    {
        public TokenMatch(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
    }
}
