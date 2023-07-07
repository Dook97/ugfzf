using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UGScraper;
using CommandLine;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CLI;

public class Cli
{
    private const string youtubeWatchUrl = "https://www.youtube.com/watch?v=";

    private Regex contentMetaTextRgx { get; }
    private Process fzfProc { get; }
    private SearchScraper searchScraper { get; }
    private PageScraper pageScraper { get; }
    private Options opts { get; }
    private string query { get; }

    private HashSet<contentType> allowedTypes { get; }
    private ScraperRecord[]? searchResults { get; set; }
    private ScraperRecord[]? pagesContent { get; set; }

    public Cli(string[] args)
    {
        this.contentMetaTextRgx = new Regex(@"\[/?(ch|tab)\]");
        this.searchScraper = new();
        this.pageScraper = new();
        this.fzfProc = MakeFzfProc();
        this.opts = Parser.Default.ParseArguments<Options>(args).Value;

        if (opts is null)
            Environment.Exit(1);

        this.query = opts.queryToks is null ? "" : string.Join(' ', this.opts.queryToks).Trim();
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

        searchResults = SearchUG(query).Where(i => isValid(i)).ToArray();

        if (searchResults.Length == 0)
        {
            Console.Error.WriteLine($"Nothing was found for query '{query}'");
            Environment.Exit(1);
        }

        var searchLookup = searchResults.ToDictionary(i => i.ScrapeUid, i => i);
        uint choiceUid = GetFzfUserInput(searchResults);
        ScraperRecord searchRecord = searchLookup[choiceUid];

        PageScraper pageScraper = new();
        pageScraper.LoadData(searchRecord.ContentUrl!); // safe - records with null url were discarded
        ScraperRecord pageRecord = pageScraper.GetRecord();

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
        fzfProc.StartInfo.Arguments = """-d ";" --with-nth=2.. --nth=1""";
        fzfProc.StartInfo.RedirectStandardInput = true;
        fzfProc.StartInfo.RedirectStandardOutput = true;
        return fzfProc;
    }

    private uint GetFzfUserInput(ScraperRecord[] searchResults)
    {
        fzfProc.Start();

        // {uid};{song_name} ?{part} by {artist} ({content_type}) ?v{version}
        StringBuilder sb = new();
        foreach (ScraperRecord r in searchResults)
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

        uint choiceUid = uint.Parse(fzfOut.Substring(0, fzfOut.IndexOf(';')));
        return choiceUid;
    }

    private string PageContentPrettify(ScraperRecord r)
    {
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

    private ScraperRecord[] SearchUG(string query)
    {
        ScraperRecord[] results;
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
}
