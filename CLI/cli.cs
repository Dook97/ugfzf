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

    private HashSet<contentType> allowedTypes { get; }
    private ScraperRecord[]? searchResults { get; set; }
    private ScraperRecord[]? pagesContent { get; set; }

    public Cli(Options opts)
    {
        this.searchScraper = new();
        this.pageScraper = new();
        this.opts = opts;

        this.query = opts.queryToks is null ? "" : string.Join(' ', opts.queryToks).Trim();
        if (this.query.Length == 0)
        {
            Console.Error.WriteLine("Empty query - exiting...");
            Environment.Exit(1);
        }

        this.allowedTypes = getAllowedTypes(opts.Types!);
    }

    public void Run()
    {
        Console.Error.WriteLine("Searching - this may take a while");

        bool isValid(ScraperRecord r) =>
            r.ContentUrl is not null && r.ContentIsPlaintext && allowedTypes.Contains(r.Type);

        SearchUG(query);
        searchResults = searchResults!.Where(i => isValid(i)).ToArray();

        if (searchResults.Length == 0)
        {
            Console.Error.WriteLine($"Nothing was found for query '{query}'");
            Environment.Exit(1);
        }

        var searchLookup = searchResults.ToDictionary(i => i.ScrapeUid, i => i);
        uint[] choiceUids = GetFzfUserInput();
        ScraperRecord[] pageRecords = choiceUids.Select(uid => searchLookup[uid]).ToArray();

        FetchAndPrint(pageRecords);
    }

    private void FetchAndPrint(ScraperRecord[] searchRecords)
    {
        for (int i = 0; i < searchRecords.Length; ++i)
        {
            pageScraper.LoadData(searchRecords[i].ContentUrl!);
            var pageRecord = pageScraper.GetRecord();

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
        fzfProc.StartInfo.Arguments = """-d ";" --with-nth=2.. --nth=1 """ + (opts.Multi ? "-m" : "+m");
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
        foreach (ScraperRecord r in searchResults!)
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

        if (r.Content is null || r.Content.Trim().Length == 0)
            return "### NO CONTENT ###";

        if (!r.ContentIsPlaintext)
            return "### CONTENT NOT PLAINTEXT ###";

        switch (r.Type)
        {
            case contentType.video:
                return youtubeWatchUrl + r.Content;
            default:
                var contentMetaTextRgx = new Regex(@"\[/?(ch|tab)\]");
                return contentMetaTextRgx.Replace(r.Content, "");
        }
    }

    private void SearchUG(string query)
    {
        try
        {
            searchScraper.LoadData(query);
            this.searchResults = searchScraper.GetSearchResults();
        }
        catch (ScraperException e)
        {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Console.Error.WriteLine(e.StackTrace);
            Environment.Exit(1);
        }
    }
}
