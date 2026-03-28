using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Document Manager ViewModel — wired to GetDocumentsQuery via MediatR.
/// </summary>
public partial class DocumentsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private ObservableCollection<DocumentListDto> documents = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;

    public DocumentsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Belgeler";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetDocumentsQuery(_currentUser.TenantId), ct);

            Documents = new ObservableCollection<DocumentListDto>(result.Documents);
            TotalCount = result.TotalCount;
            IsEmpty = TotalCount == 0;
            Summary = $"Toplam {TotalCount} belge";
        }, "Belgeler yuklenirken hata");
    }

    [RelayCommand]
    private async Task Upload()
    {
        // TODO: File picker + upload via MediatR command
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
