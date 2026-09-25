using Document.Repository.Data;
using Document.Repository.Services;
using Document.Repository.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with strong password requirements
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.ValueLengthLimit = int.MaxValue;
});

// Add antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add memory cache for performance
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(options =>
{
    // Add global anti-forgery filter
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddTransient<IEmailSender, Document.Repository.Services.NoOpEmailSender>();

// Configure for reverse proxy (Azure/IIS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Use forwarded headers for reverse proxy
app.UseForwardedHeaders();

// Add rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Add global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    
    // Content Security Policy
    if (!context.Request.Path.StartsWithSegments("/Identity"))
    {
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");
    }
    
    await next();
});

app.UseStaticFiles();

// Seeding Roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occurred seeding roles: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseResponseCaching();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "Profile",
    pattern: "{controller=Admin}/{action=Profile}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "Listing",
    pattern: "{controller=Home}/{action=Listing}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "Notice",
    pattern: "{controller=Home}/{action=Notice}")
    .WithStaticAssets();




app.MapRazorPages();

app.Run();
