using System;
using System.Collections.Generic;
using CommandLine;

namespace CLI;

// stop the compiler from complaining about things beyond its comprehension
#nullable disable

/// <summary>
/// Holds command line arguments.
/// </summary>
class Options
{
    [Option('t', "types", Required = false, Default = "ct",
        HelpText = "Set allowed content types for search ([c]hords, [t]abs, [u]kulele, [b]ass, [d]rums, [v]ideo)")]
    public string Types { get; init; }

    [Option('m', "no-multi", Required = false, Default = false,
        HelpText = "Disallow selection of multiple items using <Tab>")]
    public bool NoMulti { get; init; }

    [Option("url", Required = false, Default = false,
        HelpText = "Disable the interactive selection and scrape provided URLs instead.")]
    public bool UrlScrape { get; init; }

    [Value(0, MetaName = "query", Required = true)]
    public IEnumerable<string> queryToks { get; init; }
}

#nullable enable

class Program
{
    static void Main(string[] args)
    {
        var opts = Parser.Default.ParseArguments<Options>(args).Value;

        // the parser library prints its own error message, we just have to quit
        if (opts is null)
            Environment.Exit(1);

        Cli cli = new(opts);
        cli.Run();
    }
}
