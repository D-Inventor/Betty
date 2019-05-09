using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DsSimpleParser.NUnitTest
{
    public class TokenizerTest
    {
        [Test]
        public void Convert_OneTokenExample_ReturnsCorrectList()
        {
            // arrange
            Tokenizer tokenizer = new Tokenizer().Add(new Token("lettera", "a"));
            string input = "aa";

            // act
            TokenMatch[] result = tokenizer.Convert(input);

            // assert
            TokenMatch[] expected = new TokenMatch[] { new TokenMatch("lettera", "a", 1), new TokenMatch("lettera", "a", 2) };
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Convert_TwoTokenExampleParsableInput_ReturnsCorrectList()
        {
            //arrange
            Tokenizer tokenizer = new Tokenizer()
                .Add(new Token("a", "a"))
                .Add(new Token("b", "b"));
            string input = "ba";

            // act
            TokenMatch[] result = tokenizer.Convert(input);

            // assert
            TokenMatch[] expected = new TokenMatch[] { new TokenMatch("b", "b", 1), new TokenMatch("a", "a", 2) };
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Convert_InputRequiresBackTracking_ReturnsCorrectList()
        {
            // arrange
            Tokenizer tokenizer = new Tokenizer()
                .Add(new Token("a", "a"))
                .Add(new Token("c", "c"))
                .Add(new Token("casbs", "ca+b+"));
            string input = "acab";

            // act
            TokenMatch[] result = tokenizer.Convert(input);

            // assert
            TokenMatch[] expected = new TokenMatch[] { new TokenMatch("a", "a", 1), new TokenMatch("casbs", "cab", 4) };
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Convert_UnparsableInput_ReturnsNull()
        {
            // arrange
            Tokenizer tokenizer = new Tokenizer()
                .Add(new Token("a", "a"))
                .Add(new Token("c", "c"));

            string input = "aaab";

            // act
            TokenMatch[] result = tokenizer.Convert(input);

            // assert
            Assert.IsNull(result);
        }
    }
}
