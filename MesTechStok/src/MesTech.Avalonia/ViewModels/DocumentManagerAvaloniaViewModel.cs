using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class DocumentManagerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedType;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<DocumentItemVm> Documents { get; } = [];
    public string[] TypeOptions { get; } = ["Tumu", "PDF", "DOCX", "XLSX", "PNG"];

    public DocumentManagerAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            Documents.Clear();
            Documents.Add(new DocumentItemVm { Id = Guid.NewGuid(), Name = "sozlesme_abc_ltd.pdf", Folder = "Sozlesmeler", Size = "245 KB", Type = "PDF", UploadedBy = "Fatih I.", UploadedAt = DateTime.Now.AddDays(-3) });
            Documents.Add(new DocumentItemVm { Id = Guid.NewGuid(), Name = "teklif_xyz_as.docx", Folder = "Teklifler", Size = "128 KB", Type = "DOCX", UploadedBy = "Mehmet C.", UploadedAt = DateTime.Now.AddDays(-5) });
            Documents.Add(new DocumentItemVm { Id = Guid.NewGuid(), Name = "fatura_2026_001.pdf", Folder = "Faturalar", Size = "89 KB", Type = "PDF", UploadedBy = "Ayse K.", UploadedAt = DateTime.Now.AddDays(-1) });
            Documents.Add(new DocumentItemVm { Id = Guid.NewGuid(), Name = "stok_raporu_mart.xlsx", Folder = "Raporlar", Size = "512 KB", Type = "XLSX", UploadedBy = "Ali K.", UploadedAt = DateTime.Now.AddDays(-2) });
            Documents.Add(new DocumentItemVm { Id = Guid.NewGuid(), Name = "logo_mestech.png", Folder = "Genel", Size = "34 KB", Type = "PNG", UploadedBy = "Fatih I.", UploadedAt = DateTime.Now.AddDays(-30) });
            TotalCount = Documents.Count;
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
