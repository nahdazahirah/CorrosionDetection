using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using CorrosionDetectionBlazor;
using CorrosionDetectionBlazor.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient dengan auto-attach token JWT
builder.Services.AddScoped<AuthorizedMessageHandler>();
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri("https://localhost:7136/"))
    .AddHttpMessageHandler<AuthorizedMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// Autentikasi
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();