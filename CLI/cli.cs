using System;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        var scraper = new SearchScraper();
        try {
            scraper.LoadData(args[0]);
            var results = scraper.GetSearchResults();
            foreach (var result in results)
                Console.WriteLine($"{result.song_name} by {result.artist_name} ({result.type}; {result.tab_url})");
        } catch (ScraperException e) {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Environment.Exit(1);
        }
    }
}
