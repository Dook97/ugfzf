using System;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        var scraper = new PageScraper();
        try {
            scraper.LoadData(args[0]);
            Console.Out.WriteLine(scraper.GetChords());
        } catch (ScraperException e) {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Environment.Exit(1);
        }
    }
}