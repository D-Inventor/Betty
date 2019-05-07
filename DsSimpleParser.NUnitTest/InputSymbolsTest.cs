using DsSimpleParser;
using NUnit.Framework;

namespace DsSimpleParser.NUnitTest
{
    public class InputSymbolsTest
    {
        [Test]
        public void Current_BaseCase_PointsToTheCorrectCharacter()
        {
            // arrange
            InputSymbols<char> inputSymbols = new InputSymbols<char>("Hello world".ToCharArray());

            // act
            char result = inputSymbols.Current;

            // assert
            Assert.AreEqual('H', result);
        }

        [Test]
        public void OnCopyAndIncrement_OriginalKeepsSameIndex()
        {
            // arrange
            InputSymbols<char> inputSymbols = new InputSymbols<char>("Hello world".ToCharArray(), 3);
            InputSymbols<char> copy = inputSymbols;

            // act
            copy.Index++;

            // assert
            Assert.AreEqual(3, inputSymbols.Index);
            Assert.AreEqual(4, copy.Index);
        }
    }
}