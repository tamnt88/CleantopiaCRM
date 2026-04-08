using System.Security.Cryptography;
using System.Text;

namespace CleantopiaCRM.Web.Services;

public static class PasswordHasher
{
    public static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
