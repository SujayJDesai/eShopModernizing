using Microsoft.Extensions.Configuration;

namespace eShopWCFService.Models.Infrastructure
{
    /// <summary>
    /// Configuration helper for catalog database connection.
    /// Formerly used ConfigurationManager / Environment variable lookup;
    /// now wraps IConfiguration from ASP.NET Core DI.
    /// </summary>
    public class CatalogConfiguration
    {
        private readonly IConfiguration _configuration;

        public CatalogConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ConnectionString =>
            Environment.GetEnvironmentVariable("ConnectionString")
            ?? _configuration.GetConnectionString("EntityModel")
            ?? string.Empty;
    }
}
