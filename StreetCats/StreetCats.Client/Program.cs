using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using StreetCats.Client;
using StreetCats.Client.Services.Interfaces;
using StreetCats.Client.Services.Implementations;
using StreetCats.Client.Services.Http;
using StreetCats.Client.Services.Http.Handler;
using StreetCats.Client.Services.Exceptions;
using StreetCats.Client.Services.Exceptions.Interfaces;
using StreetCats.Client.Services.Exceptions.Implementations;
using StreetCats.Client.Services.Auth.Interfaces;
using StreetCats.Client.Services.Auth.Implementations;
using StreetCats.Client.Services.Configuration.Interfaces;
using StreetCats.Client.Services.Configuration.Implementations;
using static StreetCats.Client.Services.Configuration.Extensions.AppSettingsServiceCollectionExtensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// CONFIGURAZIONE APPLICAZIONE
Console.WriteLine("STREETCATS - Avvio configurazione servizi...");

// Registra configurazione strongly-typed
builder.Services.AddAppSettings(builder.Configuration);

// Ottieni configurazione per decidere quale implementazione usare
var config = builder.Configuration;
var useMockServices = config.GetValue<bool>("ApiSettings:UseMockServices", true);
var baseUrl = config.GetValue<string>("ApiSettings:BaseUrl", "https://localhost:3000/api");

Console.WriteLine($"Modalità: {(useMockServices ? "SVILUPPO (Mock)" : "PRODUZIONE (Real API)")}");
Console.WriteLine($"Base URL: {baseUrl}");

// HTTP CLIENT CONFIGURATION
// HttpClient base per servizi generali
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// SERVIZI DI SUPPORTO (sempre necessari)
Console.WriteLine("Registrando servizi di supporto...");

// Exception Handler
builder.Services.AddScoped<IApiExceptionHandler, ApiExceptionHandler>();

// Delegating Handlers per HTTP pipeline
builder.Services.AddScoped<LoggingDelegatingHandler>();
builder.Services.AddScoped<RetryDelegatingHandler>();

if (!useMockServices)
{
    // CONFIGURAZIONE PER PRODUZIONE - API REALI
    Console.WriteLine("Configurando servizi REALI per API REST...");

    // HttpClient per AuthService (NON autenticato)
    builder.Services.AddHttpClient("AuthClient", (serviceProvider, client) =>
    {
        var appSettings = serviceProvider.GetRequiredService<IAppSettings>();
        client.Timeout = appSettings.Api.GetTimeout();

        // Headers di default
        foreach (var header in appSettings.Api.DefaultHeaders)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    });

    // HttpClient per altri servizi (autenticato)
    builder.Services.AddHttpClient("StreetCatsApi", (serviceProvider, client) =>
    {
        var appSettings = serviceProvider.GetRequiredService<IAppSettings>();

        Console.WriteLine($"BaseUrl dal config: {appSettings.Api.BaseUrl}");

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

    // AuthenticatedHttpClient per API calls (escluso AuthService)
    builder.Services.AddScoped<IAuthenticatedHttpClient, AuthenticatedHttpClient>();

    // SERVIZI REALI - AuthService usa HttpClient normale
    builder.Services.AddScoped<IAuthService>(serviceProvider =>
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("AuthClient");

        return new AuthService(
            httpClient,
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IApiExceptionHandler>(),
            serviceProvider.GetRequiredService<IJSRuntime>(),
            serviceProvider.GetService<ILogger<AuthService>>()
        );
    });

    // Altri servizi usano AuthenticatedHttpClient
    builder.Services.AddScoped<ICatService, CatService>();
    builder.Services.AddScoped<IMapService, MapService>();

    Console.WriteLine("Servizi REALI registrati con successo");
}
else
{
    // CONFIGURAZIONE PER SVILUPPO - SERVIZI MOCK
    Console.WriteLine("Configurando servizi MOCK per sviluppo...");

    // Servizi mock per sviluppo e testing
    builder.Services.AddScoped<IAuthService, AuthServiceMock>();
    builder.Services.AddScoped<ICatService, CatServiceMock>();
    builder.Services.AddScoped<IMapService, MapServiceMock>();

    Console.WriteLine("Servizi MOCK registrati per sviluppo");
}

// AUTHORIZATION SERVICES
Console.WriteLine("Configurando sistema di autorizzazione...");

// TODO: Quando implementerai CustomAuthenticationStateProvider, decommenta questa sezione
/*
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
*/

// LOGGING CONFIGURATION
Console.WriteLine("Configurando logging...");

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Information);

    // In development, mostra più dettagli
    if (useMockServices)
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    }
});

// BUILD E AVVIO
Console.WriteLine("Building applicazione...");

var app = builder.Build();

// VERIFICA CONFIGURAZIONE
try
{
    var appSettings = app.Services.GetRequiredService<IAppSettings>();
    var validationResult = appSettings.ValidateConfiguration();

    if (!validationResult.IsValid)
    {
        Console.WriteLine("ERRORI CONFIGURAZIONE:");
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"   • {error}");
        }
        throw new InvalidOperationException("Configurazione non valida");
    }

    Console.WriteLine($"{validationResult.GetSummary()}");
}
catch (Exception ex)
{
    Console.WriteLine($"ERRORE CRITICO CONFIGURAZIONE: {ex.Message}");
    throw;
}

// STATISTICHE FINALI
Console.WriteLine("\nSTATISTICHE SERVIZI REGISTRATI:");
Console.WriteLine($"   • Modalità: {(useMockServices ? "MOCK (sviluppo)" : "REAL (produzione)")}");
Console.WriteLine($"   • Base URL: {baseUrl}");
Console.WriteLine($"   • Servizi totali: {builder.Services.Count}");
Console.WriteLine($"   • Timeout HTTP: {config.GetValue<int>("ApiSettings:TimeoutSeconds", 30)}s");
Console.WriteLine($"   • Max Retries: {config.GetValue<int>("ApiSettings:MaxRetries", 3)}");

Console.WriteLine("\nSTREETCATS pronto per l'avvio!");
Console.WriteLine("=".PadRight(50, '='));

await app.RunAsync();