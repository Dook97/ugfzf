using System;
using System.Collections.Generic;
using CommandLine;

namespace CLI;

#nullable disable
class Options
{
    [Option('t', "types", Required = false, Default = "ct",
        HelpText = "Allowed content types ([c]hords, [t]abs, [u]kulele, [b]ass, [d]rums, [v]ideo)")]
    public string Types { get; init; }

    [Option('m', "no-multi", Required = false, Default = false,
        HelpText = "Disallow selection of multiple items using <Tab>")]
    public bool NoMulti { get; init; }

    [Option("url", Required = false, Default = false,
        HelpText = "Disable the interactive selection and scrape provided URLs instead.")]
    public bool UrlScrape { get; init; }

    [Value(0, MetaName = "Search query", Required = true)]
    public IEnumerable<string> queryToks { get; init; }
}
#nullable enable

class Program
{
    static void Main(string[] args)
    {
        var opts = Parser.Default.ParseArguments<Options>(args).Value;

        if (opts is null)
            Environment.Exit(1);

        Cli cli = new(opts);
        cli.Run();
    }
}
