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
            var results = scraper.GetSearchResultsRaw();
            foreach (var result in results)
                Console.WriteLine(result);
        } catch (ScraperException e) {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Environment.Exit(1);
        }
    }
}
