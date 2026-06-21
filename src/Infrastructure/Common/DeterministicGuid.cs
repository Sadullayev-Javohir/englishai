using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Common;

/// <summary>
/// Produces a stable <see cref="Guid"/> from a string key so seeded data keeps the
/// same identifiers across runs (and later, across database seeding). Not used for
/// security - only for reproducible ids.
/// </summary>
public static class DeterministicGuid
{
    public static Guid Create(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }
}
