using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UGScraper;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace CLI;

class Cli
{
    private SearchScraper searchScraper { get; }
    private PageScraper pageScraper { get; }
    private Options opts { get; }
    private string query { get; }
    private string[] cmdlineUrls { get; }

    private HashSet<contentType> allowedTypes { get; }
    private ScraperRecord[]? searchResults { get; set; }

    public Cli(Options opts)
    {
        this.searchScraper = new();
        this.pageScraper = new();
        this.opts = opts;

        this.query = string.Join(' ', opts.queryToks).Trim();
        if (this.query.Length == 0)
        {
            Console.Error.WriteLine("Empty query - exiting...");
            Environment.Exit(1);
        }
        this.cmdlineUrls = opts.queryToks.ToArray();

        this.allowedTypes = getAllowedTypes(opts.Types);
    }

    public void Run()
    {
        if (this.opts.UrlScrape)
        {
            FetchAndPrint(this.cmdlineUrls);
        }
        else
        {
            SearchAndPrint();
        }
    }

    private void SearchAndPrint()
    {
        Console.Error.WriteLine("Searching - this may take a while");

        bool isValid(ScraperRecord r) =>
            r.ContentUrl is not null && r.ContentIsPlaintext && this.allowedTypes.Contains(r.Type);

        SearchUG();
        this.searchResults = this.searchResults!.Where(i => isValid(i)).ToArray();

        if (this.searchResults.Length == 0)
        {
            Console.Error.WriteLine($"Nothing was found for query '{this.query}'");
            Environment.Exit(1);
        }

        var searchLookup = this.searchResults.ToDictionary(i => i.ScrapeUid, i => i);
        uint[] choiceUids = GetFzfUserInput();
        string[] pageUrls = choiceUids.Select(uid => searchLookup[uid].ContentUrl!).ToArray();

        FetchAndPrint(pageUrls);
    }

    private void FetchAndPrint(string[] urls)
    {
        for (int i = 0; i < urls.Length; ++i)
        {
            ScraperRecord pageRecord;
            try
            {
                this.pageScraper.LoadData(urls[i]);
                pageRecord = this.pageScraper.GetRecord();
            }
            catch (ScraperException e)
            {
                Console.Error.WriteLine("\n### ERROR ###");
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(  "### ERROR ###");
                continue;
            }

            // blank line for separation
            if (i != 0)
                Console.WriteLine();

            Console.WriteLine(
                $"""
                =======================================================
                Song: {pageRecord.SongName}
                Artist: {pageRecord.ArtistName}
                URL: {pageRecord.ContentUrl}
                =======================================================

                {PageContentPrettify(pageRecord)}
                """);
        }
    }

    private HashSet<contentType> getAllowedTypes(string typestr)
    {
        HashSet<contentType> output = new();
        foreach (char c in typestr)
        {
            switch (c)
            {
                case 'c':
                    output.Add(contentType.chords);
                    break;
                case 't':
                    output.Add(contentType.tab);
                    break;
                case 'u':
                    output.Add(contentType.ukulele);
                    break;
                case 'b':
                    output.Add(contentType.bass);
                    break;
                case 'd':
                    output.Add(contentType.drums);
                    break;
                case 'v':
                    output.Add(contentType.video);
                    break;
                default: break;
            }
        }

        if (output.Count == 0)
        {
            output.Add(contentType.chords);
            output.Add(contentType.tab);
        }

        return output;
    }

    private Process MakeFzfProc()
    {
        Process fzfProc = new();
        fzfProc.StartInfo.FileName = "fzf";
        fzfProc.StartInfo.Arguments =
            """-d ";" --with-nth=2.. --nth=1 """ + (this.opts.NoMulti ? "+m" : "-m");
        fzfProc.StartInfo.RedirectStandardInput = true;
        fzfProc.StartInfo.RedirectStandardOutput = true;
        return fzfProc;
    }

    private uint[] GetFzfUserInput()
    {
        Process fzfProc = MakeFzfProc();
        fzfProc.Start();

        // {uid};{song_name} ?{part} by {artist} ({content_type}) ?v{version}
        StringBuilder sb = new();
        foreach (ScraperRecord r in this.searchResults!)
        {
            sb.Clear();

            string?[] tokens = {
                $"{r.ScrapeUid};{r.SongName ?? "Unknown"}",
                r.Part,
                $"by {r.ArtistName ?? "Unknown"}",
                $"({r.Type})",
                (r.Version ?? 1) != 1 ? $"v{r.Version}" : null,
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

        var sr = new StringReader(fzfOut);
        var uids = new List<uint>();
        for (string? line; (line = sr.ReadLine()) is not null;)
            uids.Add(uint.Parse(line.Substring(0, fzfOut.IndexOf(';'))));

        return uids.ToArray();
    }

    private string PageContentPrettify(ScraperRecord r)
    {
        const string youtubeWatchUrl = "https://www.youtube.com/watch?v=";

        if (!r.ContentIsPlaintext)
            return "### CONTENT NOT PLAINTEXT ###";

        if (r.Content is null || r.Content.Trim().Length == 0)
            return "### NO CONTENT ###";

        switch (r.Type)
        {
            case contentType.video:
                return youtubeWatchUrl + r.Content;
            default:
                var contentMetaTextRgx = new Regex(@"\[/?(ch|tab)\]");
                return contentMetaTextRgx.Replace(r.Content, "");
        }
    }

    private void SearchUG()
    {
        try
        {
            this.searchScraper.LoadData(this.query);
            this.searchResults = this.searchScraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(1);
        }
    }
}
