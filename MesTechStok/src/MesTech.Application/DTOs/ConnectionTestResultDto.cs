namespace MesTech.Application.DTOs;

/// <summary>
/// Connection Test Result data transfer object.
/// </summary>
public class ConnectionTestResultDto
{
    public bool IsSuccess { get; set; }
    public string PlatformCode { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public int? ProductCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int? HttpStatusCode { get; set; }
    public TimeSpan ResponseTime { get; set; }
}
