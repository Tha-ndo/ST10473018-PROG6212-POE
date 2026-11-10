using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid()}";

    public FakeFileStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            // Swap Azure Blob Storage for an in-memory fake.
            var storage = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
            if (storage is not null) services.Remove(storage);
            services.AddSingleton<IFileStorageService>(Storage);
        });
    }
}
