using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using HtmlAgilityPack;

namespace UGScraper;

/// <summary>
/// Provides basic functionality for loading jsonified content from UG html
/// documents as well as some basic shared interface for the derived scraper types.
/// </summary>
public abstract class BaseScraper
{
    // xpath identifier of the element which stores the data we need to scrape
    private const string xpathDataId = "//div[@class='js-store']";
    // name of the atribute which stores the data
    private const string htmlDataAttr = "data-content";
    // unique identifier of the next scraped item
    private uint nextItemUid = 0;
    protected uint GetNextItemUid() => nextItemUid++;

    /// <summary>
    /// Load data into the scraper object for further manipulation.
    /// This may be called repeatedly for different queries.
    /// </summary>
    public abstract void LoadData(string query);

    /// <summary>
    /// Get deserialized representation of the UG page content encoded as JSON.
    /// </summary>
    /// <param name="url">
    /// URL from which to extract the json.
    /// </param>
    /// <returns>
    /// Content of the given UG web document located at url as deserialized JSON.
    /// </returns>
    protected JsonNode ScrapeUrl(string url)
    {
        var web = new HtmlWeb();
        HtmlDocument doc;

        try
        {
            doc = web.Load(url);
        }
        catch (Exception e)
        {
            throw new ScraperException($"Unable to retrieve document ({url})", e);
        }

        HtmlNode? htmlDataNode = doc.DocumentNode.SelectSingleNode(xpathDataId);
        if (htmlDataNode is null || !htmlDataNode.Attributes.Contains(htmlDataAttr))
            throw new ScraperException($"Unable to find required data in the retrieved document ({url})");

        // all data is stored in an html attribute as html encoded json
        string? rawData = HttpUtility.HtmlDecode(htmlDataNode.Attributes[htmlDataAttr].Value);

        JsonNode scrapeData;
        try
        {
            scrapeData = JsonSerializer.Deserialize<JsonNode>(rawData.AsSpan())!;
        }
        catch (JsonException e)
        {
            throw new ScraperException("Error when parsing json data", e);
        }

        return scrapeData;
    }

}
