using System;
using System.Web;

namespace UGScraper;

/// <summary>
/// Type containing curated information from the scraper.
/// For search scraper records the content field will always be null.
/// Be careful with this - UG is apparently kind of a mess and availability
/// of this info is absolutely not guaranteed. Always check for the null case.
/// </summary>
public class ScraperRecord
{
    public uint ScrapeUid { get; }
    public string? SongName { get; }
    public string? ArtistName { get; }
    public contentType Type { get; }
    public string? Part { get; }
    public uint? Version { get; }
    public string? VersionDescription { get; }
    public uint? Votes { get; }
    public double? Rating { get; }
    public bool ContentIsPlaintext { get; }
    public string? ArtistUrl { get; }
    public string? ContentUrl { get; }
    public string? Tuning { get; }
    public string? Content { get; }

    internal ScraperRecord(DeserializationRecord r, uint uid)
    {
        this.ScrapeUid = uid;
        this.SongName = r.song_name;
        this.ArtistName = r.artist_name;
        this.Type = ScraperTools.ToContentType(r.type);
        this.Part = r.part;
        this.Version = r.version;
        this.Votes = r.votes;
        this.Rating = r.rating;
        this.VersionDescription = r.version_description?.Replace("\r\n", Environment.NewLine);
        // the tp_version check should suffice, but it doesn't hurt to be defensive
        this.ContentIsPlaintext = r.tp_version == 0
                                  && this.Type != contentType.official
                                  && this.Type != contentType.proTab
                                  && this.Type != contentType.powerTab;
        this.ArtistUrl = r.artist_url;
        this.ContentUrl = r.tab_url;
        this.Tuning = r.tuning;
        this.Content = HttpUtility.HtmlDecode(r.content?.Replace("\r\n", Environment.NewLine));
    }
}

/// <summary>
/// Helper type for easy deserialization using reflection.
/// </summary>
class DeserializationRecord
{
    public string? song_name { get; init; }
    public string? artist_name { get; init; }
    public string? type { get; init; }
    public string? part { get; init; }
    public uint? version { get; init; }
    public string? version_description { get; init; }
    public uint? votes { get; init; }
    public double? rating { get; init; }
    public uint? tp_version { get; init; } // version of tab viewer - non-zero means content isn't plaintext
    public string? artist_url { get; init; }
    public string? tab_url { get; init; }

    // fill these manually
    // TODO: figure out a way how to not have to do that
    public string? tuning { get; set; }
    public string? content { get; set; }
}
