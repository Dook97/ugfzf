using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace UGScraper;
/// <summary>
/// Object encapsulating state and functionality needed for scraping search queries off of UG.
/// </summary>
public class SearchScraper : BaseScraper
{
    /// <summary>
    /// This is a template search url to be filled by string.Format.
    ///
    /// <list>
    ///
    /// About url parameters accepted by UG
    ///
    /// search_type - "title", "band" and possibly other stuff (eg forum posts)
    /// value       - URL/percent-encoded query
    /// page        - used when results don't fit on a single page; starts at 1
    /// type        - filter by content type; see enum contentType
    ///             - no "type" parameter means "all"
    ///             - multiple allowed types may be specified using multiple parameters
    ///               of the form type[n]=value, where n starts at 0
    /// </list>
    /// </summary>
    private const string searchMetaUrl =
        @"https://www.ultimate-guitar.com/search.php?search_type={0}&value={1}&page={{0}}";
    /// <summary> path to the json item which stores the search results for the current page </summary>
    private const string jsonSearchResultsPath = "store.page.data.results";
    // <summary> path to the json item which stores info about number of search result pages </summary>
    private const string jsonPaginationPath = "store.page.data.pagination.total";
    // <summary> an array of scraped data from each search page </summary>
    private JsonNode[]? scrapeData;
    // <summary> number of search result pages </summary>
    private uint pageCount;

    public SearchScraper()
    {
        scrapeData = null;
    }

    // TODO: make it accept type parameters and generally get rid of bad code
    public override void LoadData(string searchQuery)
    {
        string? urlEncodedQuery = HttpUtility.UrlEncode(searchQuery);
        if (urlEncodedQuery is null)
            throw new ScraperException($"Couldn't process search query ({searchQuery})");

        string baseSearchUrl = string.Format(searchMetaUrl, "title", urlEncodedQuery);

        // We have to load the first page separately to find out, have many search results pages there are for the query.
        JsonNode initialPageUrl = ScrapeUrl(string.Format(baseSearchUrl, 1));
        JsonNode? pageCountNode = initialPageUrl.GetByPath(jsonPaginationPath);
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

    /// <summary> return the results just as we got them from UG </summary>
    private List<JsonNode> GetSearchResultsRaw()
    {
        List<JsonNode> results = new();

        if (this.scrapeData is null)
            return results;

        foreach (JsonNode pageData in this.scrapeData)
        {
            JsonNode? pageResults = pageData.GetByPath(jsonSearchResultsPath);
            // one malformed document shouldn't abort the entire thing
            // TODO: figure out a way to signal that there was an issue
            if (pageResults is not null)
                results.AddRange(pageResults.AsArray()!);
        }

        return results;
    }

    /// <returns>
    /// curated scraped data as a ScraperRecord.
    /// </returns>
    public ScraperRecord[] GetSearchResults()
    {
        List<JsonNode> rawSearchResults = GetSearchResultsRaw();

        var searchRecords = new ScraperRecord[rawSearchResults.Count];
        for (int i = 0; i < rawSearchResults.Count; ++i)
        {
            var record = rawSearchResults[i].Deserialize<DeserializationRecord>();
            if (record is null)
                throw new ScraperException($"Deserialization error");
            searchRecords[i] = new ScraperRecord(record, this.GetNextItemUid());
        }
        return searchRecords;
    }
}
