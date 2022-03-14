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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddHostedService<CleanExpiredMessageService>();

builder.Services.AddHostedService<SMTPService>();
builder.Services.AddTransient<IMessageStore, SMTPMessageStore>();
builder.Services.AddTransient<IUserAuthenticator, SMTPAuthenticator>();

builder.Services.AddTransient<IMessageForwarder, DiscordMessageForwarder>();
builder.Services.AddTransient<IMessageForwarder, TelegramMessageForwarder>();
builder.Services.AddTransient<MessageForwarderFactory>();

builder.Services.AddSingleton<BasicAuthMiddleware>();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var app = builder.Build();
var configuration = app.Services.GetRequiredService<IConfiguration>();

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

if (configuration.GetValue("Web:Auth", false))
{
    app.UseMiddleware<BasicAuthMiddleware>();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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

app.Run();

return 0;