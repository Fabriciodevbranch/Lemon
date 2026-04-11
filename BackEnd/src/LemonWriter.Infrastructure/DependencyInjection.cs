using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using LemonWriter.Infrastructure.EventBus;
using LemonWriter.Infrastructure.Repositories;
using LemonWriter.Infrastructure.Services.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LemonWriter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LemonDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lemon_writer.db"));

        services.AddScoped<LemonWriter.Domain.Interfaces.IUnitOfWork>(sp => sp.GetRequiredService<LemonDbContext>());

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IEventBus, InMemoryEventBus>();
        services.AddScoped<IExportService, CompositeExportService>();

        return services;
    }
}
