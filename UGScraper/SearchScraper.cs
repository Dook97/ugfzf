using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using JsonTools;

namespace UGScraper;
public class SearchScraper : BaseScraper
{
    /* About url parameters accepted by UG
     *
     * search_type - "title", "band" and possibly other stuff (eg forum posts)
     * value       - URL/percent-encoded query
     * page        - used when results don't fit on a single page; starts at 1
     * type        - filter by content type; see enum contentType
     *             - no "type" parameter means "all"
     *             - multiple allowed types may be specified using multiple parameters
     *               of the form type[n]=value, where n starts at 0
     *
     * This is a template search url to be filled by string.Format
     */
    private const string searchMetaUrl =
        @"https://www.ultimate-guitar.com/search.php?search_type={0}&value={1}&page={{0}}";
    // path to the json item which stores the search results for the current page
    private const string jsonSearchResultsPath = "store.page.data.results";
    // path to the json item which stores info about number of search result pages
    private const string jsonPaginationPath = "store.page.data.pagination.total";
    // an array of scraped data from each search page
    private JsonNode[]? scrapeData;
    // number of search result pages
    private uint pageCount;

    public SearchScraper()
    {
        scrapeData = null;
    }

    // TODO: make it accept type parameters and generally get rid of bad code
    public override void LoadData(string searchQuery)
    {
        var urlEncodedQuery = HttpUtility.UrlEncode(searchQuery);
        if (urlEncodedQuery is null)
            throw new ScraperException($"Couldn't process search query ({searchQuery})");

        var baseSearchUrl = string.Format(searchMetaUrl, "title", urlEncodedQuery);

        var initialPageUrl = ScrapeUrl(string.Format(baseSearchUrl, 1));
        var pageCountNode = initialPageUrl.GetByPath(jsonPaginationPath);
        if (pageCountNode is null)
            throw new ScraperException($"Retrieved document ({initialPageUrl}) is missing essential data (pagination)");

        try
        {
            this.pageCount = pageCountNode.GetValue<uint>();
        }
        catch (System.FormatException e)
        {
            throw new ScraperException($"Couldn't parse data from retrieved document ({initialPageUrl})", e);
        }

        if (this.pageCount == 0)
        {
            this.scrapeData = null;
            return;
        }

        this.scrapeData = new JsonNode[this.pageCount];
        this.scrapeData[0] = initialPageUrl;
        // TODO: parallelize
        for (uint i = 1; i < this.pageCount; ++i)
            scrapeData[i] = ScrapeUrl(string.Format(baseSearchUrl, i + 1));
    }

    // return the results just as we got them from UG
    public List<JsonNode>? GetSearchResultsRaw()
    {
        if (this.scrapeData is null)
            return null;

        List<JsonNode> results = new();

        foreach (var pageData in this.scrapeData)
        {
            var pageResults = pageData.GetByPath(jsonSearchResultsPath);
            // one malformed document shouldn't abort the entire thing
            // TODO: figure out a way to signal that there was an issue
            if (pageResults is not null)
                results.AddRange(pageResults.AsArray()!);
        }

        return results;
    }

    public SearchScraperRecord[]? GetSearchResults()
    {
        var rawSearchResults = GetSearchResultsRaw();

        if (rawSearchResults is null)
            return null;

        var searchRecords = new SearchScraperRecord[rawSearchResults.Count];
        for (int i = 0; i < rawSearchResults.Count; ++i)
        {
            var rawRecord = rawSearchResults[i].Deserialize<SearchScraperDeserializationRecord>()!;
            searchRecords[i] = new SearchScraperRecord(rawRecord, (uint)i);
        }
        return searchRecords;
    }
}

class SearchScraperDeserializationRecord
{
    public int? song_id { get; init; }
    public string? song_name { get; init; }
    public string? artist_name { get; init; }
    public string? type { get; init; }
    public int? votes { get; init; }
    public double? rating { get; init; }
    public string? date { get; init; }
    public string? artist_url { get; init; }
    required public string tab_url { get; init; }
}

public class SearchScraperRecord
{
    public uint ScrapeUid { get; }
    public int? SongId { get; }
    public string? SongName { get; }
    public string? ArtistName { get; }
    public contentType Type { get; }
    public int? Votes { get; }
    public double? Rating { get; }
    public DateTime? Date { get; }
    public string? ArtistUrl { get; }
    public string? TabUrl { get; }

    internal SearchScraperRecord(SearchScraperDeserializationRecord r, uint uid)
    {
        this.ScrapeUid = uid;
        this.SongId = r.song_id;
        this.SongName = r.song_name;
        this.ArtistName = r.artist_name;
        this.Type = ScraperTools.ToContentType(r.type);
        this.Votes = r.votes;
        this.Rating = r.rating;
        this.Date = r.date is null ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(r.date)).DateTime;
        this.ArtistUrl = r.artist_url;
        this.TabUrl = r.tab_url;
    }
}
