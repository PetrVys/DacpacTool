using ManyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace DacpacTool
{
    public class FilterCommand : ConsoleCommand
    {
        public string DacpacFilePath { get; set; }
        public string BlacklistFilePath { get; set; }
        public string ReplaceFilePath { get; set; }

        public FilterCommand()
        {
            IsCommand("Filter", "Filter and change elements in a DACPAC model");
            HasRequiredOption("f|file=", "DACPAC file to modify", p => DacpacFilePath = p);
            HasOption("b|blacklist=", "Blacklist file - one regex per line. When element matches one of the lines, it will be removed from the model.", p => BlacklistFilePath = p);
            HasOption("r|replace=", "Replace file - one regex and replacement expression per line, separated by \t. Matches will be replaced with the text after \t", p => ReplaceFilePath = p);
        }

        public override int Run(string[] remainingArguments)
        {
            var doBlacklist = !string.IsNullOrEmpty(BlacklistFilePath);
            var doReplace = !string.IsNullOrEmpty(ReplaceFilePath);

            Assert.FileExists(DacpacFilePath);

            Regex[] blacklist = null;
            if (doBlacklist)
            {
                Assert.FileExists(BlacklistFilePath);
                blacklist = File
                    .ReadAllLines(BlacklistFilePath)
                    .Where(s => !s.StartsWith("#"))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => new Regex(s, RegexOptions.Compiled | RegexOptions.Singleline))
                    .ToArray();
            }

            Dictionary<Regex, string> replacements = null;
            if (doReplace)
            {
                Assert.FileExists(ReplaceFilePath);
                replacements = File
                    .ReadAllLines(ReplaceFilePath)
                    .Where(s => !s.StartsWith("#"))
                    .Where(s => s.Contains('\t'))
                    .Select(s =>
                    {
                        var input = s.Split(new char[] { '\t' }, 2);
                        var regex = new Regex(input[0], RegexOptions.Compiled | RegexOptions.Singleline);
                        var result = new Tuple<Regex, string>(regex, input[1]);
                        return result;
                    })
                    .ToDictionary(t => t.Item1, t => t.Item2);
            }

            ProcessDacpac.Process(DacpacFilePath, (element, elementStr, writer) =>
            {
                if (doReplace)
                {
                    foreach (var entry in replacements)
                    {
                        elementStr = entry.Key.Replace(elementStr, entry.Value);
                    }
                }

                if (doBlacklist)
                {
                    if (!blacklist.Any(r => r.IsMatch(elementStr)))
                    {
                        writer.WriteRaw(elementStr);
                    }
                }
                else
                {
                    writer.WriteRaw(elementStr);
                }
            });

            return 0;
        }
    }
}
