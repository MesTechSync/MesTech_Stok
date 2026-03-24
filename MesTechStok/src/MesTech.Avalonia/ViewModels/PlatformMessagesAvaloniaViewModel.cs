using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Avalonia.Services;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

public partial class PlatformMessagesAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;

    [ObservableProperty] private int totalCount;

    // Filter state
    [ObservableProperty] private string selectedStatusFilter = "Tumu";
    [ObservableProperty] private string selectedPlatformFilter = "Tumu";

    // Master-detail
    [ObservableProperty] private PlatformMessageItemVm? selectedMessage;
    [ObservableProperty] private string replyText = string.Empty;
    [ObservableProperty] private bool isSendingReply;

    public ObservableCollection<PlatformMessageItemVm> Messages { get; } = [];

    public string[] StatusFilters { get; } = ["Tumu", "Okunmadi", "Yanit Bekliyor", "Arsiv"];
    public string[] PlatformFilters { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"];

    public PlatformMessagesAvaloniaViewModel(IMediator mediator, IDialogService dialog)
    {
        _mediator = mediator;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var platform = SelectedPlatformFilter switch
            {
                "Trendyol" => (PlatformType?)PlatformType.Trendyol,
                "Hepsiburada" => PlatformType.Hepsiburada,
                "N11" => PlatformType.N11,
                "Amazon" => PlatformType.Amazon,
                "Ciceksepeti" => PlatformType.Ciceksepeti,
                _ => null
            };

            var status = SelectedStatusFilter switch
            {
                "Okunmadi" => (MessageStatus?)MessageStatus.Unread,
                "Yanit Bekliyor" => MessageStatus.Read,
                "Arsiv" => MessageStatus.Archived,
                _ => null
            };

            var result = await _mediator.Send(new GetPlatformMessagesQuery(
                TenantId: Guid.Empty,
                Platform: platform,
                Status: status));

            Messages.Clear();
            foreach (var dto in result.Items)
            {
                Messages.Add(MapToVm(dto));
            }

            TotalCount = result.TotalCount;
            IsEmpty = result.TotalCount == 0;

            if (Messages.Count > 0 && SelectedMessage is null)
                SelectedMessage = Messages[0];
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Mesajlar yuklenemedi: {ex.Message}";
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

        IsSendingReply = true;
        try
        {
            await _mediator.Send(new ReplyToMessageCommand(
                SelectedMessage.Id,
                ReplyText,
                "CurrentUser"));

            ReplyText = string.Empty;
            await _dialog.ShowInfoAsync("Yanit gonderildi.", "MesTech CRM");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await _dialog.ShowInfoAsync($"Yanit gonderilemedi: {ex.Message}", "Hata");
        }
        finally
        {
            IsSendingReply = false;
        }
    }

    [RelayCommand]
    private void UseAiSuggestion()
    {
        if (SelectedMessage?.AiSuggestedReply is not null)
            ReplyText = SelectedMessage.AiSuggestedReply;
    }

    [RelayCommand]
    private void DismissAiSuggestion()
    {
        if (SelectedMessage is not null)
            SelectedMessage.AiSuggestedReply = null;
    }

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();
    partial void OnSelectedPlatformFilterChanged(string value) => _ = LoadAsync();

    private static PlatformMessageItemVm MapToVm(PlatformMessageDto dto) => new()
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
        ReceivedAtDisplay = FormatTimeAgo(dto.ReceivedAt),
        PlatformColor = GetPlatformColor(dto.Platform)
    };

    private static string GetPlatformColor(string platform) => platform switch
    {
        "Trendyol" => "#FF6000",
        "Hepsiburada" => "#FF6100",
        "N11" => "#7B2D8E",
        "Amazon" => "#FF9900",
        "Ciceksepeti" => "#E91E63",
        _ => "#0078D4"
    };

    private static string FormatTimeAgo(DateTime date)
    {
        var diff = DateTime.Now - date;
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} gun once";
        return date.ToString("dd.MM.yyyy");
    }
}

public partial class PlatformMessageItemVm : ObservableObject
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool HasAiSuggestion { get; set; }
    [ObservableProperty] private string? aiSuggestedReply;
    public DateTime ReceivedAt { get; set; }
    public string ReceivedAtDisplay { get; set; } = string.Empty;
    public string PlatformColor { get; set; } = "#0078D4";
}
