using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using HtmlAgilityPack;

namespace UGScraper
{
    public abstract class BaseScraper
    {
        // xpath identifier of the element which stores the data we need to scrape
        private const string xpathDataId = "//div[@class='js-store']";
        // name of the atribute which stores the data
        private const string htmlDataAttr = "data-content";
        // last loaded url
        protected string? url;

        public BaseScraper()
        {
            this.url = null;
        }

        // a method which loads data requested by query from the web
        // this is required before asking the scraper object for any information (like chords or results of a search)
        public abstract void LoadData(string query);

        protected JsonNode ScrapeUrl(string url)
        {
            this.url = url;

            var web = new HtmlWeb();
            HtmlDocument doc;

            try
            {
                doc = web.Load(url);
            }
            catch (Exception e)
            {
                throw new ScraperException($"Unable to retrieve document ({url})", e);
            }

            var htmlDataNode = doc.DocumentNode.SelectSingleNode(xpathDataId);
            if (htmlDataNode is null)
                throw new ScraperException($"Unable to find required data in the retrieved document ({url})");

            // all data is stored in an html attribute as html encoded json
            if (!htmlDataNode.Attributes.Contains(htmlDataAttr))
                throw new ScraperException($"Unable to find required data in the retrieved document ({url})");
            var rawData = HttpUtility.HtmlDecode(htmlDataNode.Attributes[htmlDataAttr].Value);

            JsonNode json;
            try
            {
                json = JsonSerializer.Deserialize<JsonNode>(rawData.AsSpan())!;
            }
            catch (System.Text.Json.JsonException e)
            {
                throw new ScraperException("Error when parsing json data", e);
            }

            return json;
        }

    }
}
