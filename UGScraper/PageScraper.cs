using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JsonTools;

namespace UGScraper;
public class PageScraper : BaseScraper
{
    // json path to the content of the site (like tabs or chords)
    private const string jsonDataPath = "store.page.data";
    // json containing all data as it was retrieved from UG
    private JsonNode? scrapeData;
    private Regex contentMetaTextRgx;
    private string? url;

    public PageScraper()
    {
        this.scrapeData = null;
        this.url = null;
        this.contentMetaTextRgx = new Regex(@"\[/?(ch|tab)\]");
    }

    public override void LoadData(string url)
    {
        this.url = url;
        scrapeData = ScrapeUrl(url);
    }

    public PageScraperRecord GetRecord()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        JsonNode? dataNode = scrapeData.GetByPath(jsonDataPath);
        JsonNode? tabNode = dataNode?.GetByPath("tab");
        if (dataNode is null || tabNode is null)
            throw new ScraperException($"Unable to find required data in the retrieved document ({url})");

        var record = tabNode.Deserialize<PageScraperDeserializationRecord>();
        if (record is null)
            throw new ScraperException($"Deserialization error on document ({url})");

        // TODO: figure out a way to have these set during deserialization
        // ideas:
        //  * flatten the json before deserialization
        //  * investigate custom deserializers
        record.tuning = dataNode.GetByPath("tab_view.meta.tuning.value")?.GetValue<string>();
        record.content_annotated = dataNode.GetByPath("tab_view.wiki_tab.content")?.GetValue<string>();
        record.content = record.content_annotated is null ? null : contentMetaTextRgx.Replace(record.content_annotated, "");

        return new PageScraperRecord(record, GetNextItemUid());
    }
}

class PageScraperDeserializationRecord
{
    public string? song_name { get; init; }
    public string? artist_name { get; init; }
    public string? type { get; init; }
    public string? part { get; init; }
    public uint? version { get; init; }
    public uint? votes { get; init; }
    public double? rating { get; init; }
    public string? version_description { get; init; }
    public string? artist_url { get; init; }
    public string? tab_url { get; init; }

    // fill these manually
    public string? tuning { get; set; }
    public string? content { get; set; }
    public string? content_annotated { get; set; }
}

public class PageScraperRecord
{
    public uint ScraperUid { get; }
    public string? SongName { get; }
    public string? ArtistName { get; }
    public contentType Type { get; }
    public string? Part { get; }
    public uint? Version { get; }
    public uint? Votes { get; }
    public double? Rating { get; }
    public string? VersionDescription { get; }
    public string? ArtistUrl { get; }
    public string? ContentUrl { get; }
    public string? Tuning { get; }
    public string? Content { get; }
    public string? ContentAnnotated { get; }

    internal PageScraperRecord(PageScraperDeserializationRecord r, uint uid)
    {
        this.ScraperUid = uid;
        this.SongName = r.song_name;
        this.ArtistName = r.artist_name;
        this.Type = ScraperTools.ToContentType(r.type);
        this.Part = r.part;
        this.Version = r.version;
        this.Votes = r.votes;
        this.Rating = r.rating;
        this.VersionDescription = r.version_description;
        this.ArtistUrl = r.artist_url;
        this.ContentUrl = r.tab_url;
        this.Tuning = r.tuning;
        this.Content = r.content;
        this.ContentAnnotated = r.content_annotated;
    }
}
