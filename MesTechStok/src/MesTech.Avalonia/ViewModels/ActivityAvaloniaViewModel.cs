using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class ActivityAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string? selectedFilter;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ActivityItemVm> Activities { get; } = [];
    public string[] FilterOptions { get; } = ["Tumu", "Arama", "Toplanti", "E-posta", "Not", "Gorev"];

    public ActivityAvaloniaViewModel(IMediator mediator)
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
            // MediatR handler bağlantısı bekliyor — Task.Delay kaldırıldı
            Activities.Clear();
            Activities.Add(new ActivityItemVm
            {
                Id = Guid.NewGuid(), Type = "Arama", Subject = "ABC Ltd ile gorusme",
                Description = "Teklif detaylari konusuldu", ContactName = "Ahmet Yilmaz",
                ActivityDate = DateTime.Now.AddHours(-2), CreatedBy = "Fatih I."
            });
            Activities.Add(new ActivityItemVm
            {
                Id = Guid.NewGuid(), Type = "E-posta", Subject = "Teklif gonderimi",
                Description = "Fiyat listesi mail ile iletildi", ContactName = "Fatma Demir",
                ActivityDate = DateTime.Now.AddHours(-5), CreatedBy = "Mehmet C."
            });
            Activities.Add(new ActivityItemVm
            {
                Id = Guid.NewGuid(), Type = "Toplanti", Subject = "Demo sunumu",
                Description = "Urun demo'su yapildi, olumlu donus", ContactName = "Ali Kaya",
                ActivityDate = DateTime.Now.AddDays(-1), CreatedBy = "Fatih I."
            });
            Activities.Add(new ActivityItemVm
            {
                Id = Guid.NewGuid(), Type = "Not", Subject = "Musteri notu",
                Description = "Butce onay sureci devam ediyor", ContactName = "Zeynep Arslan",
                ActivityDate = DateTime.Now.AddDays(-2), CreatedBy = "Ayse K."
            });
            TotalCount = Activities.Count;
            IsEmpty = Activities.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Aktiviteler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ActivityItemVm
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactName { get; set; }
    public DateTime ActivityDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - ActivityDate;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
            return $"{(int)diff.TotalDays} gun once";
        }
    }

    public string TypeIcon => Type switch
    {
        "Arama" => "T",    // phone icon placeholder
        "E-posta" => "@",
        "Toplanti" => "M",
        "Not" => "N",
        "Gorev" => "G",
        _ => "?"
    };
}
