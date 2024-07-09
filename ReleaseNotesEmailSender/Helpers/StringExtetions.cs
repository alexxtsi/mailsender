using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReleaseNotesEmailSender.Helpers
{
    public static class StringExtetions
    {
        public static string GetDirName(this string path)
        {
            return new DirectoryInfo(path).Name;
        }

        public static string GetTagFromTpName(this string tpName)
        {
            string pattern = @"(?<=^.{10}).{5}";  //Regex finding the tag from tp name 
            string tag = string.Empty;
            Match match = Regex.Match(tpName, pattern);
            if (match.Success)
            {
                tag = match.Value;
                Console.WriteLine(tag);
            }
            else
            {
                Console.WriteLine("Failed to get Tag from TP name");
            }
            return tag;
        }
    }
}
