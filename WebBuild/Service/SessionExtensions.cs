using System.Text.Json;

namespace WebBuild.Service;
public static class SessionExtensions
{
    public static void SetJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
    public static void SetLong(this ISession session, string key, long value)
    {
        session.Set(key, BitConverter.GetBytes(value));
    }

    public static long? GetLong(this ISession session, string key)
    {
        var bytes = session.Get(key);
        if (bytes == null || bytes.Length == 0) return null;
        return BitConverter.ToInt64(bytes, 0);
    }
}
 