using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Document Manager ViewModel — wired to GetDocumentsQuery via MediatR.
/// </summary>
public partial class DocumentsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<DocumentListDto> documents = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false; // newest first

    private List<DocumentListDto> _allDocuments = [];

    public DocumentsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Belgeler";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetDocumentsQuery(_currentUser.TenantId), ct);

            _allDocuments = result.Documents.ToList();
            ApplyFilter();
        }, "Belgeler yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allDocuments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(d =>
                d.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (d.MimeType ?? string.Empty).Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (d.FolderName ?? string.Empty).Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        var sorted = SortColumn switch
        {
            "FileName"   => SortAscending ? filtered.OrderBy(x => x.FileName).ToList()   : filtered.OrderByDescending(x => x.FileName).ToList(),
            "MimeType"   => SortAscending ? filtered.OrderBy(x => x.MimeType).ToList()   : filtered.OrderByDescending(x => x.MimeType).ToList(),
            "FolderName" => SortAscending ? filtered.OrderBy(x => x.FolderName).ToList() : filtered.OrderByDescending(x => x.FolderName).ToList(),
            _            => SortAscending ? filtered.OrderBy(x => x.FileName).ToList()   : filtered.OrderByDescending(x => x.FileName).ToList(),
        };

        Documents = new ObservableCollection<DocumentListDto>(sorted);
        TotalCount = Documents.Count;
        IsEmpty = TotalCount == 0;
        Summary = $"Toplam {TotalCount} belge";
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "documents", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Belgeler disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Upload()
    {
        Summary = "Belge yukleme modulu hazirlaniyor...";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
