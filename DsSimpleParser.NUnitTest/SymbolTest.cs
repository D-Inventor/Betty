using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DsSimpleParser.NUnitTest
{
    public class SymbolTest
    {
        [Test]
        public void Parse_SymbolsMatch_ReturnsCorrectResult()
        {
            // arrange
            Symbol<char> parser = new Symbol<char>('H');

            // act
            Result<char> result = parser.Parse(new InputSymbols<char>("Hello world".ToCharArray()));

            // assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual('H', result.ValueAs<char>());
            Assert.AreEqual(1, result.Rest.Index);
        }

        [Test]
        public void Parse_SymbolsDontMatch_ReturnsCorrectResult()
        {
            // arrange
            Symbol<char> parser = new Symbol<char>('H');

            // act
            Result<char> result = parser.Parse(new InputSymbols<char>("Bye world".ToCharArray()));

            // assert
            Assert.IsFalse(result.Success);
        }
    }
}
