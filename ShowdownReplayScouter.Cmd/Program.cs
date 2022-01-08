using CommandLine;
using ShowdownReplayScouter.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShowdownReplayScouter.Cmd
{
    public static class Program
    {
        public class Options
        {
            [Option('u', "users", Required = false, Default = null, HelpText = "The users you want to interpret the replays from.")]
            public IEnumerable<string> Users { get; set; }
            [Option('t', "tiers", Required = false, Default = null, HelpText = "The tiers you want to look at.")]
            public IEnumerable<string> Tiers { get; set; }
            [Option('o', "opponents", Required = false, Default = null, HelpText = "The opponents your users should have fought.")]
            public IEnumerable<string> Opponents { get; set; }
            [Option('l', "links", Required = false, Default = null, HelpText = "The links you want to analyze.")]
            public IEnumerable<Uri> Links { get; set; }
            [Option('f', "file", Required = true, HelpText = "The file you want to write to.")]
            public string File { get; set; }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (o.Links?.Any() == false && o.Users?.Any() == false)
                    {
                        Console.WriteLine("Either a user or replays have to be provided!");
                        return;
                    }

                    var replayScouter = new Core.ReplayScouter.ShowdownReplayScouter();
                    var scoutingRequest = new Core.Data.ScoutingRequest()
                    {
                        Users = o.Users,
                        Tiers = o.Tiers,
                        Links = o.Links,
                        Opponents = o.Opponents
                    };
                    var result = replayScouter.ScoutReplays(scoutingRequest);

                    var output = OutputPrinter.Print(scoutingRequest, result.Teams);

                    var f = new FileInfo(o.File);
                    if (f.Exists)
                    {
                        f.Delete();
                    }
                    File.WriteAllText(o.File, output);
                }
            );
        }
    }
}
