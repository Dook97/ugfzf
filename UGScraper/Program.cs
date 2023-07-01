using System;
using HtmlAgilityPack;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace UGScraper;

public class Scraper
{
    // xpath identifier of the element which stores the data we need to scrape
    private static string xpathDataIdentifier = "//div[@class='js-store']";
    // name of the atribute which stores the data
    private static string htmlDataAttr = "data-content";
    // parsed json containing all data as it was retrieved from ug
    private JsonNode ugData;

    public Scraper(string url) {
        this.ugData = LoadData(url);
    }

    /* System.Net.WebException          - unavailable
     * System.UriFormatException        - invalid URI
     * System.Text.Json.JsonException   - invalid json recieved for deserialization
     */
    private JsonNode LoadData(string url)
    {
        // load the webpage
        var web = new HtmlWeb();
        var doc = web.Load(url);

        // select the data-storing element from the page
        HtmlNode? item;
        if ((item = doc.DocumentNode.SelectSingleNode(xpathDataIdentifier)) == null)
            throw new Exception();

        // all data is stored in an html attribute as html encoded json
        if (!item.Attributes.Contains(htmlDataAttr))
            throw new Exception();
        var raw = HttpUtility.HtmlDecode(item.Attributes[htmlDataAttr].Value);

        var json = JsonSerializer.Deserialize<JsonNode>(raw.AsSpan());
        Debug.Assert(json is not null); // shouldn't happen

        return json;
    }

    public string GetChords()
    {
        var text = ugData["store"]["page"]["data"]["tab_view"]["wiki_tab"]["content"];
        var rgx = new Regex(@"\[[/]?(ch|tab)\]");
        var clean = rgx.Replace(text.ToString(), "");
        return clean;
    }

    public JsonNode DumpAll() => ugData;
}
