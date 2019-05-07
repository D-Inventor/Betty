using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DsSimpleParser.NUnitTest
{
    public class TokenTest
    {
        [Test]
        public void Match_ValidTokenAtIndex_ReturnsMatch()
        {
            // arrange
            Token token = new Token("lettera", "a");
            string input = "matching string";

            // act
            TokenMatch? result = token.Match(input, 1);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("a", result.Value.Value);
            Assert.AreEqual("lettera", result.Value.Name);
        }

        [Test]
        public void Match_NoValidToken_ReturnsNull()
        {
            // arrange
            Token token = new Token("letterb", "b");
            string input = "matching string";

            // act
            TokenMatch? result = token.Match(input, 1);

            // assert
            Assert.IsNull(result);
        }

        [Test]
        public void Match_ValidTokenButNotAtIndex_ReturnsNull()
        {
            // arrange
            Token token = new Token("lettera", "a");
            string input = "matching string";

            // act
            TokenMatch? result = token.Match(input, 0);

            // assert
            Assert.IsNull(result);
        }

        [Test]
        public void Match_GivenIndexHigherThanLength_ThrowsIndexOutOfRangeException()
        {
            // arrange
            Token token = new Token("lettera", "a");
            string input = "matching string";

            // act
            void result() => token.Match(input, 50);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(result);
        }

        [Test]
        public void Match_GivenIndexLowerThanZero_ThrowsIndexOutOfRangeException()
        {
            // arrange
            Token token = new Token("lettera", "a");
            string input = "matching string";

            // act
            void result() => token.Match(input, -1);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(result);
        }
    }
}
