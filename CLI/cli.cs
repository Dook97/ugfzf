using System;
using System.Diagnostics;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        SearchScraper scraper = new();
        Process fzfProc = new();
        SearchScraperRecord[]? results = null;

        try
        {
            scraper.LoadData(args[0]);
            results = scraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Environment.Exit(1);
        }

        fzfProc.StartInfo.FileName = "fzf";
        fzfProc.StartInfo.Arguments = "-d \";\" --with-nth=2 --nth=1 --border --border-label \" select a song \"";
        fzfProc.StartInfo.RedirectStandardInput = true;
        fzfProc.StartInfo.RedirectStandardOutput = true;
        fzfProc.Start();

        foreach (SearchScraperRecord result in results)
            fzfProc.StandardInput.WriteLine($"{result.ScrapeUid};{result.SongName} by {result.ArtistName} ({result.Type})");
        fzfProc.StandardInput.Close();

        string output = fzfProc.StandardOutput.ReadToEnd();

        fzfProc.WaitForExit();

        switch (fzfProc.ExitCode)
        {
            case 0: break;
            case 1:
                Console.Error.WriteLine("No match - exiting");
                Environment.Exit(0);
                break;
            case 2:
                Console.Error.WriteLine("fzf error");
                Environment.Exit(1);
                break;
            case 130:
                Console.Error.WriteLine("User interrupt - exiting");
                Environment.Exit(0);
                break;
        }

        Console.Write(output);
    }
}
