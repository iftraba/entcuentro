using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Entcuentro.Application.Interfaces;
using Entcuentro.Application.Repositories;
using Entcuentro.UI.Auth;
using Entcuentro.Web;
using Entcuentro.Web.Auth;
using Entcuentro.Web.Data;
using Entcuentro.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001";

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<ITokenManager>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

builder.Services.AddScoped<ISyncService, WebSyncService>();
builder.Services.AddScoped(typeof(IOfflineRepository<>), typeof(IndexedDbRepository<>));
builder.Services.AddScoped(typeof(IEntityRepository<>), typeof(CachedRepository<>));

await builder.Build().RunAsync();
