using CommandLine;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace ShowdownReplayScouter.Cmd
{
    public class Program
    {
        public class Options
        {
            [Option('u', "user", Required = false, HelpText = "The user you want to interpret the replays from.")]
            public string User { get; set; }
            [Option('t', "tier", Required = false, HelpText = "The tier you want to look at.")]
            public string Tier { get; set; }
            [Option('o', "opponent", Required = false, HelpText = "The opponent your user should have fought.")]
            public string Opponent { get; set; }
            [Option('l', "links", Required = false, Default = null, HelpText = "The links you want to analyze.")]
            public IEnumerable<Uri> Links { get; set; }
            [Option('f', "file", Required = true, HelpText = "The file you want to write to.")]
            public string File { get; set; }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    var replayScouter = new ShowdownReplayScouter.Core.ReplayScouter.ShowdownReplayScouter();
                    var result = replayScouter.ScoutReplays(new ShowdownReplayScouter.Core.Data.ScoutingRequest()
                    {
                        User = o.User,
                        Tier = o.Tier,
                        Links = o.Links,
                        Opponent = o.Opponent
                    });
                    
                    var output = o.User ?? "";
                    output += o.Tier != null ? $" ({o.Tier})" : "";
                    output += ":\r\n\r\n";

                    foreach(var team in result.Teams)
                    {
                        output += $"{team}:\r\n";
                        output += string.Join("\r\n", team.Links);
                        output += "\r\n\r\n";
                        output += TeamPrinter.Print(team);
                        output += "\r\n\r\n\r\n";
                    }

                    var f = new FileInfo(o.File);
                    if (f.Exists)
                    {
                        f.Delete();
                    }
                    File.WriteAllText(o.File, output);
                });
        }
    }
}
