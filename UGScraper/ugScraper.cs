using System;
using HtmlAgilityPack;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Diagnostics;
using JsonTools;

namespace UGScraper;

public class Scraper
{
    // xpath identifier of the element which stores the data we need to scrape
    private static string xpathDataIdentifier = "//div[@class='js-store']";
    // name of the atribute which stores the data
    private static string htmlDataAttr = "data-content";
    // parsed json containing all data as it was retrieved from ug
    private JsonNode? ugData;

    public Scraper()
    {
        this.ugData = null;
    }

    /* System.Net.WebException          - unavailable
     * System.UriFormatException        - invalid URI
     * System.Text.Json.JsonException   - invalid json recieved for deserialization
     */
    public void LoadData(string url)
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
        Debug.Assert(json is not null); // should never happen

        ugData = json;
    }

    public string GetChords()
    {
        var contentNode = ugData.GetByPath("store.page.data.tab_view.wiki_tab.content");
        if (contentNode is null)
            throw new Exception();

        var text = contentNode.ToString();
        var metaTextRgx = new Regex(@"\[/?(ch|tab)\]");
        var clean = metaTextRgx.Replace(text, "");
        return clean;
    }

    public JsonNode? DumpAll() => ugData;
}
