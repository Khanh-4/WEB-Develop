using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

var builder = WebApplication.CreateBuilder(args);

// Railway sets PORT; bind on all interfaces so the container is reachable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

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
            var msg = Uri.EscapeDataString(ctx.Failure?.Message ?? "unknown");
            ctx.Response.Redirect($"/Account/Login?error={msg}");
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

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Use the ForwardedHeadersOptions configured in services (KnownNetworks/KnownProxies cleared)
app.UseForwardedHeaders();

// Temporary: verify ForwardedHeaders + DataProtection are working on Railway
app.MapGet("/debug/info", (HttpContext ctx, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dp) =>
{
    var protector = dp.CreateProtector("diag");
    var token = protector.Protect("ok");
    var roundtrip = protector.Unprotect(token) == "ok";
    return Results.Ok(new
    {
        scheme = ctx.Request.Scheme,
        isHttps = ctx.Request.IsHttps,
        host = ctx.Request.Host.ToString(),
        dpRoundtrip = roundtrip,
        forwardedProto = ctx.Request.Headers["X-Forwarded-Proto"].ToString(),
    });
});

// Railway terminates HTTPS at its proxy; only redirect in dev to avoid redirect loops
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

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

app.Run();
