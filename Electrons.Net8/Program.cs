
using Electrons.Core.Net8.Infrastructure;
using Electrons.Net8;
using Electrons.Net8.Models;
using log4net.Repository.Hierarchy;
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

var app = builder.Build();
app.Logger.LogInformation($"Current Environment: {app.Environment.EnvironmentName}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UsePathBase("/electronsnet8");
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapDefaultControllerRoute();


app.Run();
