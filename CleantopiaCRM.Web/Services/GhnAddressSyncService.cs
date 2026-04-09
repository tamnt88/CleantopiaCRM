using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleantopiaCRM.Web.Services;

public class GhnAddressSyncService(AppDbContext db, IHttpClientFactory httpFactory, IOptions<GhnSettings> settings)
{
    private readonly GhnSettings _settings = settings.Value;

    private sealed record ProvinceDto(int ProvinceId, string ProvinceName);
    private sealed record WardDto(int WardIdV2, string WardCode, string WardName);

    public async Task SyncAsync()
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Token", _settings.Token);
        client.DefaultRequestHeaders.Add("ShopId", _settings.ShopId);

        var provinces = await LoadProvincesAsync(client);

        foreach (var p in provinces)
        {
            var province = await db.GhnProvinces.FirstOrDefaultAsync(x => x.ProvinceId == p.ProvinceId);
            if (province is null)
            {
                province = new GhnProvince { ProvinceId = p.ProvinceId, ProvinceName = p.ProvinceName, SyncedAt = DateTime.UtcNow };
                db.GhnProvinces.Add(province);
                await db.SaveChangesAsync();
            }
            else
            {
                province.ProvinceName = p.ProvinceName;
                province.SyncedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            var wards = await LoadWardsAsync(client, p.ProvinceId);
            foreach (var w in wards)
            {
                var ward = await db.GhnWards.FirstOrDefaultAsync(x => x.WardIdV2 == w.WardIdV2);
                if (ward is null)
                {
                    db.GhnWards.Add(new GhnWard
                    {
                        WardIdV2 = w.WardIdV2,
                        WardCode = w.WardCode,
                        WardName = w.WardName,
                        ProvinceId = province.Id,
                        SyncedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    ward.WardCode = w.WardCode;
                    ward.WardName = w.WardName;
                    ward.ProvinceId = province.Id;
                    ward.SyncedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
        }
    }

    private async Task<List<ProvinceDto>> LoadProvincesAsync(HttpClient client)
    {
        var v3 = $"{_settings.BaseUrl}/shiip/public-api/v3/master-data/province/all";
        var v2 = $"{_settings.BaseUrl}/shiip/public-api/v2/master-data/province";
        var v3Legacy = $"{_settings.BaseUrl}/shiip/public-api/v3/master-data/province";

        var response = await client.GetAsync(v3);
        if (response.StatusCode == HttpStatusCode.NotFound)
            response = await client.GetAsync(v3Legacy);
        if (response.StatusCode == HttpStatusCode.NotFound)
            response = await client.GetAsync(v2);

        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GHN province API failed: {(int)response.StatusCode} - {text}");

        using var doc = JsonDocument.Parse(text);
        var list = new List<ProvinceDto>();
        foreach (var e in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = GetInt(e, "_id", "ProvinceID", "ProvinceId");
            var name = GetString(e, "name", "Name", "ProvinceName");
            list.Add(new ProvinceDto(id, name));
        }

        return list;
    }

    private async Task<List<WardDto>> LoadWardsAsync(HttpClient client, int provinceId)
    {
        var body = JsonSerializer.Serialize(new { province_id = provinceId, offset = 0, limit = 500 });

        var v3 = $"{_settings.BaseUrl}/shiip/public-api/v3/master-data/ward/all-by-province-id";
        var v2 = $"{_settings.BaseUrl}/shiip/public-api/v2/master-data/ward";

        var response = await client.PostAsync(v3, new StringContent(body, Encoding.UTF8, "application/json"));
        if (response.StatusCode == HttpStatusCode.NotFound)
            response = await client.PostAsync(v2, new StringContent(body, Encoding.UTF8, "application/json"));

        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GHN ward API failed: {(int)response.StatusCode} - {text}");

        using var doc = JsonDocument.Parse(text);
        var list = new List<WardDto>();
        foreach (var e in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = GetInt(e, "_id", "WardId", "WardID");
            var code = GetString(e, "WardCode");
            if (string.IsNullOrWhiteSpace(code))
                code = id.ToString();
            var name = GetString(e, "name", "Name", "WardName");
            list.Add(new WardDto(id, code, name));
        }

        return list;
    }

    private static int GetInt(JsonElement element, params string[] names)
    {
        foreach (var n in names)
            if (element.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.Number)
                return p.GetInt32();

        throw new InvalidOperationException($"Missing int field: {string.Join("/", names)}");
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var n in names)
            if (element.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString() ?? string.Empty;

        return string.Empty;
    }
}
