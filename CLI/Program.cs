using System;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        var scraper = new Scraper();
        Console.WriteLine(scraper.TestScrape(args[0]));
    }
}
