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
builder.Services.AddHttpClient<IPolityKitRunClient, HttpPolityKitRunClient>(client =>
{
    var baseUrl = builder.Configuration["PolityKitApi:BaseUrl"] ?? "http://localhost:5020";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
});
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
