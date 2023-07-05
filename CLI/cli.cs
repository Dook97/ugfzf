using System;
using System.Diagnostics;
using System.Linq;
using UGScraper;

namespace CLI;
class Program
{
    static Process MakeFzfProc()
    {
        Process fzfProc = new();
        fzfProc.StartInfo.FileName = "fzf";
        fzfProc.StartInfo.Arguments = "-d \";\" --with-nth=2 --nth=1 --border --border-label \" select a song \"";
        fzfProc.StartInfo.RedirectStandardInput = true;
        fzfProc.StartInfo.RedirectStandardOutput = true;
        return fzfProc;
    }

    static SearchScraperRecord[] SearchUG(string query)
    {
        SearchScraper searchScraper = new();
        try
        {
            searchScraper.LoadData(query);
            return searchScraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(1);
            return null; // someone should tell the compiler about the noreturn function attribute
        }
    }

    static string GetFzfUserInput(SearchScraperRecord[] searchResults)
    {
        Process fzfProc = MakeFzfProc();
        fzfProc.Start();

        foreach (SearchScraperRecord record in searchResults)
            fzfProc.StandardInput.WriteLine($"{record.ScrapeUid};{record.SongName} by {record.ArtistName} ({record.Type})");
        fzfProc.StandardInput.Close();

        string fzfOut = fzfProc.StandardOutput.ReadToEnd();
        fzfProc.StandardOutput.Close();

        fzfProc.WaitForExit();

        switch (fzfProc.ExitCode)
        {
            case 0: break;
            case 1:
                Console.Error.WriteLine("No match - exiting");
                Environment.Exit(0);
                break;
            case 2:
                Console.Error.WriteLine("fzf error - exiting");
                Environment.Exit(1);
                break;
            case 130:
                Console.Error.WriteLine("User interrupt - exiting");
                Environment.Exit(0);
                break;
        }

        return fzfOut;
    }

    static void Main(string[] args)
    {
        string query = string.Join(' ', args);

        bool isValid(SearchScraperRecord r) => r.ContentUrl is not null
                                               && r.Type != contentType.official
                                               && r.Type != contentType.proTab;

        SearchScraperRecord[] searchResults = SearchUG(query).Where(i => isValid(i)).ToArray();

        if (searchResults.Length == 0)
        {
            Console.Error.WriteLine($"Nothing was found for query '{query}'");
            Environment.Exit(1);
        }

        string fzfOut = GetFzfUserInput(searchResults);
        var searchLookup = searchResults.ToDictionary(i => i.ScrapeUid, i => i);

        // I'm aware this could theoretically fail, but it really shouldn't so if it does,
        // the process should terminate and write out a stack trace, which is what it does by default
        uint choiceUid = uint.Parse(fzfOut.Substring(0, fzfOut.IndexOf(';')));
        SearchScraperRecord r = searchLookup[choiceUid];

        PageScraper contentScraper = new();
        // silence nullability warning because we filtered out items without ContentUrl
        contentScraper.LoadData(r.ContentUrl!);
        Console.WriteLine(contentScraper.GetContent());
    }
}
