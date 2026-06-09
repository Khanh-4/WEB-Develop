using System.Text.Json;

namespace WebBanHang_Bai2.Services;

/// <summary>Lưu / đọc object dưới dạng JSON trong Session.</summary>
public static class SessionExtensions
{
    public static void SetObject(this ISession session, string key, object value)
        => session.SetString(key, JsonSerializer.Serialize(value));

    public static T? GetObject<T>(this ISession session, string key)
    {
        var raw = session.GetString(key);
        return raw is null ? default : JsonSerializer.Deserialize<T>(raw);
    }
}
