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
     * type        - filter by content type; see enum contentType below
     *             - no "type" parameter means "all"
     *             - multiple allowed types may be specified using multiple parameters
     *               of the form type[n]=value, where n starts at 0
     */
    private const string searchMetaUrl =
        @"https://www.ultimate-guitar.com/search.php?search_type={0}&value={1}&page=";
    // path to the json item which stores the search results for the current page
    private const string jsonSearchResultsPath = "store.page.data.results";
    // path to the json item which stores info about number of search result pages
    private const string jsonPaginationPath = "store.page.data.pagination.total";
    // an array of scraped data from each search page
    private JsonNode[]? scrapeData;
    // number of search result pages
    private uint pageCount;

    // values of URL parameter(s) denoting type of requested content
    public enum contentType
    {
        video = 100, // page with a youtube embed link
        tab = 200,
        chord = 300,
        bass = 400,
        pro_tab = 500, // inaccessible for us; not plaintext
        power_tab = 600, // inaccessible for us; not plaintext
        drums = 700,
        ukulele = 800,
        official = 900, // inaccessible for us; paid
    }

    public SearchScraper()
    {
        scrapeData = null;
    }

    public override void LoadData(string searchQuery)
    {
        var urlEncodedQuery = HttpUtility.UrlEncode(searchQuery);
        if (urlEncodedQuery is null)
            throw new ScraperException($"Couldn't process search query ({searchQuery})");

        var baseSearchUrl = string.Format(searchMetaUrl, "title", urlEncodedQuery);

        var initialPageUrl = ScrapeUrl(baseSearchUrl + "1");
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

        this.scrapeData = new JsonNode[this.pageCount];
        this.scrapeData[0] = initialPageUrl;
        // TODO: parallelize
        for (uint i = 1; i < this.pageCount; ++i)
            scrapeData[i] = ScrapeUrl(baseSearchUrl + $"{i+1}");
    }

    // return the results just as we got them from UG
    public List<JsonNode> GetSearchResultsRaw()
    {
        if (this.scrapeData is null)
            throw new ScraperException("Scraper not properly initialized");

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

    public SearchScraperRecord[] GetSearchResults()
    {
        var rawSearchResults = GetSearchResultsRaw();
        var searchRecords = new SearchScraperRecord[rawSearchResults.Count];
        for (int i = 0; i < rawSearchResults.Count; ++i)
            // TODO: this is retarded; rework it
            searchRecords[i] = JsonSerializer.Deserialize<SearchScraperRecord>(rawSearchResults[i].ToJsonString());
        return searchRecords;
    }

    public JsonNode[] Dump()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");
        return scrapeData;
    }
}
