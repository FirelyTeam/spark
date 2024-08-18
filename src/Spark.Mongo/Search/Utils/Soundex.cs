/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Spark.Search.Mongo
{
    public static class Soundex
    {
        public static string For(string word)
        {
            const int length = 4;

            var soundex = new StringBuilder();
            var previousWasHOrW = false;

            word = Regex.Replace(word == null ? string.Empty : word.ToUpper(), @"[^\w\s]", string.Empty);

            if (string.IsNullOrEmpty(word))
                return string.Empty.PadRight(length, '0');

            soundex.Append(word.First());

            for (var i = 1; i < word.Length; i++)
            {
                var n = GetCharNumberForLetter(word[i]);

                if (i == 1 && n == GetCharNumberForLetter(soundex[0]))
                    continue;

                if (soundex.Length > 2 && previousWasHOrW && n == soundex[soundex.Length - 2])
                    continue;

                if (soundex.Length > 0 && n == soundex[soundex.Length - 1])
                    continue;

                soundex.Append(n);

                previousWasHOrW = "HW".Contains(word[i]);
            }

            return soundex
                    .Replace("0", string.Empty).ToString()
                    .PadRight(length, '0')
                    .Substring(0, length);
        }

        private static char GetCharNumberForLetter(char letter)
        {
            if ("BFPV".Contains(letter)) return '1';
            if ("CGJKQSXZ".Contains(letter)) return '2';
            if ("DT".Contains(letter)) return '3';
            if ('L' == letter) return '4';
            if ("MN".Contains(letter)) return '5';
            if ('R' == letter) return '6';

            return '0';
        }
    } 
}