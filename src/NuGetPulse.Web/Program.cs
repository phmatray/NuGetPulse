using NuGetPulse.Export;
using NuGetPulse.Graph;
using NuGetPulse.Persistence;
using NuGetPulse.Scanner;
using NuGetPulse.Security;
using NuGetPulse.Web.Components;
using NuGetPulse.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── NuGet API client ──────────────────────────────────────────────────────────
// HttpClient for NuGet API — 30s timeout, auto-decompress gzip (registration5-gz endpoints)
builder.Services.AddHttpClient<NuGetService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NuGetPulse/1.0 (+https://nugetpulse.garry-ai.cloud)");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// ── Package Scanner ───────────────────────────────────────────────────────────
builder.Services.AddNuGetPulseScanner();

// ── Vulnerability Scanner (OSV API) ──────────────────────────────────────────
builder.Services.AddNuGetPulseSecurity();

// ── Dependency Graph Builder ──────────────────────────────────────────────────
builder.Services.AddNuGetPulseGraph();

// ── Export Service (CSV / JSON) ───────────────────────────────────────────────
builder.Services.AddNuGetPulseExport();

// ── Persistence (EF Core SQLite — scan history) ───────────────────────────────
var dbPath = builder.Configuration.GetValue<string>("Database:Path")
             ?? Path.Combine(builder.Environment.ContentRootPath, "nugetpulse.db");
builder.Services.AddNuGetPulsePersistence(dbPath);

// ── Data Protection (persist keys for multi-pod deployments) ─────────────────
var keysPath = builder.Configuration.GetValue<string>("DataProtection:KeysPath")
              ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("NuGetPulse");

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure database exists (creates tables if not present)
await app.Services.MigrateNuGetPulseDbAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.MapHealthChecks("/health");
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
