namespace MesTech.Application.DTOs;

public class XmlImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<XmlImportError> Errors { get; set; } = [];
}

public class XmlImportError
{
    public int Row { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
