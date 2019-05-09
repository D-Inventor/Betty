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
            IEnumerator<Result<char>> enumerator = parser.Parse(new InputSymbols<char>("Hello world".ToCharArray())).GetEnumerator();
            enumerator.MoveNext();

            Result<char> result = enumerator.Current;

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
            IEnumerator<Result<char>> enumerator = parser.Parse(new InputSymbols<char>("Bye world".ToCharArray())).GetEnumerator();
            enumerator.MoveNext();

            Result<char> result = enumerator.Current;

            // assert
            Assert.IsFalse(result.Success);
        }
    }
}
