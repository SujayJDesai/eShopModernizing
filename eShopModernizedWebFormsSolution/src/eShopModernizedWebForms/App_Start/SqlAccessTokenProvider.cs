using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace eShopModernizedWebForms
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }

    public class ManagedIdentitySqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        private readonly DefaultAzureCredential _credential;

        public ManagedIdentitySqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CatalogDBContext")
                ?? throw new InvalidOperationException("Connection string 'CatalogDBContext' not found.");
            _credential = new DefaultAzureCredential();
        }

        public SqlConnection CreateConnection()
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var accessToken = _credential.GetToken(tokenRequestContext, default);

            return new SqlConnection
            {
                AccessToken = accessToken.Token,
                ConnectionString = _connectionString
            };
        }
    }

    public class AppSettingsSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public AppSettingsSqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CatalogDBContext")
                ?? throw new InvalidOperationException("Connection string 'CatalogDBContext' not found.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection
            {
                ConnectionString = _connectionString
            };
        }
    }
}
