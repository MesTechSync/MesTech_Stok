namespace MesTech.Application.Interfaces;

/// <summary>
/// Provides localized string resolution for the application layer.
/// Supports Turkish (tr) and English (en) cultures via .resx resource files.
/// NOT YET IMPLEMENTED — no DI registration exists. Injecting will cause runtime failure.
/// </summary>
[System.Obsolete("Not implemented — no DI registration. Will throw at runtime if injected. Planned for future sprint.")]
public interface ILocalizationService
{
    /// <summary>
    /// Gets the localized string for the specified resource key.
    /// </summary>
    /// <param name="key">The resource key (e.g., "Nav.Dashboard", "Finance.Revenue").</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    string Get(string key);

    /// <summary>
    /// Gets the localized string for the specified resource key and formats it with the given arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments to insert into the localized string.</param>
    /// <returns>The formatted localized string.</returns>
    string GetFormat(string key, params object[] args);

    /// <summary>
    /// Gets the current culture name (e.g., "tr", "en").
    /// </summary>
    string CurrentCulture { get; }

    /// <summary>
    /// Sets the active culture for localization.
    /// </summary>
    /// <param name="cultureName">The culture name to set (e.g., "tr", "en").</param>
    void SetCulture(string cultureName);
}
