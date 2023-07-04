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
    public int SongId { get; set; }
    public string SongName { get; set; }
    public string ArtistName { get; set; }
    public contentType Type { get; set; }
    public int Votes { get; set; }
    public double Rating { get; set; }
    public DateTime Date { get; set; }
    public string ArtistUrl { get; set; }
    public string TabUrl { get; set; }

    internal SearchScraperRecord(SearchScraperDeserializationRecord r)
    {
        this.SongId = r.song_id ?? -1;
        this.SongName = r.song_name!;
        this.ArtistName = r.artist_name!;
        this.Type = r.type.ToContentType();
        this.Votes = r.votes ?? -1;
        this.Rating = r.rating ?? -1;
        this.Date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(r.date ?? "0")).DateTime;
        this.ArtistUrl = r.artist_url!;
        this.TabUrl = r.tab_url!;
    }
}

public static class ScraperTools
{
    public static contentType ToContentType(this string? strtype)
    {
        switch (strtype)
        {
            case "Tabs":
            case "tabs":
                return contentType.tab;
            case "Chords":
            case "chords":
                return contentType.chord;
            case "Bass Tabs":
            case "BassTabs":
                return contentType.bass;
            case "Pro":
            case "pro":
                return contentType.proTab;
            case "Power":
            case "power":
                return contentType.powerTab;
            case "Drum Tabs":
            case "DrumTabs":
                return contentType.powerTab;
            case "Ukulele Chords":
            case "UkuleleChords":
                return contentType.ukulele;
            case "Video":
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
