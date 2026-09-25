using Document.Repository.Data;
using Document.Repository.Services;
using Document.Repository.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

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

    // In Development, IEmailSender is a no-op, so requiring confirmation would leave
    // every newly registered account unable to ever sign in.
    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();
    options.SignIn.RequireConfirmedEmail = !builder.Environment.IsDevelopment();
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

// Rate limiting, per client IP. Replaces a hand-rolled middleware that
// full-scanned its tracking dictionary on every request. The old limit of
// 100/min was only ever calibrated against a counter that also counted
// every CSS/JS/font request, so 100 requests was really ~9 page views.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("per-ip", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Use forwarded headers for reverse proxy
app.UseForwardedHeaders();

// Add global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    // SAMEORIGIN, not DENY: the in-app PDF viewers (the #pdfModal iframe and
    // the notice <embed>) frame our own PDFs. DENY - and frame-ancestors
    // 'none' below - made the browser refuse to render them, so the modal
    // opened blank. Same-origin framing still blocks clickjacking.
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    
    // Content Security Policy
    if (!context.Request.Path.StartsWithSegments("/Identity"))
    {
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'self';");
    }
    
    await next();
});

// MapStaticAssets() serves from the build-time static assets manifest, not by
// scanning the web root at runtime. Files written by FileService at upload time
// have no manifest entry, so every newly uploaded PDF 404'd. Serve the four
// runtime upload folders off disk instead - scoped to those folders so /lib,
// /css and the fingerprinted assets still fall through to MapStaticAssets, and
// registered after UseResponseCompression() so they stay compressed.

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

// Uploads, notices, slider images and user avatars. These are the only folders
// written at runtime, and MapStaticAssets() knows nothing about them until the
// next build. Placed before UseRouting() so a large PDF fetch short-circuits
// ahead of the per-IP rate limit rather than spending a page view.
foreach (var uploadFolder in new[] { "uploads", "notice", "SliderImages", "images/user" })
{
    var folderPath = Path.Combine(app.Environment.WebRootPath, uploadFolder);
    if (Directory.Exists(folderPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(folderPath),
            RequestPath = "/" + uploadFolder
        });
    }
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// Static assets are fingerprinted and served straight from the build
// output, so they are excluded from the per-IP budget - otherwise a
// single page view spends 6-7 of the 300 permitted requests on CSS,
// JS and fonts.
app.MapStaticAssets().DisableRateLimiting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets()
    .RequireRateLimiting("per-ip");

app.MapControllerRoute(
    name: "Profile",
    pattern: "{controller=Admin}/{action=Profile}")
    .WithStaticAssets()
    .RequireRateLimiting("per-ip");

app.MapControllerRoute(
    name: "Listing",
    pattern: "{controller=Home}/{action=Listing}")
    .WithStaticAssets()
    .RequireRateLimiting("per-ip");

app.MapControllerRoute(
    name: "Notice",
    pattern: "{controller=Home}/{action=Notice}")
    .WithStaticAssets()
    .RequireRateLimiting("per-ip");




app.MapRazorPages().RequireRateLimiting("per-ip");

app.Run();
