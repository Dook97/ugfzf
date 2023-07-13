using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UGScraper;

namespace CLI;

/// <summary>
/// Represents the user interface and its logic.
/// </summary>
class Cli
{
    private SearchScraper searchScraper { get; }
    private PageScraper pageScraper { get; }
    private Options opts { get; }
    private string cmdlineQuery { get; } // if we're in interactive mode
    private string[] cmdlineUrls { get; } // if in url-scraping mode

    private HashSet<contentType> allowedTypes { get; }
    private ScraperRecord[]? searchResults { get; set; }

    public Cli(Options opts)
    {
        this.searchScraper = new();
        this.pageScraper = new();
        this.opts = opts;

        this.cmdlineQuery = string.Join(' ', opts.queryToks).Trim();
        if (this.cmdlineQuery.Length == 0)
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
            FetchAndPrint(this.cmdlineUrls);
        else
            SearchAndPrint();
    }

    /// <summary>
    /// Run the program in interactive mode (with fzf).
    /// </summary>
    private void SearchAndPrint()
    {
        Console.Error.Write("Searching - this may take a while");

        bool isValid(ScraperRecord r) =>
            r.ContentUrl is not null && r.ContentIsPlaintext && this.allowedTypes.Contains(r.Type);

        // this may spit out an exception and I'm ok with that
        this.searchScraper.LoadData(this.cmdlineQuery);
        this.searchResults = this.searchScraper.GetSearchResults();
        this.searchResults = this.searchResults!.Where(i => isValid(i)).ToArray();

        // some ANSI escape code magic to remove the "Searching" text from stderr
        var stderr = Console.OpenStandardError();
        stderr.WriteByte(0x1b);
        Console.Error.Write("[1G"); // move cursor to beggining of line
        stderr.WriteByte(0x1b);
        Console.Error.Write("[0K"); // clear line right of cursor

        if (this.searchResults.Length == 0)
        {
            Console.Error.WriteLine($"No entry of specified type was found for query '{this.cmdlineQuery}'");
            Environment.Exit(1);
        }

        var searchLookup = this.searchResults.ToDictionary(i => i.ScrapeUid, i => i);
        uint[] choiceUids = GetFzfUserInput();
        string[] pageUrls = choiceUids.Select(uid => searchLookup[uid].ContentUrl!).ToArray();

        FetchAndPrint(pageUrls);
    }

    /// <summary>
    /// Consecutively fetch resources from the given URLs and, if valid, print their contents to stdout.
    /// </summary>
    private void FetchAndPrint(string[] urls)
    {
        for (int i = 0; i < urls.Length; ++i)
        {
            // blank line for separation
            if (i != 0)
                Console.WriteLine();

            ScraperRecord pageRecord;
            try
            {
                this.pageScraper.LoadData(urls[i]);
                pageRecord = this.pageScraper.GetRecord();
            }
            catch (ScraperException e)
            {
                Console.WriteLine($"""
                    ### ERROR ###
                    {e.Message}
                    ### ERROR ###
                    """);
                continue;
            }

            // TODO: user defined output format
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

    /// <summary>
    /// Parse the value of the '-t/--types' cmdline option.
    /// </summary>
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

        // if the user entered nonsense, just revert back to default
        if (output.Count == 0)
        {
            output.Add(contentType.chords);
            output.Add(contentType.tab);
        }

        return output;
    }

    /// <summary>
    /// Helper to setup a Process object for running fzf.
    /// </summary>
    private Process MakeFzfProc()
    {
        Process fzfProc = new();
        fzfProc.StartInfo.FileName = "fzf";
        // see man fzf, but brief explanation:
        // -d: sets a delimeter character
        // --with-nth: selects which fields to display (here we hide the UID, which is the first field)
        // --nth: fields to exclude from search (we exclude the UID)
        // --reverse: layout with the input on top; its better because <Tab> always moves the selection downwards
        // +m or -m: disable or enable multi-select with <Tab>
        fzfProc.StartInfo.Arguments =
            """-d ";" --with-nth=2.. --nth=1 --reverse """ + (this.opts.NoMulti ? "+m" : "-m");
        fzfProc.StartInfo.RedirectStandardInput = true;
        fzfProc.StartInfo.RedirectStandardOutput = true;
        return fzfProc;
    }

    /// <summary>
    /// Run fzf, supply formated input to it and retrieve UIDs of
    /// the selected items from its output.
    ///
    /// If fzf exits in an unexpected way, ugfzf will be killed as
    /// well with a matching error message and exit code.
    /// </summary>
    /// <returns>
    /// An array of UIDs of the selected ScraperRecords
    /// </returns>
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
            uids.Add(uint.Parse(line.Substring(0, line.IndexOf(';')))); // let's pretend this is safe

        return uids.ToArray();
    }

    /// <summary>
    /// Converts the raw content string to something nicer looking.
    /// </summary>
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
}
