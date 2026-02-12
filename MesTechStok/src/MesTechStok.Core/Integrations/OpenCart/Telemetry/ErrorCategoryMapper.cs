using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace MesTechStok.Core.Integrations.OpenCart.Telemetry
{
    internal static class ErrorCategoryMapper
    {
        public static OpenCartErrorCategory From(HttpResponseMessage? resp, Exception? ex)
        {
            if (ex != null)
            {
                if (ex is OperationCanceledException || ex is TaskCanceledException) return OpenCartErrorCategory.Timeout;
                if (ex is HttpRequestException hre)
                {
                    if (hre.InnerException is SocketException) return OpenCartErrorCategory.Network;
                }
            }

            if (resp != null)
            {
                var code = (int)resp.StatusCode;
                return resp.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => OpenCartErrorCategory.Auth,
                    HttpStatusCode.NotFound => OpenCartErrorCategory.NotFound,
                    (HttpStatusCode)429 => OpenCartErrorCategory.RateLimit,
                    HttpStatusCode.RequestTimeout => OpenCartErrorCategory.Timeout,
                    HttpStatusCode.BadRequest or (HttpStatusCode)422 => OpenCartErrorCategory.Validation,
                    _ when code >= 500 => OpenCartErrorCategory.Transient,
                    _ => OpenCartErrorCategory.None
                };
            }

            return ex != null ? OpenCartErrorCategory.Unknown : OpenCartErrorCategory.None;
        }
    }
}
