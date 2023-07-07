using System;
using System.Text.Json.Nodes;

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

    /// <summary>
    /// Get a value from nested JsonNodes in a safe and simple way.
    /// </summary>
    /// <param name="node">
    /// Root JsonNode - the path traversal begins here.
    /// </param>
    /// <param name="path">
    /// '.' separated fields identifying the relative location of the desired entry.
    /// </param>
    /// <returns>
    /// null if not found, else the requested node
    /// </returns>
    public static JsonNode? GetByPath(this JsonNode node, string path)
    {
        JsonNode? output = node;
        var pathArr = path.Split(".");
        for (int i = 0; i < pathArr.Length; ++i)
        {
            try
            {
                output = output?[pathArr[i]];
            }
            catch
            {
                return null;
            }
        }
        return output;
    }
}

/// <summary>
/// Possible types of content UG provides.
/// </summary>
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
