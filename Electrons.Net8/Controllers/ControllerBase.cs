using Electrons.Core.Net8.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;


namespace Electrons.Net8.Controllers
{
    public class ControllerBase : Controller, IExceptionFilter, IAuthorizationFilter
    {
        protected Repository Repository;
        protected readonly ILogger Logger;
        protected IMemoryCache Cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected HttpContext CurrentContext => _httpContextAccessor.HttpContext;
        protected IWebHostEnvironment WebHostEnvironment { get; set; }
        protected GameSettings GameSettings { get; set; }
        public ControllerBase(NHibernate.ISession session, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IOptionsSnapshot<GameSettings> settings, ILogger logger)
        {
            _httpContextAccessor = httpContextAccessor;
            GameSettings = settings.Value;
            Logger = logger;
            WebHostEnvironment = env;
            var configPath = WebHostEnvironment.ContentRootPath;
            Cache = cache;
            Repository = new Repository(session, cache);
        }
        protected T GetSessionValue<T>(string key) where T : class
        {
            var json = _httpContextAccessor.HttpContext?.Session?.GetString(key);
            var output = json != null ? JsonSerializer.Deserialize<T>(json) : null;
            return output;
        }
        protected bool IsAdmin => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdmin") == "true";
        protected void SetSessionObject<T>(string key, T val)
        {
            var json = JsonSerializer.Serialize(val);
            _httpContextAccessor.HttpContext?.Session?.SetString(key, json);
        }

        void IExceptionFilter.OnException(ExceptionContext context)
        {
            var ex = context.Exception;
            Logger.LogError(ex.Message, ex);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ViewBag.Title = "Winnemac Electrons Baseball";
        }
    }
}
