using System;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JsonTools;

namespace UGScraper;

public class Scraper
{
    // xpath identifier of the element which stores the data we need to scrape
    private static string xpathDataIdentifier = "//div[@class='js-store']";
    // name of the atribute which stores the data
    private static string htmlDataAttr = "data-content";
    // json containing all data as it was retrieved from UG
    private JsonNode scrapeData;

    public Scraper(string url)
    {
        this.scrapeData = LoadData(url);
    }

    /* System.Net.WebException          - unavailable
     * System.UriFormatException        - invalid URI
     * System.Text.Json.JsonException   - invalid json recieved for deserialization
     *
     * TODO: replace Exception with own type
     */
    private JsonNode LoadData(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);

        var htmlDataNode = doc.DocumentNode.SelectSingleNode(xpathDataIdentifier);
        if (htmlDataNode is null)
            throw new Exception();

        // all data is stored in an html attribute as html encoded json
        if (!htmlDataNode.Attributes.Contains(htmlDataAttr))
            throw new Exception();
        var rawData = HttpUtility.HtmlDecode(htmlDataNode.Attributes[htmlDataAttr].Value);

        var json = JsonSerializer.Deserialize<JsonNode>(rawData.AsSpan());
        if (json is null)
            throw new Exception();

        return json;
    }

    public string GetChords()
    {
        const string jsonChordPath = "store.page.data.tab_view.wiki_tab.content";

        var contentNode = scrapeData.GetByPath(jsonChordPath);
        if (contentNode is null)
            throw new Exception();

        var text = contentNode.ToString();
        var metaTextRgx = new Regex(@"\[/?(ch|tab)\]");
        var clean = metaTextRgx.Replace(text, "");

        return clean;
    }

    public JsonNode DumpAll() => scrapeData;
}
