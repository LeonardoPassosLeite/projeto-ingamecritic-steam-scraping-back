namespace SteamChartsAPI.Exceptions
{
    public class ScraperException : Exception
    {
        public ScraperException(string message) : base(message) { }

        public ScraperException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}