using System;

namespace UGScraper
{
    public class ScraperException : Exception
    {
        protected ScraperException() : base() { }
        public ScraperException(string msg) : base(msg) { }
        public ScraperException(string msg, Exception innerException) : base(msg, innerException) { }
    }
}
