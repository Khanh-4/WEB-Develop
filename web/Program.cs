using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;
using TechSpecs.Data;
using TechSpecs.Models;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Railway sets PORT; only override URL binding when explicitly set so local
// dev launch profiles (port 5003) are not clobbered.
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (railwayPort is not null)
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        // SameSite=None (default) requires Secure=true or Chrome rejects the cookie.
        // Force Secure regardless of detected scheme (Railway proxies HTTP internally).
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.Events.OnRemoteFailure = ctx =>
        {
            ctx.Response.Redirect("/Account/Login?error=google-login-failed");
            ctx.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Force auth session cookie to Secure in all environments (same reason as above)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<TechSpecs.Models.ApplicationUser>, TechSpecs.Services.AppUserClaimsPrincipalFactory>();
builder.Services.AddScoped<TechSpecs.Services.ICompatibilityEngine, TechSpecs.Services.CompatibilityEngine>();
builder.Services.AddScoped<TechSpecs.Services.IAIAssistantService, TechSpecs.Services.AIAssistantService>();
builder.Services.AddScoped<TechSpecs.Services.IEmailSender, TechSpecs.Services.ResendEmailSender>();
builder.Services.AddScoped<TechSpecs.Services.IMockDataService, TechSpecs.Services.MockDataService>();
builder.Services.AddHttpClient();

// Persist DataProtection keys to DB so sessions survive container restarts/redeploys
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<TechSpecs.Data.AppDbContext>();

// Trust Railway's reverse proxy for ForwardedHeaders
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // trust all upstream proxies
    options.KnownProxies.Clear();
});

builder.Services.AddMemoryCache();
builder.Services.AddOutputCache();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Use the ForwardedHeadersOptions configured in services (KnownNetworks/KnownProxies cleared)
app.UseForwardedHeaders();

// Railway terminates HTTPS at its proxy; only redirect in dev to avoid redirect loops
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();
app.UseStaticFiles();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("vi") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures     = supportedCultures,
    SupportedUICultures   = supportedCultures,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider(),
    }
});

app.UseRouting();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var maxCpu = await db.Cpus.MaxAsync(c => c.ApproximatePerformance);
    var maxGpu = await db.VideoCards.MaxAsync(c => c.ApproximatePerformance);
    Console.WriteLine($"MAX CPU PERF = {maxCpu}, MAX GPU PERF = {maxGpu}");
}

// SET ALL STOCK TO 1000
using (var scope2 = app.Services.CreateScope())
{
    var db2 = scope2.ServiceProvider.GetRequiredService<TechSpecs.Data.AppDbContext>();
    
    await db2.Cpus.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.Motherboards.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.Memories.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.VideoCards.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.Storages.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.PowerSupplies.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.CaseEnclosures.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
    await db2.CpuCoolers.ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, 1000));
}
app.Run();
