using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using LemonWriter.Infrastructure.Services;
using LemonWriter.Infrastructure.EventBus;
using LemonWriter.Infrastructure.Repositories;
using LemonWriter.Infrastructure.Services.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LemonWriter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LemonDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<LemonWriter.Domain.Interfaces.IUnitOfWork>(sp => sp.GetRequiredService<LemonDbContext>());

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        services.AddScoped<ILibraryQueryService, LibraryQueryService>();
        services.AddScoped<IPrivacyPreferenceService, PrivacyPreferenceService>();
        services.AddScoped<IStudioEntryService, StudioEntryService>();
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IStoryMetricsService, StoryMetricsService>();
        services.AddScoped<EpubExportService>();
        services.AddScoped<PdfExportService>();
        services.AddScoped<IExportService, CompositeExportService>();

        services.AddScoped<IEventBus, InMemoryEventBus>();

        return services;
    }
}
