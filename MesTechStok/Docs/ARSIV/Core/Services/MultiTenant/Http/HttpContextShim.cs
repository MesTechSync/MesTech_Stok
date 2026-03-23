using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesTechStok.Core.Services.MultiTenant.Http
{
    // Minimal shim to allow Core library to compile without ASP.NET Core
    public class HttpContextShim
    {
        public HttpRequestShim Request { get; } = new();
        public HttpResponseShim Response { get; } = new();
        public System.Security.Claims.ClaimsPrincipal? User { get; set; }
    }

    public class HttpRequestShim
    {
        public HeaderDictionary Headers { get; } = new();
        public HostStringShim Host { get; } = new();
        public PathStringShim Path { get; } = new();
        public string Method { get; set; } = "GET";
        public QueryCollectionShim Query { get; } = new();
        public CookieCollectionShim Cookies { get; } = new();
    }

    public class HttpResponseShim
    {
        public int StatusCode { get; set; } = 200;
        public Task WriteAsync(string text) => Task.CompletedTask;
    }

    public class HeaderDictionary : Dictionary<string, string>
    {
        public new string this[string key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set => base[key] = value;
        }
    }

    public class HostStringShim
    {
        public string Host { get; set; } = "localhost";
    }

    public class PathStringShim
    {
        public string? Value { get; set; } = "/";
    }

    public class QueryCollectionShim : Dictionary<string, string>
    {
        public new string this[string key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set => base[key] = value;
        }
    }

    public class CookieCollectionShim : Dictionary<string, string>
    {
        public new string this[string key]
        {
            get => TryGetValue(key, out var value) ? value : string.Empty;
            set => base[key] = value;
        }
    }
}


