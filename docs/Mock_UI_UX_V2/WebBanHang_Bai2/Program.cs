using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Data;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;
using WebBanHang_Bai2.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Identity =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequireDigit           = false;
        opts.Password.RequiredLength         = 6;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Password.RequireUppercase       = false;
        opts.Password.RequireLowercase       = false;
        opts.SignIn.RequireConfirmedAccount  = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.LogoutPath       = "/Account/Logout";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.ExpireTimeSpan   = TimeSpan.FromDays(7);
    opt.SlidingExpiration = true;
    opt.Cookie.Name      = "TechStore.Auth";
});

// ===== Upload limit (100 MB) =====
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 104_857_600);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize      = 104_857_600);

// ===== MVC =====
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// ===== Repositories (Scoped — EF Core requires Scoped) =====
builder.Services.AddScoped<IProductRepository,  EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IOrderRepository,    EFOrderRepository>();
builder.Services.AddScoped<IReviewRepository,   EFReviewRepository>();

// ===== Session & Services =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout       = TimeSpan.FromHours(2);
    opt.Cookie.HttpOnly   = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.Name       = "TechStore.Session";
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<WishlistService>();
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();   // tắt để tránh ERR_CONNECTION_ABORTED trong dev
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ===== Seed DB =====
await DbSeeder.SeedAsync(app);

app.Run();
