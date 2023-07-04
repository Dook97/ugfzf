using System;
using System.ComponentModel;
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
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(result))
                    Console.WriteLine($"{descriptor.Name}: {descriptor.GetValue(result)}");
                Console.WriteLine();
            }
        } catch (ScraperException e) {
            Console.Error.WriteLine($"An error occured: {e.Message}");
            Environment.Exit(1);
        }
    }
}
