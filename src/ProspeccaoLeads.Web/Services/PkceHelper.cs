using System.Security.Cryptography;
using System.Text;

namespace ProspeccaoLeads.Web.Services;

public static class PkceHelper
{
    public static string CreateVerifier()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    public static string CreateChallenge(string verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);
        return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
