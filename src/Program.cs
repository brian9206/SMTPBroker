using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using SMTPBroker.Interfaces;
using SMTPBroker.Middlewares;
using SMTPBroker.Persistence;
using SMTPBroker.Services;
using SMTPBroker.Services.Forwarders;
using SMTPBroker.Utilities;
using SmtpServer.Authentication;
using SmtpServer.Storage;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(configuration.GetValue<string>("DataDir")))
    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    });

builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddHostedService<CleanExpiredMessageService>();

builder.Services.AddHostedService<SMTPService>();
builder.Services.AddTransient<IMessageStore, SMTPMessageStore>();
builder.Services.AddTransient<IUserAuthenticator, SMTPAuthenticator>();

builder.Services.AddTransient<IMessageForwarder, DiscordMessageForwarder>();
builder.Services.AddTransient<IMessageForwarder, TelegramMessageForwarder>();
builder.Services.AddTransient<MessageRouter>();

builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(Path.Combine(configuration.GetValue<string>("DataDir"), "_hangfire.db"),
        new SQLiteStorageOptions()));

builder.Services.AddHangfireServer();

builder.Services.AddSingleton<BasicAuthMiddleware>();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

var isWebAuthEnable = configuration.GetValue("Web:Auth", false);
if (isWebAuthEnable)
{
    app.UseMiddleware<BasicAuthMiddleware>();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment() || isWebAuthEnable)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new [] { new HangfireDashboardAuthorizationFilter() }
    });
    app.MapHangfireDashboard();
}

// check forwarder config
try
{
    
    var forwarderConfigs = await ForwarderConfigParser.Parse(configuration);;
    Log.Logger.Information(forwarderConfigs.Length + " forwarder(s) loaded.");
}
catch (Exception e)
{
    Log.Logger.Fatal("Unable to read forwarder config. " + e.Message);
    return 1;
}

// init DB context
var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = serviceScopeFactory.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

Console.WriteLine("App started.");
app.Run();

return 0;