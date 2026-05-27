using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenGoPro.Client.Generated
{
    public interface IOpenGoProHttpClient
    {
        Task<string> GetCameraNameAsync();
        Task<string> SetCameraNameAsync(string name);
        Task<JsonElement> GetCameraInfoAsync();
        Task<JsonElement> GetStateAsync();
        Task<JsonElement> GetLastMediaAsync();
        Task<JsonElement> ListMediaAsync();
        Task<Stream> DownloadMediaAsync(string directory, string filename);
        Task<string> FirmwareUpdateAsync(Stream firmwareStream, string fileName);
    }

    public class OpenGoProHttpClient : IOpenGoProHttpClient
    {
        private readonly HttpClient _http;

        public OpenGoProHttpClient(string openApiPath, HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetCameraNameAsync()
        {
            var res = await _http.GetAsync("/gopro/camera/name");
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> SetCameraNameAsync(string name)
        {
            var payload = JsonSerializer.Serialize(new { name = name });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res = await _http.PutAsync("/gopro/camera/name", content);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }

        public async Task<JsonElement> GetCameraInfoAsync()
        {
            var res = await _http.GetAsync("/gopro/camera/info");
            res.EnsureSuccessStatusCode();
            using var st = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(st);
            return doc.RootElement.Clone();
        }

        public async Task<JsonElement> GetStateAsync()
        {
            var res = await _http.GetAsync("/gopro/camera/state");
            res.EnsureSuccessStatusCode();
            using var st = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(st);
            return doc.RootElement.Clone();
        }

        public async Task<JsonElement> GetLastMediaAsync()
        {
            var res = await _http.GetAsync("/gopro/media/last_captured");
            res.EnsureSuccessStatusCode();
            using var st = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(st);
            return doc.RootElement.Clone();
        }

        public async Task<JsonElement> ListMediaAsync()
        {
            var res = await _http.GetAsync("/gopro/media/list");
            res.EnsureSuccessStatusCode();
            using var st = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(st);
            return doc.RootElement.Clone();
        }

        public async Task<Stream> DownloadMediaAsync(string directory, string filename)
        {
            var path = $"/videos/DCIM/{Uri.EscapeDataString(directory)}/{Uri.EscapeDataString(filename)}";
            var res = await _http.GetAsync(path);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStreamAsync();
        }

        public async Task<string> FirmwareUpdateAsync(Stream firmwareStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(firmwareStream);
            content.Add(streamContent, "file", fileName);
            var res = await _http.PostAsync("/gp/gpSoftUpdate", content);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }
    }
}
