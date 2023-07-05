using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JsonTools;

namespace UGScraper;
public class PageScraper : BaseScraper
{
    // json path to the content of the site (like tabs or chords)
    private const string jsonContentPath = "store.page.data.tab_view.wiki_tab.content";
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

    public string GetContentAnotated()
    {
        if (scrapeData is null)
            throw new ScraperException("Scraper not correctly initialized");

        var contentNode = scrapeData.GetByPath(jsonContentPath);
        if (contentNode is null)
            throw new ScraperException($"Unable to find content in the retrieved document ({url})");

        var text = contentNode.ToString();

        return text;
    }

    public string GetContent() => contentMetaTextRgx.Replace(GetContentAnotated(), "");
    // public JsonNode? Dump() => this.scrapeData;
}

class PageScraperDeserializationRecord
{
    public uint? id { get; init; }
    public uint? song_id { get; init; }
    public string? song_name { get; init; }
    public uint? artist_id { get; init; }
    public string? artist_name { get; init; }
    public string? type { get; init; }
    public uint? version { get; init; }
    public uint? votes { get; init; }
    public double? rating { get; init; }
    public string? version_description { get; init; }
    public string? artist_url { get; init; }
    public string? tab_url { get; init; }
    public string? content { get; init; }
    public string? tuning_name { get; init; }
    public string? tuning { get; init; }
    public string? difficulty { get; init; }
    public uint? view_total { get; init; }
    public uint? favorites_count { get; init; }
    public JsonNode?[]? versions { get; init; }
}

public class PageScraperRecord
{
    public uint ScraperUid { get; }
    public uint? Id { get; }
    public uint? SongId { get; }
    public string? SongName { get; }
    public uint? ArtistId { get; }
    public string? ArtistName { get; }
    public contentType Type { get; }
    public uint? Version { get; }
    public uint? Votes { get; }
    public double? Rating { get; }
    public string? VersionDescription { get; }
    public string? ArtistUrl { get; }
    public string? ContentUrl { get; }
    public string? Content { get; }
    public string? TuningName { get; }
    public string? Tuning { get; }
    public string? Difficulty { get; }
    public uint? ViewTotal { get; }
    public uint? FavoritesCount { get; }
    public PageScraperRecord?[]? Versions { get; }

    internal PageScraperRecord(PageScraperDeserializationRecord r, uint uid)
    {
        this.ScraperUid = uid;
        this.SongId = r.song_id;
        this.SongName = r.song_name;
        this.ArtistName = r.artist_name;
        this.Type = ScraperTools.ToContentType(r.type);
        this.Votes = r.votes;
        this.Rating = r.rating;
        this.ArtistUrl = r.artist_url;
        this.ContentUrl = r.tab_url;
    }
}
