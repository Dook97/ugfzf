using System.Text.Json.Nodes;
using System.Web;
using JsonTools;

namespace UGScraper
{
    public class SearchScraper : BaseScraper
    {
        /* About url parameters accepted by UG
         *
         * search_type - "title" or "band"
         * value       - URL/percent-encoded query
         * page        - used when results don't fit on a single page; starts at 1
         * type        - filter by content type; see enum contentType below
         *             - no "type" parameter means "all"
         *             - multiple allowed types may be specified using multiple parameters
         *               of the form type[n]=value, where n starts at 0
         */
        private const string searchMetaUrl =
            @"https://www.ultimate-guitar.com/search.php?search_type={0}&value={1}&page={2}";
        // path to the json item which stores the search results for the current page
        private const string jsonSearchResultsPath = "store.page.data.results";
        // path to the json item which stores info about number of search result pages
        private const string jsonPaginationPath = "store.page.data.pagination";
        // TODO: redo this; blocks the ability to retrieve more than a single result page
        private JsonNode? scrapeData;
        // number of search result pages
        private uint pageCount;

        // values of URL parameter(s) denoting type of requested content
        private enum contentType
        {
            video = 100, // page with a youtube embed link
            tab = 200,
            chord = 300,
            bass = 400,
            pro_tab = 500, // inaccessible for us; paid
            power_rab = 600, // inaccessible for us; not plaintext
            drums = 700,
            ukulele = 800,
            official = 900, // inaccessible for us; paid
        }

        public SearchScraper() : base() { }

        public override void LoadData(string searchQuery)
        {
            var urlEncodedQuery = HttpUtility.UrlEncode(searchQuery);
            if (urlEncodedQuery is null)
                throw new ScraperException($"Couldn't process search query ({searchQuery})");

            var searchUrl = string.Format(searchMetaUrl, "title", urlEncodedQuery, 1);

            this.scrapeData = ScrapeUrl(searchUrl);
        }

        // return the results just as we got them from UG
        public JsonArray GetSearchResultsRaw()
        {
            if (this.scrapeData is null)
                throw new ScraperException("Scraper not properly initialized");

            var node = scrapeData.GetByPath(jsonSearchResultsPath);
            if (node is null)
                throw new ScraperException("Unable to find search results in the retrieved document");

            var results = node.AsArray();

            return results;
        }

        public JsonNode Dump()
        {
            if (scrapeData is null)
                throw new ScraperException("Scraper not correctly initialized");
            return scrapeData;
        }
    }
}
