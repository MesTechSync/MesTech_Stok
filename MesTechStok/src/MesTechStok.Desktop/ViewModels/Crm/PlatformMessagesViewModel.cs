using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Crm;

public partial class PlatformMessagesViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedPlatform;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private MessageItemVm? selectedMessage;
    [ObservableProperty] private string replyText = string.Empty;
    [ObservableProperty] private string? errorMessage;

    public ObservableCollection<MessageItemVm> Messages { get; } = [];

    public string[] PlatformOptions { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti", "OpenCart"];

    public string[] StatusOptions { get; } =
        ["Tumu", "Okunmamis", "Okundu", "Yanitlandi", "Arsivlendi"];

    // Visibility helpers for XAML binding
    public Visibility NoSelectionVisibility =>
        SelectedMessage is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DetailVisibility =>
        SelectedMessage is not null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AiSuggestionPanelVisibility =>
        SelectedMessage?.HasAiSuggestion == true ? Visibility.Visible : Visibility.Collapsed;

    public PlatformMessagesViewModel(IMediator mediator, ITenantProvider tenantProvider, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var platformFilter = MapPlatform(SelectedPlatform);
            var statusFilter = MapStatus(SelectedStatus);

            var result = await _mediator.Send(new GetPlatformMessagesQuery(
                tenantId, platformFilter, statusFilter, 1, 50));

            Messages.Clear();
            foreach (var dto in result.Items)
            {
                Messages.Add(new MessageItemVm
                {
                    Id = dto.Id,
                    Platform = dto.Platform,
                    SenderName = dto.SenderName,
                    Subject = dto.Subject,
                    BodyPreview = dto.BodyPreview,
                    Status = dto.Status,
                    Direction = dto.Direction,
                    HasAiSuggestion = dto.HasAiSuggestion,
                    AiSuggestedReply = dto.AiSuggestedReply,
                    ReceivedAt = dto.ReceivedAt,
                    RepliedAt = dto.RepliedAt,
                    PlatformColor = GetPlatformColor(dto.Platform),
                    PlatformInitial = GetPlatformInitial(dto.Platform),
                    StatusColor = GetStatusColor(dto.Status),
                    ReceivedAtFormatted = dto.ReceivedAt.ToString("dd.MM HH:mm"),
                    AiSuggestionVisibility = dto.HasAiSuggestion ? Visibility.Visible : Visibility.Collapsed
                });
            }
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlatformMessagesViewModel] LoadAsync error: {ex.Message}");
            ErrorMessage = ex.Message;
            LoadPlaceholderData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task SendReply()
    {
        if (SelectedMessage is null || string.IsNullOrWhiteSpace(ReplyText))
            return;

        try
        {
            await _mediator.Send(new ReplyToMessageCommand(
                SelectedMessage.Id,
                ReplyText,
                _currentUser.Username ?? "system"));

            ReplyText = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlatformMessagesViewModel] SendReply error: {ex.Message}");
            System.Windows.MessageBox.Show($"Yanit gonderilemedi: {ex.Message}", "MesTech CRM");
        }
    }

    [RelayCommand]
    private void UseAiSuggestion()
    {
        if (SelectedMessage?.AiSuggestedReply is not null)
            ReplyText = SelectedMessage.AiSuggestedReply;
    }

    [RelayCommand]
    private void EditAiSuggestion()
    {
        if (SelectedMessage?.AiSuggestedReply is not null)
            ReplyText = SelectedMessage.AiSuggestedReply;
        // Focus will be on the reply TextBox — user can edit
    }

    [RelayCommand]
    private void IgnoreAiSuggestion()
    {
        // Just close/dismiss — no action needed, user writes own reply
    }

    partial void OnSelectedMessageChanged(MessageItemVm? value)
    {
        OnPropertyChanged(nameof(NoSelectionVisibility));
        OnPropertyChanged(nameof(DetailVisibility));
        OnPropertyChanged(nameof(AiSuggestionPanelVisibility));
        ReplyText = string.Empty;
    }

    partial void OnSelectedPlatformChanged(string? value)
        => _ = LoadAsync();

    partial void OnSelectedStatusChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }

    private void LoadPlaceholderData()
    {
        Messages.Clear();
        TotalCount = 0;
    }

    private static PlatformType? MapPlatform(string? selected) => selected switch
    {
        "Trendyol" => PlatformType.Trendyol,
        "Hepsiburada" => PlatformType.Hepsiburada,
        "N11" => PlatformType.N11,
        "Amazon" => PlatformType.Amazon,
        "Ciceksepeti" => PlatformType.Ciceksepeti,
        "OpenCart" => PlatformType.OpenCart,
        _ => null
    };

    private static MessageStatus? MapStatus(string? selected) => selected switch
    {
        "Okunmamis" => MessageStatus.Unread,
        "Okundu" => MessageStatus.Read,
        "Yanitlandi" => MessageStatus.Replied,
        "Arsivlendi" => MessageStatus.Archived,
        _ => null
    };

    private static string GetPlatformColor(string platform) => platform switch
    {
        "Trendyol" => "#FF6000",
        "Hepsiburada" => "#FF6100",
        "N11" => "#7B2D8E",
        "Amazon" => "#FF9900",
        "Ciceksepeti" => "#E91E63",
        "OpenCart" => "#2196F3",
        "Pazarama" => "#00BCD4",
        _ => "#607D8B"
    };

    private static string GetPlatformInitial(string platform) => platform switch
    {
        "Trendyol" => "T",
        "Hepsiburada" => "H",
        "N11" => "N",
        "Amazon" => "A",
        "Ciceksepeti" => "C",
        "OpenCart" => "O",
        "Pazarama" => "P",
        _ => "?"
    };

    private static string GetStatusColor(string status) => status switch
    {
        "Unread" => "#D32F2F",
        "Read" => "#1976D2",
        "Replied" => "#388E3C",
        "Archived" => "#757575",
        _ => "#607D8B"
    };
}

public class MessageItemVm
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool HasAiSuggestion { get; set; }
    public string? AiSuggestedReply { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? RepliedAt { get; set; }

    // UI helpers
    public string PlatformColor { get; set; } = "#607D8B";
    public string PlatformInitial { get; set; } = "?";
    public string StatusColor { get; set; } = "#607D8B";
    public string ReceivedAtFormatted { get; set; } = string.Empty;
    public Visibility AiSuggestionVisibility { get; set; } = Visibility.Collapsed;
}
