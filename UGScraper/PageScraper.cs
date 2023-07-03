using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JsonTools;

namespace UGScraper;

public class PageScraper : BaseScraper
{
    // json containing all data as it was retrieved from UG
    private JsonNode? scrapeData;

    public PageScraper() : base()
    {
        this.scrapeData = null;
    }

    public override void LoadData(string url)
    {
        scrapeData = ScrapeUrl(url);
        this.url = url;
    }

    public string GetChordsAnotated()
    {
        const string jsonChordPath = "store.page.data.tab_view.wiki_tab.content";

        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        var contentNode = scrapeData.GetByPath(jsonChordPath);
        if (contentNode is null)
            throw new ScraperException($"Unable to find chords in the retrieved document ({url})");

        var text = contentNode.ToString();

        return text;
    }

    public string GetChords() => Regex.Replace(GetChordsAnotated(), @"\[/?(ch|tab)\]", "");

    public JsonNode Dump()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");
        return scrapeData;
    }
}
