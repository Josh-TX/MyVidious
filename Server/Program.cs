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
using System.Net;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.Configure<AppSettings>(builder.Configuration);
services.AddSingleton(sp =>
{
    var appsettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    AppSettings.Validate(appsettings);
    return appsettings; //this makes AppSettings injectable instead of just IOptions<AppSettings>
});
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
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return Task.CompletedTask;
        };
        
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
{

    options.UseNpgsql(builder.Configuration["ConnectionString"]);
    options.UseSnakeCaseNamingConvention();
#if DEBUG
    options.EnableSensitiveDataLogging();
#endif

});


services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(builder.Configuration["ConnectionString"]));
services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddScoped<InvidiousAPIAccess>();
services.AddSingleton<MeilisearchAccess>();
services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();
services.AddScoped<IPScopedCache>();
services.AddScoped<GlobalCache>();
services.AddScoped<AlgorithmAccess>();
services.AddScoped<ImageUrlUtility>();
services.AddScoped<AdminAccess>();

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
        trigger.ForJob(urlJobKey).WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromHours(2)).RepeatForever())
    );
    var channelVideoJobKey = JobKey.Create(nameof(ChannelVideoJob));

    quartz.AddJob<ChannelVideoJob>(channelVideoJobKey, options => options.DisallowConcurrentExecution());
    quartz.AddTrigger(trigger =>
        trigger.ForJob(channelVideoJobKey)
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(15))
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(60)).RepeatForever())
    );

    var playlistVideoJobKey = JobKey.Create(nameof(PlaylistVideoJob));
    quartz.AddJob<PlaylistVideoJob>(playlistVideoJobKey, options => options.DisallowConcurrentExecution());
    quartz.AddTrigger(trigger =>
    trigger.ForJob(playlistVideoJobKey)
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(30))
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(60)).RepeatForever())
    );


    var videoDetailsJobKey = JobKey.Create(nameof(VideoDetailsJob));
    quartz.AddJob<VideoDetailsJob>(videoDetailsJobKey, options => options.DisallowConcurrentExecution());
    quartz.AddTrigger(trigger =>
    trigger.ForJob(videoDetailsJobKey)
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(45))
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromHours(2)).RepeatForever())
    );
});
services.AddQuartzHostedService();

var app = builder.Build();

app.UseMiddleware<WebRequestExceptionMiddleware>();
app.MapControllers();
#if DEBUG
//app.UseDeveloperExceptionPage(); //This prevents the WebRequestExceptionMiddleware from working correctly
#endif
app.UseSwagger();
app.UseSwaggerUI(z => z.SwaggerEndpoint("/swagger/v1/swagger.json", "MyVidious API V1"));
app.MapReverseProxy();

app.Run();