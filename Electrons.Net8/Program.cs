
using Electrons.Net8;
using Electrons.Net8.Models;
using log4net.Repository.Hierarchy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
