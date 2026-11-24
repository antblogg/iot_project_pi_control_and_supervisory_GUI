using System.Threading.RateLimiting;
using AspControllApp.Options;
using AspControllApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Secrets/config: JSON in secrets/ (preferred when JSON has been used thus far).
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "secrets", "mongodb.json"),
    optional: true,
    reloadOnChange: true);
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "secrets", "security.json"),
    optional: true,
    reloadOnChange: true);

// Optional: load a MongoDB-generated .env file from secrets/ and map known keys.
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, "secrets", ".env");
if (File.Exists(envFilePath))
{
    var envValues = new Dictionary<string, string?>();
    foreach (var rawLine in File.ReadAllLines(envFilePath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"', '\'');
        envValues[key] = value;
    }

    if (envValues.TryGetValue("MONGODB_URI", out var uri))
    {
        envValues["MongoDb:ConnectionString"] = uri;
    }

    if (envValues.TryGetValue("MONGODB_DATABASE", out var database))
    {
        envValues["MongoDb:DatabaseName"] = database;
    }

    if (envValues.TryGetValue("MONGODB_COLLECTION", out var collection))
    {
        envValues["MongoDb:TemperatureCollectionName"] = collection;
    }

    if (envValues.TryGetValue("APP_ACCESS_PASSWORD", out var accessPassword))
    {
        envValues["Security:AccessPassword"] = accessPassword;
    }

    builder.Configuration.AddInMemoryCollection(envValues);
}

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.AddSingleton<MongoConnectionManager>();
builder.Services.AddSingleton<TemperatureAnalyticsService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("connect", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

