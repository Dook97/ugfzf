using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        SearchScraperRecord[] results;
        try
        {
            searchScraper.LoadData(query);
            results = searchScraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(1);
            return null; // someone should tell the compiler about the noreturn function attribute
        }

        return results;
    }

    static string GetFzfUserInput(SearchScraperRecord[] searchResults)
    {
        Process fzfProc = MakeFzfProc();
        fzfProc.Start();

        // {uid};{song_name} by {artist} ({content_type}) ?v{version}
        StringBuilder sb = new();
        foreach (SearchScraperRecord r in searchResults)
        {
            sb.Clear();
            string?[] tokens = {
                $"{r.ScrapeUid};{r.SongName}",
                r.Part,
                $"by {r.ArtistName}",
                $"({r.Type})",
                (r.Version ?? 1) != 1 ? $"v{r.Version}" : null
            };
            sb.AppendJoin(' ', tokens.Where(i => i is not null && i.Length != 0));

            fzfProc.StandardInput.WriteLine(sb.ToString());
        }
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

        // TODO: maybe start a separate thread which shows some animation?
        Console.Error.WriteLine("Searching - this may take a while");

        bool isValid(SearchScraperRecord r) => r.ContentUrl is not null && r.ContentIsPlaintext;
        SearchScraperRecord[] searchResults = SearchUG(query).Where(i => isValid(i)).ToArray();

        if (searchResults.Length == 0)
        {
            Console.Error.WriteLine($"Nothing was found for query '{query}'");
            Environment.Exit(1);
        }

        string fzfOut = GetFzfUserInput(searchResults);
        var searchLookup = searchResults.ToDictionary(i => i.ScrapeUid, i => i);

        // I'm aware this could theoretically fail, but it really shouldn't so if it does,
        // the process should terminate and write out a stack trace anyway
        uint choiceUid = uint.Parse(fzfOut.Substring(0, fzfOut.IndexOf(';')));
        SearchScraperRecord searchRecord = searchLookup[choiceUid];

        PageScraper pageScraper = new();
        pageScraper.LoadData(searchRecord.ContentUrl!);
        PageScraperRecord pageRecord = pageScraper.GetRecord();

        Console.WriteLine(
                $"""
                Song: {pageRecord.SongName}
                Artist: {pageRecord.ArtistName}
                URL: {pageRecord.ContentUrl}
                ==================================================

                {pageRecord.Content}
                """);
    }
}
