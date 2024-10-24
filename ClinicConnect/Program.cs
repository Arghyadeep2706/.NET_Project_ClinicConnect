using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using DNTCaptcha.Core;
using DNTCaptcha.Core.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Register DNTCaptcha services without encryption key
builder.Services.AddDNTCaptcha(options =>
{
    options.UseCookieStorageProvider()
           .ShowThousandsSeparators(false);
});

// Register the validator service
builder.Services.AddScoped<IDNTCaptchaValidatorService, DNTCaptchaValidatorService>();

//-----------------------------------------------------------------------------------------------------------

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Set minimum level to Debug or Error based on your preference
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day) // Rolling log file by day
    .Filter.ByIncludingOnly(evt => evt.Level == Serilog.Events.LogEventLevel.Error) // Filter to log only errors
    .CreateLogger();

builder.Host.UseSerilog();

//---------------------------------------------------------------------------------------------------------

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(c =>
    {
        c.LoginPath = "/Patient/Login";
        c.AccessDeniedPath = "/Patient/Login";
    });

//---------------------------------------------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Ensure session middleware is placed before authentication
app.UseAuthentication(); // Ensure authentication middleware is added
app.UseAuthorization();

// Define routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
