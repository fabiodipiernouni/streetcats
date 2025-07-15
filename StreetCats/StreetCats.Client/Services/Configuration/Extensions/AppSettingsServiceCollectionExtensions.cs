using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Configuration.Implementations;
using StreetCats.Client.Models.Api;

namespace StreetCats.Client.Services.Configuration.Extensions;

/// <summary>
/// Estensioni per IServiceCollection per registrare AppSettings
/// </summary>
public static class AppSettingsServiceCollectionExtensions
{
    /// <summary>
    /// Registra AppSettings nel container DI
    /// </summary>
    public static IServiceCollection AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        // Configura e valida ApiConfiguration
        services.Configure<ApiConfiguration>(configuration.GetSection(ApiConfiguration.SectionName));

        // Registra AppSettings come singleton
        services.AddSingleton<IAppSettings>(serviceProvider =>
        {
            return new AppSettings(configuration);
        });

        return services;
    }
}