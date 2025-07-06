using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StreetCats.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient per future API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

bool useMockServices = true; // TODO: Da configuration o environment

if (useMockServices)
{
    // MODALIT� SVILUPPO: Usa servizi mock
    builder.Services.AddScoped<IAuthService, AuthServiceMock>();
    builder.Services.AddScoped<IMapService, MapServiceMock>();
    // builder.Services.AddScoped<ICatService, CatServiceMock>();  // Prossimo step
}
else
{
    // MODALIT� PRODUZIONE: Usa servizi reali
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMapService, MapService>();
    // builder.Services.AddScoped<ICatService, CatService>();
}

await builder.Build().RunAsync();
