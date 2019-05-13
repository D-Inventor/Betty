using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DsSimpleParser.NUnitTest
{
    public class RepeatTest
    {
        [Test]
        public void Parse_TwoMatches_ReturnsAllPossibleMatches()
        {
            // arrange
            Repeat<char> parser = new Repeat<char>(new Symbol<char>('a'));
            InputSymbols<char> input = new InputSymbols<char>("ab".ToCharArray());

            // act
            IEnumerator<Result<char>> result = parser.Parse(input).GetEnumerator();

            // assert
            Assert.IsTrue(result.MoveNext());
            CollectionAssert.AreEqual(new char[0], (IList<object>)result.Current.Value);
            Assert.AreEqual(0, result.Current.Rest.Index);
            Assert.IsTrue(result.MoveNext());
            CollectionAssert.AreEqual(new char[] { 'a' }, (IList<object>)result.Current.Value);
            Assert.AreEqual(1, result.Current.Rest.Index);
            Assert.IsTrue(result.MoveNext());
            Assert.IsFalse(result.Current);
            Assert.AreEqual(0, result.Current.Rest.Index);
        }

        [Test]
        public void Parse_HasMinimumValue_ReturnsMatchesAboveMinimum()
        {
            // arrange
            Repeat<char> parser = new Repeat<char>(new Symbol<char>('a'), 5);
            InputSymbols<char> input = new InputSymbols<char>("aaaaa".ToCharArray());

            // act
            IEnumerable<Result<char>> result = parser.Parse(input);

            // assert
            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new int[] { 5 }, result.Where(x => x.Success).Select(x => x.ValueAs<object[]>().Length).ToArray());
        }

        [Test]
        public void Parse_HasMaximumValue_ReturnsMatchesBelowMaximum()
        {
            // arrange
            Repeat<char> parser = new Repeat<char>(new Symbol<char>('a'), 3, 3);
            InputSymbols<char> input = new InputSymbols<char>("aaaaa".ToCharArray());

            // act
            IEnumerable<Result<char>> result = parser.Parse(input);

            // assert
            Assert.AreEqual(2, result.Count());
            CollectionAssert.AreEqual(new int[] { 3 }, result.Where(x => x.Success).Select(x => x.ValueAs<object[]>().Length).ToArray());
        }
    }
}
