using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGoPro.Client
{
    public class OpenApiHttpTransport : IProtoTransport, IDisposable
    {
        private readonly HttpClient _http;
        private readonly Dictionary<string, (string method, string path)> _ops = new();

        public OpenApiHttpTransport(string openApiPath, HttpClient? httpClient = null, string? baseAddress = null)
        {
            _http = httpClient ?? new HttpClient();
            if (!string.IsNullOrEmpty(baseAddress)) _http.BaseAddress = new Uri(baseAddress);
            var json = File.ReadAllText(openApiPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("paths", out var paths))
            {
                foreach (var prop in paths.EnumerateObject())
                {
                    var path = prop.Name;
                    foreach (var methodProp in prop.Value.EnumerateObject())
                    {
                        var method = methodProp.Name.ToUpper();
                        if (methodProp.Value.TryGetProperty("operationId", out var opidEl))
                        {
                            var opid = opidEl.GetString();
                            if (!string.IsNullOrEmpty(opid)) _ops[opid] = (method, path);
                        }
                    }
                }
            }
        }

        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(string operationId, TRequest request, CancellationToken cancellationToken = default)
        {
            if (!_ops.TryGetValue(operationId, out var entry)) throw new InvalidOperationException($"Operation '{operationId}' not found in OpenAPI.");
            var method = entry.method;
            var path = entry.path;

            if (request != null)
            {
                foreach (var prop in typeof(TRequest).GetProperties())
                {
                    var placeholder = "{" + prop.Name + "}";
                    if (path.Contains(placeholder))
                    {
                        var val = prop.GetValue(request)?.ToString();
                        path = path.Replace(placeholder, Uri.EscapeDataString(val ?? string.Empty));
                    }
                }
            }

            var requestUri = path;
            HttpRequestMessage msg = new(new HttpMethod(method), requestUri);

            if (method == "POST" || method == "PUT" || method == "PATCH")
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
                msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            else if (request != null)
            {
                var sb = new StringBuilder();
                bool first = true;
                foreach (var prop in typeof(TRequest).GetProperties())
                {
                    var placeholder = "{" + prop.Name + "}";
                    if (path.Contains(placeholder)) continue;
                    var val = prop.GetValue(request);
                    if (val == null) continue;
                    var sep = first ? "?" : "&";
                    sb.Append(sep).Append(Uri.EscapeDataString(prop.Name)).Append("=").Append(Uri.EscapeDataString(val.ToString()));
                    first = false;
                }
                requestUri = path + sb.ToString();
                msg.RequestUri = new Uri(_http.BaseAddress, requestUri);
            }

            var resp = await _http.SendAsync(msg, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var respStr = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (typeof(TResponse) == typeof(string))
            {
                return (TResponse)(object)respStr;
            }
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<TResponse>(respStr, options);
            return result!;
        }

        public void Dispose() => _http.Dispose();
    }
}
