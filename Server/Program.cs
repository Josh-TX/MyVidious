using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyVidious.Access;
using MyVidious.Background;
using MyVidious.Data;
using MyVidious.Utilities;
using Quartz;
using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.Configure<AppSettings>(builder.Configuration);
services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value); //make AppSettings injectable instead of just IOptions<AppSettings>
services.AddControllers();
services.AddHttpClient().ConfigureHttpClientDefaults((builder) =>
{
    builder.ConfigurePrimaryHttpMessageHandler(proovider =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        return handler;
    });
});

services.AddIdentityCore<IdentityUser>(options => {
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();


services.AddAuthentication(defaultScheme: CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events.OnRedirectToAccessDenied = ReplaceRedirector(HttpStatusCode.Forbidden, options.Events.OnRedirectToAccessDenied);
        options.Events.OnRedirectToLogin = ReplaceRedirector(HttpStatusCode.Unauthorized, options.Events.OnRedirectToLogin);
    });
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(7);
});


services.AddDbContext<VideoDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration["ConnectionString"])
        .UseSnakeCaseNamingConvention()
);
Console.WriteLine("CURRENT DIRECTORY");
Console.WriteLine(Directory.GetCurrentDirectory());


services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(builder.Configuration["ConnectionString"]));
services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddSingleton<InvidiousAPIAccess>();
services.AddSingleton<MeilisearchAccess>();
services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();
services.AddScoped<IPScopedCache>();
services.AddScoped<GlobalCache>();
services.AddScoped<AlgorithmAccess>();
services.AddScoped<ImageUrlUtility>();

services.AddSingleton<CustomProxyConfigProvider>();
services.AddSingleton<IProxyConfigProvider>((provider) => provider.GetRequiredService<CustomProxyConfigProvider>());
services.AddReverseProxy();

services.AddRazorPages();
services.AddSwaggerGen(options =>
{
    //not sure why this isn't being done automatically 
    options.CustomOperationIds(e => $"{e.ActionDescriptor.RouteValues["action"]}");
    options.EnableAnnotations();
});

services.AddSingleton<InvidiousUrlsAccess>();

services.AddQuartz(quartz =>
{
    var urlJobKey = JobKey.Create(nameof(InvidiousUrlsAccess));
    quartz.AddJob<InvidiousUrlsAccess>(urlJobKey);
    quartz.AddTrigger(trigger => 
        trigger.ForJob(urlJobKey).WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromSeconds(60)).RepeatForever())
    );
    var videoFetchJobKey = JobKey.Create(nameof(VideoFetchJob));
    quartz.AddJob<VideoFetchJob>(videoFetchJobKey, options => options.DisallowConcurrentExecution());
    quartz.AddTrigger(trigger =>
        trigger.ForJob(videoFetchJobKey)
        .StartAt(DateTimeOffset.UtcNow.AddSeconds(10))
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromSeconds(60)).RepeatForever())
    );
});
services.AddQuartzHostedService();

var app = builder.Build();


app.MapControllers();
app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(z => z.SwaggerEndpoint("/swagger/v1/swagger.json", "MyVidious API V1"));
app.MapReverseProxy();

app.Run();


static Func<RedirectContext<CookieAuthenticationOptions>, Task> ReplaceRedirector(
    HttpStatusCode statusCode,
    Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector) => context =>
    {
        if (context.Request.Path.StartsWithSegments("/account"))
        {
            context.Response.StatusCode = (int)statusCode;
            return Task.CompletedTask;
        }
        return existingRedirector(context);
    };


public class DebugOutputTraceListener : TraceListener
{
    public override void Write(string message)
    {
        Debug.Write(message);
    }

    public override void WriteLine(string message)
    {
        Debug.WriteLine(message);
    }
}
