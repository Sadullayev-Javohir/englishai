using System.Text;

namespace Infrastructure.Common;

/// <summary>
/// Deterministic 32-bit FNV-1a hash. Unlike <see cref="string.GetHashCode()"/>
/// (randomized per process on .NET Core) this is stable across runs, so the local
/// deterministic adapters produce reproducible results.
/// </summary>
public static class StableHash
{
    public static int Of(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }

        return (int)(hash & 0x7FFFFFFF);
    }
}
