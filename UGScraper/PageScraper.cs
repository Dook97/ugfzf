using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JsonTools;

namespace UGScraper;
public class PageScraper : BaseScraper
{
    // json path to the content of the site (like tabs or chords)
    private const string jsonContentPath = "store.page.data.tab_view.wiki_tab.content";
    // json containing all data as it was retrieved from UG
    private JsonNode? scrapeData;
    private string? url;

    public PageScraper()
    {
        this.scrapeData = null;
        this.url = null;
    }

    public override void LoadData(string url)
    {
        scrapeData = ScrapeUrl(url);
        this.url = url;
    }

    public string GetChordsAnotated()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        var contentNode = scrapeData.GetByPath(jsonContentPath);
        if (contentNode is null)
            throw new ScraperException($"Unable to find chords in the retrieved document ({url})");

        var text = contentNode.ToString();

        return text;
    }

    public string GetChords()
    {
        var metaTextRgx = new Regex(@"\[/?(ch|tab)\]");
        return metaTextRgx.Replace(GetChordsAnotated(), "");
    }
}
