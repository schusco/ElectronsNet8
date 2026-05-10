using Microsoft.AspNetCore.StaticFiles;
using Electrons.Core.Net8.Infrastructure;
using Electrons.Net8;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NHibernate;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<GameSettings>(builder.Configuration.GetSection(nameof(GameSettings)));
string pwd = builder.Configuration["GameSettings:DefaultConnection:Password"];
if (string.IsNullOrEmpty(pwd))
{
    Console.WriteLine("CRITICAL: Database Password is missing!");
}
builder.Services.AddDistributedMemoryCache(); // Required for Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set your timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Required for GDPR/compliance logic
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var config = builder.Configuration.GetSection("GameSettings").Get<GameSettings>();
var helper = new NHibernateHelper(config.DefaultConnection);
builder.Services.AddSingleton(NHibernateHelper.SessionFactory);
builder.Services.AddScoped(f =>
{
    var factory = f.GetRequiredService<ISessionFactory>();
    var session = factory.OpenSession();
    session.CreateSQLQuery($"SET SESSION sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''));").ExecuteUpdate();
    return session;
});
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("https://18.218.101.1", "https://localhost:7046")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
var app = builder.Build();
var provider = new FileExtensionContentTypeProvider();

// 2. Add the .apk mapping
provider.Mappings[".apk"] = "application/vnd.android.package-archive";

// 3. Tell the app to use this provider
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.Logger.LogInformation($"Current Environment: {app.Environment.EnvironmentName}");
app.UseCors("SignalRPolicy");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UsePathBase("/electronsnet8");
    app.UseDeveloperExceptionPage();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapDefaultControllerRoute();


app.Run();
