using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class DocumentFolderAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private DocFolderItemVm? selectedFolder;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<DocFolderItemVm> Folders { get; } = [];
    public ObservableCollection<DocFileItemVm> Files { get; } = [];

    public DocumentFolderAvaloniaViewModel(IMediator mediator)
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
            Folders.Clear();
            Files.Clear();

            Folders.Add(new DocFolderItemVm { Id = Guid.NewGuid(), Name = "Sozlesmeler", FileCount = 12, Icon = "F" });
            Folders.Add(new DocFolderItemVm { Id = Guid.NewGuid(), Name = "Teklifler", FileCount = 8, Icon = "F" });
            Folders.Add(new DocFolderItemVm { Id = Guid.NewGuid(), Name = "Faturalar", FileCount = 45, Icon = "F" });
            Folders.Add(new DocFolderItemVm { Id = Guid.NewGuid(), Name = "Raporlar", FileCount = 6, Icon = "F" });

            Files.Add(new DocFileItemVm { Id = Guid.NewGuid(), Name = "sozlesme_abc_ltd.pdf", Size = "245 KB", ModifiedAt = DateTime.Now.AddDays(-3), Type = "PDF" });
            Files.Add(new DocFileItemVm { Id = Guid.NewGuid(), Name = "teklif_xyz_as.docx", Size = "128 KB", ModifiedAt = DateTime.Now.AddDays(-5), Type = "DOCX" });
            Files.Add(new DocFileItemVm { Id = Guid.NewGuid(), Name = "fatura_2026_001.pdf", Size = "89 KB", ModifiedAt = DateTime.Now.AddDays(-1), Type = "PDF" });

            TotalCount = Folders.Count;
            IsEmpty = Folders.Count == 0 && Files.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Belge klasorleri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void Add()
    {
        // NAV: Navigate to folder create dialog
    }

    partial void OnSelectedFolderChanged(DocFolderItemVm? value)
    {
        // Load files for selected folder
        _ = LoadAsync();
    }
}

public class DocFolderItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public string Icon { get; set; } = "F";
}

public class DocFileItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public string Type { get; set; } = string.Empty;

    public string ModifiedDisplay => ModifiedAt.ToString("dd.MM.yyyy HH:mm");
}
