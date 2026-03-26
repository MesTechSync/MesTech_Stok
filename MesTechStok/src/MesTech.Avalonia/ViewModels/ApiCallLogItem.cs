namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF013 — Trendyol API çağrı log kaydı.
/// DataGrid için lightweight DTO.
/// </summary>
public sealed record ApiCallLogItem(
    string Zaman,
    string Endpoint,
    string Durum,
    string Sure);
