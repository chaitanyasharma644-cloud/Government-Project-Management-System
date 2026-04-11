using GPMS.Data;
using GPMS.Services; // ✅ IMPORTANT (for PermissionService)
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// SERVICES
// ==============================

// Add MVC
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 REGISTER PERMISSION SERVICE (CRITICAL)
builder.Services.AddScoped<PermissionService>();

// Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        // 🔐 Secure cookies
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
    });

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // 🔐 Secure session cookies
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ==============================
// APP PIPELINE
// ==============================

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔐 HTTPS
app.UseHttpsRedirection();

// Static files
app.UseStaticFiles();

app.UseRouting();

// Session
app.UseSession();

// 🔐 Auth (ORDER MATTERS)
app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();