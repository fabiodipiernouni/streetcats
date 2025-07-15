using StreetCats.Client.Models.Api;
using StreetCats.Client.Services.Auth.Interfaces;
using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Auth.Implementations;
using StreetCats.Client.Services.Http.Handler;


namespace StreetCats.Services.Http.Extensions;

/// <summary>
/// Estensioni per la registrazione nel DI container
/// </summary>
public static class AuthenticatedHttpClientExtensions
{
    /// <summary>
    /// Registra AuthenticatedHttpClient nel container DI
    /// </summary>
    public static IServiceCollection AddAuthenticatedHttpClient(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticatedHttpClient, AuthenticatedHttpClient>();
        return services;
    }

    /// <summary>
    /// Configura HttpClient named per API autenticate
    /// </summary>
    public static IServiceCollection AddAuthenticatedApiClient(
        this IServiceCollection services,
        string name = "StreetCatsApi")
    {
        services.AddHttpClient(name, (serviceProvider, client) =>
        {
            var appSettings = serviceProvider.GetRequiredService<IAppSettings>();

            // Configura client
            client.BaseAddress = new Uri(appSettings.Api.BaseUrl);
            client.Timeout = appSettings.Api.GetTimeout();

            // Headers di default
            foreach (var header in appSettings.Api.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        })
        .AddHttpMessageHandler<RetryDelegatingHandler>()
        .AddHttpMessageHandler<LoggingDelegatingHandler>();

        return services;
    }
}