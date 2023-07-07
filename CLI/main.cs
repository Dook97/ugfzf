using System.Collections.Generic;
using CommandLine;

namespace CLI;

class Options
{
    [Option('t', "types", Required = false, Default = "ct",
            HelpText = "Allowed content types ([c]hords, [t]abs, [u]kulele, [b]ass, [d]rums, [v]ideo)")]
    public string? Types { get; init; }

    [Value(0, MetaName = "Search query", Required = true)]
    public IEnumerable<string>? queryToks { get; init; }
}

class Program
{
    static void Main(string[] args)
    {
        Cli cli = new(args);
        cli.Run();
    }
}
