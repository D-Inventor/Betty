using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Discord;

namespace Betty
{
	public class StringConverter
	{
		// regular expressions
		static Regex expression = new Regex("^(?<keyword>[a-zA-Z]+(\\.[a-zA-Z]+)*)\\s*=\\s*(?<options>\".*?(?<!\\\\)\"(\\s*\\|\\s*\".*?(?<!\\\\)\")*)", RegexOptions.Compiled | RegexOptions.Multiline);
		static Regex option = new Regex("\"(?<option>.*?)(?<!\\\\)\"", RegexOptions.Compiled | RegexOptions.Multiline);
		static Regex replacable = new Regex(@"(?<!\\)(\{(?<nonterminal>[a-zA-Z]+(\.[a-zA-Z]+)*)\}|\[(?<variable>[a-zA-Z]+(\.[a-zA-Z]+)*)\])", RegexOptions.Compiled);
		
		Random random;
		Dictionary<string, Sentence[]> translations;

		private StringConverter(string name, Dictionary<string, Sentence[]> translations)
		{
			this.translations = translations;
			this.Name = name;
			this.random = new Random();
		}

		// load a language from a file
		public static StringConverter LoadFromFile(string path, Logger logger)
		{
			Dictionary<string, Sentence[]> translations = new Dictionary<string, Sentence[]>();

			if (File.Exists(path))
			{
				// read the file
				string input;
				using (StreamReader sr = new StreamReader(path))
				{
					input = sr.ReadToEnd();
				}

				// find all expressions
				MatchCollection matches = expression.Matches(input);
				foreach (Match m in matches)
				{
					// store all expressions
					GroupCollection groups = m.Groups;
					string key = groups["keyword"].Value;

					Sentence[] options = option.Matches(groups["options"].Value).Select(v => new Sentence(v.Groups["option"].Value)).ToArray();
					translations.Add(key, options);
				}
			}
			else
			{
				logger.Log(new LogMessage(LogSeverity.Warning, "StringConverter", $"Couldn't find given language file: {path}"));
			}

			return new StringConverter(Path.GetFileNameWithoutExtension(path), translations);
		}

		// produces a string, using the language, starting from the given keyword
		public string GetString(string keyword, SentenceContext context = null)
		{
			if (context == null) context = SentenceContext.Empty;
			if (!translations.ContainsKey(keyword)) return $"{{{keyword}}}";

			Sentence s = translations[keyword][random.Next(translations[keyword].Length)];

			string result = new string(s.value.ToCharArray());
			foreach (Replacable n in s.replacables.Reverse())
			{
				string r;
				switch (n.type)
				{
					case ReplaceType.Nonterminal:
						r = GetString(n.keyword, context);
						break;
					case ReplaceType.Variable:
						r = context.Get(n.keyword);
						break;
					default:
						throw new Exception("This replace type is unknown!!");
				}
				result = result.Remove(n.start, n.length).Insert(n.start, r);
			}

			return result;
		}

		public string Name { get; }

		private struct Sentence
		{
			public string value;
			public Replacable[] replacables;

			public Sentence(string value)
			{
				// keep the sentence like this for now
				this.value = value;
				MatchCollection matches = replacable.Matches(value);
				replacables = matches.Select(m =>
				{
					string kw;
					ReplaceType t;
					if (m.Groups["variable"].Success)
					{
						kw = m.Groups["variable"].Value;
						t = ReplaceType.Variable;
					}
					else
					{
						kw = m.Groups["nonterminal"].Value;
						t = ReplaceType.Nonterminal;
					}
					return new Replacable
					{
						start = m.Index,
						length = m.Length,
						keyword = kw,
						type = t
					};
				}).ToArray();
				Array.Sort(replacables);
				FilterEscapes();
			}

			private void FilterEscapes()
			{
				int n = 0;
				for (int i = 0; i < value.Length; i++)
				{
					if (n < replacables.Length && replacables[n].start <= i)
						n++;

					if (value[i] == '\\')
					{
						value = value.Remove(i, 1);
						for (int j = n; j < replacables.Length; j++)
							replacables[j].start--;
					}
				}
			}
		}

		private struct Replacable : IComparable<Replacable>
		{
			public int start, length;
			public string keyword;
			public ReplaceType type;

			public int CompareTo(Replacable other)
			{
				return this.start - other.start;
			}
		}

		private enum ReplaceType { Variable, Nonterminal }
	}

	public class SentenceContext
	{
		private Dictionary<string, string> collection;

		public SentenceContext()
		{
			collection = new Dictionary<string, string>();
		}

		public string Get(string name)
		{
			return collection.ContainsKey(name) ? collection[name] : "";
		}

		public SentenceContext Add(string name, string value)
		{
			collection[name] = value;
			return this;
		}
		public static SentenceContext Empty { get; } = new SentenceContext();
	}
}
