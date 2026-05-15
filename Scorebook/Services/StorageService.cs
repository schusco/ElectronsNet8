using ScoreboardApi.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scorebook.Services
{
    public class LocalStorageService : IAuthStorageService
    {
        public bool ClearToken()
        {
            _apiToken = null;
            _refreshToken = null;
            return true;
        }

        public async Task<string?> GetRefreshTokenAsync() => await Task.FromResult(_refreshToken);

        public async Task<string?> GetTokenAsync() => await Task.FromResult(_apiToken);

        public Task SaveRefreshTokenAsync(string token)
        {
            _refreshToken = token;
            return Task.CompletedTask;
        }

        public Task SaveTokenAsync(string token)
        {
            _apiToken = token;
            return Task.CompletedTask;
        }
        private string? _apiToken;
        private string? _refreshToken;
    }
}
