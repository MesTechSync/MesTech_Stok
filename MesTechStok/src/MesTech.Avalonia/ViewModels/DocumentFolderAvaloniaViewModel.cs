using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Documents.Queries.GetDocumentFolders;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class DocumentFolderAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DocFolderItemVm? selectedFolder;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<DocFolderItemVm> Folders { get; } = [];
    public ObservableCollection<DocFileItemVm> Files { get; } = [];

    public DocumentFolderAvaloniaViewModel(IMediator mediator, IDialogService dialog, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _dialog = dialog;
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
            var result = await _mediator.Send(new GetDocumentFoldersQuery(_currentUser.TenantId));
            Folders.Clear();
            Files.Clear();
            foreach (var f in result.Folders)
            {
                Folders.Add(new DocFolderItemVm
                {
                    Id = f.Id,
                    Name = f.Name,
                    FileCount = f.DocumentCount,
                    Icon = "F"
                });
            }
            TotalCount = result.TotalCount;
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
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
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
