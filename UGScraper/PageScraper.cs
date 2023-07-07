using System.Text.Json;
using System.Text.Json.Nodes;

namespace UGScraper;
public class PageScraper : BaseScraper
{
    // json path to the content of the site (like tabs or chords)
    private const string jsonDataPath = "store.page.data";
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
        this.scrapeData = null;
        this.url = null;

        this.url = url;
        scrapeData = ScrapeUrl(url);
    }

    public ScraperRecord GetRecord()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        JsonNode? dataNode = scrapeData.GetByPath(jsonDataPath);
        JsonNode? tabNode = dataNode?.GetByPath("tab");
        if (dataNode is null || tabNode is null)
            throw new ScraperException($"Unable to find required data in the retrieved document ({url})");

        var record = tabNode.Deserialize<DeserializationRecord>();
        if (record is null)
            throw new ScraperException($"Deserialization error ({url})");

        // TODO: figure out a way to have these set during deserialization
        // ideas:
        //  * flatten the json before deserialization
        //  * investigate custom deserializers
        record.tuning = dataNode.GetByPath("tab_view.meta.tuning.value")?.GetValue<string>();
        record.content = dataNode.GetByPath("tab_view.wiki_tab.content")?.GetValue<string>();

        return new ScraperRecord(record, GetNextItemUid());
    }
}
