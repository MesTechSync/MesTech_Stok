using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class DocumentManagerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedType;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<DocumentItemVm> Documents { get; } = [];
    public string[] TypeOptions { get; } = ["Tumu", "PDF", "DOCX", "XLSX", "PNG"];

    public DocumentManagerAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetDocumentsQuery(_currentUser.TenantId));
            Documents.Clear();
            foreach (var doc in result.Documents)
            {
                Documents.Add(new DocumentItemVm
                {
                    Id = doc.Id,
                    Name = doc.FileName,
                    Folder = "—",
                    Size = "—",
                    Type = doc.MimeType ?? "—",
                    UploadedBy = "—",
                    UploadedAt = DateTime.Now
                });
            }
            TotalCount = result.TotalCount;
            IsEmpty = Documents.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Belgeler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }

    [RelayCommand]
    private void Upload()
    {
        // NAV: Open file picker and upload document
    }
}

public class DocumentItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public string UploadedDisplay => UploadedAt.ToString("dd.MM.yyyy");
}
