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
    });

builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<TechSpecs.Models.ApplicationUser>, TechSpecs.Services.AppUserClaimsPrincipalFactory>();
builder.Services.AddScoped<TechSpecs.Services.ICompatibilityEngine, TechSpecs.Services.CompatibilityEngine>();
builder.Services.AddScoped<TechSpecs.Services.IAIAssistantService, TechSpecs.Services.AIAssistantService>();
builder.Services.AddScoped<TechSpecs.Services.IEmailSender, TechSpecs.Services.ResendEmailSender>();
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Trust Railway's reverse proxy so OAuth callbacks and cookies use https://
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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
