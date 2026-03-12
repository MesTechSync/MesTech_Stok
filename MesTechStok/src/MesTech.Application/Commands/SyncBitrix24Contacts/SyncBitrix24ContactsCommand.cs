using MediatR;

namespace MesTech.Application.Commands.SyncBitrix24Contacts;

public record SyncBitrix24ContactsCommand : IRequest<SyncBitrix24ContactsResult>;

public class SyncBitrix24ContactsResult
{
    public bool IsSuccess { get; set; }
    public int SyncedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
