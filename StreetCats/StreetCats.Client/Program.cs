using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StreetCats.Client;
using StreetCats.Client.Services.Interfaces;
using StreetCats.Client.Services.Implementation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient base
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// 🎯 LEGGI CONFIGURAZIONE DA appsettings.json
var config = builder.Configuration;
var useMockServices = config.GetValue<bool>("ApiSettings:UseMockServices", true);
var baseUrl = config.GetValue<string>("ApiSettings:BaseUrl", "https://localhost:3000/api");

Console.WriteLine($"🔧 STREETCATS Config:");
Console.WriteLine($"   • Modalità: {(useMockServices ? "SVILUPPO (Mock)" : "PRODUZIONE (Real)")}");
Console.WriteLine($"   • Base URL: {baseUrl}");

// 🏗️ REGISTRAZIONE SERVIZI CONFIGURABILE
if (useMockServices)
{
    // MODALITÀ SVILUPPO: Servizi mock
    Console.WriteLine("   • Registrando servizi MOCK per sviluppo");
    builder.Services.AddScoped<IAuthService, AuthServiceMock>();
    builder.Services.AddScoped<IMapService, MapServiceMock>();
    builder.Services.AddScoped<ICatService, CatServiceMock>();
}
else
{
    // MODALITÀ PRODUZIONE: Servizi reali
    Console.WriteLine("   • Registrando servizi REALI per produzione");
    builder.Services.AddScoped<IAuthService, AuthServiceMock>(); //cambiare con AuthService quando implementato
    builder.Services.AddScoped<IMapService, MapServiceMock>(); //cambiare con MapService quando implementato
    builder.Services.AddScoped<ICatService, CatServiceMock>(); //cambiare con CatService quando implementato
}

await builder.Build().RunAsync();