namespace Integration.Tests.Llm;

/// <summary>
/// Clears a set of environment variables for the lifetime of a `using` block and restores their prior
/// values on dispose. <see cref="Infrastructure.Llm.HermesGatewayOptions"/> resolves its API key from
/// well-known process env vars (HERMES_GATEWAY_API_KEY, HermesGateway__ApiKey) as a fallback, which may
/// legitimately be set on a developer's machine - "unconfigured" tests need this to stay deterministic
/// regardless of ambient env.
/// </summary>
internal sealed class EnvVarGuard : IDisposable
{
    private readonly Dictionary<string, string?> _original = new();

    private EnvVarGuard(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _original[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    public static EnvVarGuard Clear(params string[] names) => new(names);

    public void Dispose()
    {
        foreach (var (name, value) in _original)
            Environment.SetEnvironmentVariable(name, value);
    }
}
