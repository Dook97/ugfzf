using System;
using System.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace UGScraper;

public class Scraper
{
    public string TestScrape(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);
        var item = doc.DocumentNode.SelectNodes("//div[@class='js-store']");
        var raw = HttpUtility.HtmlDecode(item.First().Attributes["data-content"].Value);
        var json = JsonSerializer.Deserialize<JsonNode>(raw.AsSpan());
        var text = json["store"]["page"]["data"]["tab_view"]["wiki_tab"]["content"];
        var rgx = new Regex(@"\[[/]?(ch|tab)\]");
        var clean = rgx.Replace(text.ToString(), "");
        return clean;
    }
}
