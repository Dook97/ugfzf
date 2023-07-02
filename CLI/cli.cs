using System;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        var scraper = new Scraper();
        try {
            scraper.LoadData(args[0]);
            Console.Out.WriteLine(scraper.GetChords());
        } catch (ScraperException e) {
            Console.Error.WriteLine($"An error occured: {e.Message}\nexiting");
            Environment.Exit(1);
        }
    }
}
