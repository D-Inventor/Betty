using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Betty.Services
{
    public class Language
    {
        private Dictionary<string, IList<string>> translations;

        public string Name { get; set; }

        private Language(Dictionary<string, IList<string>> translations)
        {
            this.translations = translations;
        }

        public string GetTranslation(string keyword)
        {
            throw new NotImplementedException();
        }

        public static Language FromFile(string path)
        {
            using(var sr = new StreamReader(path))
            {
                Language l = FromFile(sr);
                l.Name = Path.GetFileNameWithoutExtension(path);
                return l;
            }
        }

        public static Language FromFile(TextReader stream)
        {
            var sentences = GetSentencesFromStream(stream);
            var translations = new Dictionary<string, IList<string>>(sentences.Select(s => GetTranslationFromSentence(s)));
            return new Language(translations);
        }

        public static string[] GetSentencesFromStream(TextReader input)
        {
            string line, key = null;
            while((line = input.ReadLine()) != null) //  read all the lines in the input
            {
                line = line.TrimStart();
            }

            throw new NotImplementedException();
        }

        public static KeyValuePair<string, IList<string>> GetTranslationFromSentence(string sentence)
        {
            throw new NotImplementedException();
        }
    }
}
