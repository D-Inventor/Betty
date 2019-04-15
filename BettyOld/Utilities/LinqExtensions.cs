using System;
using System.Collections.Generic;
using System.Linq;

namespace Betty
{
    public static class LinqExtensions
	{
		public static T Max<T>(this IEnumerable<T> inputs, Func<T, IComparable> comparer)
		{
			if (inputs.Count() == 0) throw new ArgumentException("Can't get the maximum of an empty sequence");

			T result = inputs.First();
			IComparable resultvalue = comparer(result);

			foreach(T x in inputs.Skip(1))
			{
				IComparable nextresult = comparer(x);
				if(resultvalue.CompareTo(nextresult) < 0)
				{
					result = x;
					resultvalue = nextresult;
				}
			}

			return result;
		}

		public static T Min<T>(this IEnumerable<T> inputs, Func<T, IComparable> comparer)
		{
			if (inputs.Count() == 0) throw new ArgumentException("Can't get the minimum of an empty sequence");

			T result = inputs.First();
			IComparable resultvalue = comparer(result);

			foreach (T x in inputs.Skip(1))
			{
				IComparable nextresult = comparer(x);
				if (resultvalue.CompareTo(nextresult) > 0)
				{
					result = x;
					resultvalue = nextresult;
				}
			}

			return result;
		}
	}
}
