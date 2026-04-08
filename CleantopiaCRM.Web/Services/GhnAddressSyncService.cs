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

    public async Task SyncAsync()
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Token", _settings.Token);
        client.DefaultRequestHeaders.Add("ShopId", _settings.ShopId);

        var provincesDoc = await client.GetStringAsync($"{_settings.BaseUrl}/shiip/public-api/v3/master-data/province");
        using var provincesJson = JsonDocument.Parse(provincesDoc);
        var provinces = provincesJson.RootElement.GetProperty("data").EnumerateArray().ToList();

        foreach (var p in provinces)
        {
            var pId = p.GetProperty("ProvinceID").GetInt32();
            var pName = p.GetProperty("ProvinceName").GetString() ?? string.Empty;

            var province = await db.GhnProvinces.FirstOrDefaultAsync(x => x.ProvinceId == pId);
            if (province is null)
            {
                province = new GhnProvince { ProvinceId = pId, ProvinceName = pName, SyncedAt = DateTime.UtcNow };
                db.GhnProvinces.Add(province);
                await db.SaveChangesAsync();
            }
            else
            {
                province.ProvinceName = pName;
                province.SyncedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            var body = JsonSerializer.Serialize(new { province_id = pId, offset = 0, limit = 500 });
            var response = await client.PostAsync(
                $"{_settings.BaseUrl}/shiip/public-api/v3/master-data/ward/all-by-province-id",
                new StringContent(body, Encoding.UTF8, "application/json"));
            var wardDoc = await response.Content.ReadAsStringAsync();
            using var wardJson = JsonDocument.Parse(wardDoc);
            var wards = wardJson.RootElement.GetProperty("data").EnumerateArray().ToList();

            foreach (var w in wards)
            {
                var wardIdV2 = w.GetProperty("WardId").GetInt32();
                var wardCode = w.TryGetProperty("WardCode", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                var wardName = w.GetProperty("WardName").GetString() ?? string.Empty;
                var ward = await db.GhnWards.FirstOrDefaultAsync(x => x.WardIdV2 == wardIdV2);
                if (ward is null)
                {
                    db.GhnWards.Add(new GhnWard
                    {
                        WardIdV2 = wardIdV2,
                        WardCode = wardCode,
                        WardName = wardName,
                        ProvinceId = province.Id,
                        SyncedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    ward.WardCode = wardCode;
                    ward.WardName = wardName;
                    ward.ProvinceId = province.Id;
                    ward.SyncedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
