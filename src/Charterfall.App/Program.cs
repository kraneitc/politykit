using Charterfall.App.Components;
using Charterfall.App.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddScoped<ICharterfallSessionStore, InMemoryCharterfallSessionStore>();
builder.Services.AddSingleton<IPrototypeContentProvider, PrototypeContentProvider>();
builder.Services.AddSingleton<IPolityKitRunClient, PlaceholderPolityKitRunClient>();
builder.Services.AddSingleton<IPlayerMessageFormatter, PlayerMessageFormatter>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
