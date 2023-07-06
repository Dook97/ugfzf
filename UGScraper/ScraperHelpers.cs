using System;

namespace UGScraper;

public class ScraperException : Exception
{
    protected ScraperException() : base() { }
    public ScraperException(string msg) : base(msg) { }
    public ScraperException(string msg, Exception innerException) : base(msg, innerException) { }
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
                return contentType.chords;
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
    chords = 300,
    bass = 400,
    proTab = 500, // inaccessible for us; not plaintext
    powerTab = 600, // inaccessible for us; not plaintext
    drums = 700,
    ukulele = 800,
    official = 900, // inaccessible for us; paid
}
