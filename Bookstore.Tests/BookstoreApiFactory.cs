using Bookstore.Api.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Bookstore.Tests;

public sealed class BookstoreApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _databaseIsHealthy;
    private readonly string _environmentName;

    public BookstoreApiFactory(bool databaseIsHealthy = true, string environmentName = "Development")
    {
        _databaseIsHealthy = databaseIsHealthy;
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBookRepository>();
            services.AddSingleton<IBookRepository>(_ => new InMemoryBookRepository(_databaseIsHealthy));
            services.RemoveAll<IBookIdGenerator>();
            services.AddSingleton<IBookIdGenerator, TestBookIdGenerator>();
            services.RemoveAll<IHostedService>();
        });
    }
}
