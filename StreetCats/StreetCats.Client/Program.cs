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

var config = builder.Configuration;
bool useMockServices = config.GetValue<bool>("ApiSettings:UseMockServices", true);

if (useMockServices)
{
    // MODALITÀ SVILUPPO: Usa servizi mock
    builder.Services.AddScoped<IAuthService, AuthServiceMock>();
    builder.Services.AddScoped<IMapService, MapServiceMock>();
    // builder.Services.AddScoped<ICatService, CatServiceMock>();  // Prossimo step
}
else
{
    // MODALITÀ PRODUZIONE: Usa servizi reali
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMapService, MapService>();
    // builder.Services.AddScoped<ICatService, CatService>();
}

await builder.Build().RunAsync();
