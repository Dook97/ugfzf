using System;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JsonTools;

namespace UGScraper;

public class ScraperException : Exception
{
    protected ScraperException() : base() { }
    public ScraperException(string msg) : base(msg) { }
    public ScraperException(string msg, Exception innerException) : base(msg, innerException) { }
}

public class Scraper
{
    // xpath identifier of the element which stores the data we need to scrape
    private static string xpathDataId = "//div[@class='js-store']";
    // name of the atribute which stores the data
    private static string htmlDataAttr = "data-content";
    // json containing all data as it was retrieved from UG
    private JsonNode? scrapeData;

    public Scraper()
    {
        this.scrapeData = null;
    }

    public void LoadData(string url)
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

        var htmlDataNode = doc.DocumentNode.SelectSingleNode(xpathDataId);
        if (htmlDataNode is null)
            throw new ScraperException($"Unable to find required data in the retrieved document ({url})");

        // all data is stored in an html attribute as html encoded json
        if (!htmlDataNode.Attributes.Contains(htmlDataAttr))
            throw new ScraperException($"Unable to find required data in the retrieved document ({url})");
        var rawData = HttpUtility.HtmlDecode(htmlDataNode.Attributes[htmlDataAttr].Value);

        JsonNode json;
        try
        {
            json = JsonSerializer.Deserialize<JsonNode>(rawData.AsSpan())!;
        }
        catch (System.Text.Json.JsonException e)
        {
            throw new ScraperException("Error when parsing json data", e);
        }

        scrapeData = json;
    }

    public string GetChords()
    {
        const string jsonChordPath = "store.page.data.tab_view.wiki_tab.content";

        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        var contentNode = scrapeData.GetByPath(jsonChordPath);
        if (contentNode is null)
            throw new ScraperException("Unable to find chords in the retrieved document");

        var text = contentNode.ToString();
        var metaTextRgx = new Regex(@"\[/?(ch|tab)\]");
        var clean = metaTextRgx.Replace(text, "");

        return clean;
    }

    public JsonNode DumpAll() {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");
        return scrapeData;
    }
}
