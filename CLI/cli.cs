using System;
using UGScraper;

namespace CLI;
class Program
{
    static void Main(string[] args)
    {
        var scraper = new Scraper();
        scraper.LoadData(args[0]);
        Console.Out.WriteLine(scraper.GetChords());
    }
}
