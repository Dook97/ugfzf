using System;
using System.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ugfzf;
class Program
{
    static void Main(string[] args)
    {
        var web = new HtmlWeb();
        var doc = web.Load(args[0]);
        var item = doc.DocumentNode.SelectNodes("//div[@class='js-store']");
        var raw = HttpUtility.HtmlDecode(item.First().Attributes["data-content"].Value);
        var json = JsonSerializer.Deserialize<JsonNode>(raw.AsSpan());
        var text = json["store"]["page"]["data"]["tab_view"]["wiki_tab"]["content"];
        var rgx = new Regex(@"\[[/]?(ch|tab)\]");
        var clean = rgx.Replace(text.ToString(), "");
        Console.WriteLine(clean);
    }
}
