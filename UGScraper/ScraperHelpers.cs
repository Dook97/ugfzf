using System;

namespace UGScraper;

public class ScraperException : Exception
{
    protected ScraperException() : base() { }
    public ScraperException(string msg) : base(msg) { }
    public ScraperException(string msg, Exception innerException) : base(msg, innerException) { }
}

class SearchScraperDeserializationRecord
{
    public int? song_id { get; set; }
    public string? song_name { get; set; }
    public string? artist_name { get; set; }
    public string? type { get; set; }
    public int? votes { get; set; }
    public double? rating { get; set; }
    public string? date { get; set; }
    public string? artist_url { get; set; }
    public string? tab_url { get; set; }
}

public class SearchScraperRecord
{
    public uint ScrapeUid { get; }
    public int? SongId { get; }
    public string? SongName { get; }
    public string? ArtistName { get; }
    public contentType Type { get; }
    public int? Votes { get; }
    public double? Rating { get; }
    public DateTime? Date { get; }
    public string? ArtistUrl { get; }
    public string? TabUrl { get; }

    internal SearchScraperRecord(SearchScraperDeserializationRecord r, uint uid)
    {
        this.ScrapeUid = uid;
        this.SongId = r.song_id;
        this.SongName = r.song_name;
        this.ArtistName = r.artist_name;
        this.Type = ScraperTools.ToContentType(r.type);
        this.Votes = r.votes;
        this.Rating = r.rating;
        this.Date = r.date is null ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(r.date)).DateTime;
        this.ArtistUrl = r.artist_url;
        this.TabUrl = r.tab_url;
    }
}

static class ScraperTools
{
    public static contentType ToContentType(string? strtype)
    {
        switch (strtype?.ToLower())
        {
            case "tabs":
                return contentType.tab;
            case "chords":
                return contentType.chord;
            case "bass tabs":
            case "basstabs":
                return contentType.bass;
            case "pro":
                return contentType.proTab;
            case "power":
                return contentType.powerTab;
            case "drum tabs":
            case "drumtabs":
                return contentType.drums;
            case "ukulele chords":
            case "ukulelechords":
                return contentType.ukulele;
            case "video":
                return contentType.video;
            default:
                return contentType.official;
        }
    }
}

// values of URL parameter(s) denoting type of requested content
public enum contentType
{
    video = 100, // page with a youtube embed link
    tab = 200,
    chord = 300,
    bass = 400,
    proTab = 500, // inaccessible for us; not plaintext
    powerTab = 600, // inaccessible for us; not plaintext
    drums = 700,
    ukulele = 800,
    official = 900, // inaccessible for us; paid
}
