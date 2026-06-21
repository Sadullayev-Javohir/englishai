namespace Application.Notifications.Ports;

/// <summary>
/// Resolves notification text from vetted Uzbek templates (docs/development-guide.md rule 11 - Uzbek
/// text is never free-generated). <c>{placeholder}</c> tokens are filled from the args.
/// </summary>
public interface INotificationTemplateProvider
{
    /// <summary>
    /// Returns the resolved text for a template code, or <c>null</c> when the code is
    /// unknown. Any <c>{key}</c> tokens are replaced with the matching arg value.
    /// </summary>
    string? Get(string code, IReadOnlyDictionary<string, string>? args = null);
}
