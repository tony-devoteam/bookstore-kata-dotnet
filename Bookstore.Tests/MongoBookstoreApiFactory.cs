using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Tests;

public sealed class MongoBookstoreApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var databaseName = $"bookstore_tests_{Guid.NewGuid():N}";
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MONGODB_URI"] = connectionString,
                ["MONGODB_DATABASE"] = databaseName
            });
        });
    }
}
