using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Orders;

/// <summary>
/// Sipariş Kanban Board ViewModel — Bitrix24 pattern.
/// Yeni → Onaylandı → Hazırlanıyor → Kargoda → Teslim Edildi
/// </summary>
public partial class OrderKanbanViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public OrderKanbanViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    [ObservableProperty] private string selectedPlatform = "Tümü";

    public ObservableCollection<string> PlatformFilters { get; } =
        ["Tümü", "Trendyol", "Hepsiburada", "N11", "Çiçeksepeti", "Pazarama"];

    public ObservableCollection<KanbanColumnVm> KanbanColumns { get; } = [];
    public ObservableCollection<ColumnSummaryVm> ColumnSummaries { get; } = [];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(
                new GetOrdersByStatusQuery(tenantId), CancellationToken);

            BuildKanbanBoard(result);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kanban yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ISolidColorBrush GetBrush(string key, string fallback = "#888888")
    {
        var app = global::Avalonia.Application.Current;
        if (app != null && app.Resources.TryGetResource(key, app.ActualThemeVariant, out var res)
            && res is ISolidColorBrush brush)
            return brush;
        // Color token'dan brush oluştur
        if (app != null && app.Resources.TryGetResource(key, app.ActualThemeVariant, out var colorRes)
            && colorRes is Color color)
            return new SolidColorBrush(color);
        return SolidColorBrush.Parse(fallback);
    }

    private void BuildKanbanBoard(OrderKanbanResult result)
    {
        KanbanColumns.Clear();
        ColumnSummaries.Clear();

        // Status → brush mapping
        ISolidColorBrush GetStatusBrush(string status) => status switch
        {
            "Yeni" => GetBrush("InfoBrush", "#3B82F6"),
            "Hazırlanıyor" or "Onaylandı" => GetBrush("WarningOrangeBrush", "#F59E0B"),
            "Kargoda" => GetBrush("SuccessBrush", "#10B981"),
            "Teslim Edildi" => GetBrush("MesPurple", "#6366F1"),
            _ => GetBrush("InfoBrush", "#3B82F6")
        };

        ISolidColorBrush GetPlatformBrush(string? platform) => platform switch
        {
            "Trendyol" => GetBrush("MesBrandAccent", "#F27A1A"),
            "Hepsiburada" => GetBrush("MesBrandHepsiburada", "#FF6000"),
            "N11" => GetBrush("MesBrandN11", "#6B21A8"),
            "Çiçeksepeti" or "Ciceksepeti" => GetBrush("MesBrandCiceksepeti", "#E91E63"),
            "Pazarama" => GetBrush("MesBrandPazarama", "#FF5722"),
            _ => GetBrush("InfoBrush", "#3B82F6")
        };

        string PlatformShort(string? platform) => platform switch
        {
            "Trendyol" => "TR",
            "Hepsiburada" => "HB",
            "N11" => "N11",
            "Çiçeksepeti" or "Ciceksepeti" => "ÇS",
            "Pazarama" => "PZ",
            _ => platform?[..Math.Min(2, platform.Length)] ?? "?"
        };

        string TimeAgoText(DateTime date)
        {
            var diff = DateTime.UtcNow - date;
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} dk";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat";
            return $"{(int)diff.TotalDays} gün";
        }

        foreach (var col in result.Columns)
        {
            var statusBrush = GetStatusBrush(col.Status);
            var kanbanCol = new KanbanColumnVm(col.Status, statusBrush);

            foreach (var order in col.Orders)
            {
                var card = new KanbanOrderCard(
                    order.OrderNumber,
                    order.Platform ?? "-",
                    PlatformShort(order.Platform),
                    GetPlatformBrush(order.Platform),
                    order.CustomerName ?? "-",
                    $"{order.TotalAmount:N0} TL",
                    "-",
                    TimeAgoText(order.OrderDate));
                kanbanCol.Orders.Add(card);
            }

            KanbanColumns.Add(kanbanCol);
            ColumnSummaries.Add(new ColumnSummaryVm(col.Status, statusBrush, col.Count));
        }

        IsEmpty = KanbanColumns.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSelectedPlatformChanged(string value)
    {
        // Platform filtre uygulanacak — şimdilik tüm verileri göster
        _ = LoadAsync();
    }
}

public class KanbanColumnVm
{
    public KanbanColumnVm(string statusName, ISolidColorBrush headerColor)
    {
        StatusName = statusName;
        HeaderColor = headerColor;
    }

    public string StatusName { get; }
    public ISolidColorBrush HeaderColor { get; }
    public ObservableCollection<KanbanOrderCard> Orders { get; } = [];
}

public class KanbanOrderCard
{
    public KanbanOrderCard(string orderNumber, string platform, string platformShort,
        ISolidColorBrush platformColor, string customerName, string totalAmountText,
        string itemCountText, string timeAgo)
    {
        OrderNumber = orderNumber;
        Platform = platform;
        PlatformShort = platformShort;
        PlatformColor = platformColor;
        CustomerName = customerName;
        TotalAmountText = totalAmountText;
        ItemCountText = itemCountText;
        TimeAgo = timeAgo;
    }

    public string OrderNumber { get; }
    public string Platform { get; }
    public string PlatformShort { get; }
    public ISolidColorBrush PlatformColor { get; }
    public string CustomerName { get; }
    public string TotalAmountText { get; }
    public string ItemCountText { get; }
    public string TimeAgo { get; }
}

public class ColumnSummaryVm
{
    public ColumnSummaryVm(string statusName, ISolidColorBrush headerColor, int count)
    {
        StatusName = statusName;
        HeaderColor = headerColor;
        Count = count;
    }

    public string StatusName { get; }
    public ISolidColorBrush HeaderColor { get; }
    public int Count { get; }
}
