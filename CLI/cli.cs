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

    static SearchScraperRecord[]? SearchUG(string query)
    {
        SearchScraperRecord[]? results = null;
        try
        {
            SearchScraper searchScraper = new();
            searchScraper.LoadData(query);
            results = searchScraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(1);
        }
        return results;
    }

    static string GetFzfUserInput(SearchScraperRecord[] searchResults)
    {
        Process fzfProc = MakeFzfProc();
        fzfProc.Start();

        string fzfOut;
        try
        {
            foreach (SearchScraperRecord record in searchResults)
                fzfProc.StandardInput.WriteLine($"{record.ScrapeUid};{record.SongName} by {record.ArtistName} ({record.Type})");
            fzfProc.StandardInput.Close();
            fzfOut = fzfProc.StandardOutput.ReadToEnd();
        }
        finally
        {
            fzfProc.StandardInput.Close();
            fzfProc.StandardOutput.Close();
        }

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
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Expected exactly 1 argument");
            Environment.Exit(1);
        }

        string query = args[0];
        SearchScraperRecord[]? searchResults = SearchUG(query);

        if (searchResults is null)
        {
            Console.Error.WriteLine($"Nothing was found for query '{query}'");
            Environment.Exit(1);
        }

        string fzfOut = GetFzfUserInput(searchResults);
        var searchLookup = searchResults!.ToDictionary(i => i.ScrapeUid, i => i);

        uint choiceUid = uint.Parse(fzfOut.Substring(0, fzfOut.IndexOf(';')));
        SearchScraperRecord r = searchLookup[choiceUid];

        PageScraper contentScraper = new();
        contentScraper.LoadData(r.TabUrl);
        Console.WriteLine(contentScraper.GetChords());
    }
}
