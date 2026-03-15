using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Documents;

public partial class DocumentManagerViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ObservableCollection<DocumentFolderVm> Folders { get; } = [];
    public ObservableCollection<DocumentItemVm> CurrentDocuments { get; } = [];

    [ObservableProperty] private DocumentFolderVm? selectedFolder;
    [ObservableProperty] private bool isLoading;

    public DocumentManagerViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Folders.Clear();
            Folders.Add(new DocumentFolderVm { Id = Guid.NewGuid(), Name = "Siparişler",       Icon = "PackageVariant",      DocumentCount = 0 });
            Folders.Add(new DocumentFolderVm { Id = Guid.NewGuid(), Name = "Faturalar",         Icon = "ReceiptText",         DocumentCount = 0 });
            Folders.Add(new DocumentFolderVm { Id = Guid.NewGuid(), Name = "Ürün Görselleri",   Icon = "ImageMultipleOutline", DocumentCount = 0 });
            Folders.Add(new DocumentFolderVm { Id = Guid.NewGuid(), Name = "Sözleşmeler",       Icon = "FileDocumentOutline",  DocumentCount = 0 });
            Folders.Add(new DocumentFolderVm { Id = Guid.NewGuid(), Name = "Kargo Belgeleri",   Icon = "TruckOutline",         DocumentCount = 0 });
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectFolder(DocumentFolderVm folder)
    {
        SelectedFolder = folder;
        CurrentDocuments.Clear();

        // Mock documents for the selected folder
        var now = DateTime.Now;
        CurrentDocuments.Add(new DocumentItemVm
        {
            Id            = Guid.NewGuid(),
            FileName      = $"{folder.Name}_Ornek_1.pdf",
            MimeType      = "application/pdf",
            FileSizeBytes = 512 * 1024,
            UploadedAt    = now.AddDays(-3),
            Tags          = folder.Name,
        });
        CurrentDocuments.Add(new DocumentItemVm
        {
            Id            = Guid.NewGuid(),
            FileName      = $"{folder.Name}_Ornek_2.xlsx",
            MimeType      = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 2 * 1024 * 1024 + 128 * 1024,
            UploadedAt    = now.AddDays(-7),
            Tags          = folder.Name,
        });

        folder.DocumentCount = CurrentDocuments.Count;
    }

    [RelayCommand]
    private void UploadDocument()
    {
        MessageBox.Show("Belge yükleme yakında", "MesTech Belgeler",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void DeleteDocument(Guid docId)
    {
        MessageBox.Show("Silme yakında", "MesTech Belgeler",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public partial class DocumentFolderVm : ObservableObject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "FolderOutline";
    [ObservableProperty] private int documentCount;
}

public class DocumentItemVm
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:F1} KB"
        : $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
    public DateTime UploadedAt { get; set; }
    public string? Tags { get; set; }
}
