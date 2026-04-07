using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Manuel Sipariş Oluşturma ViewModel — G10875 P1 gap fix.
/// CreateOrderCommand ile yeni sipariş kaydeder.
/// </summary>
public partial class NewOrderAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IToastService _toast;

    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string customerEmail = string.Empty;
    [ObservableProperty] private string orderType = "Manuel";
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private DateTimeOffset? requiredDate;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string resultMessage = string.Empty;

    public string[] OrderTypeOptions { get; } = ["Manuel", "Telefon", "E-posta", "Toptan"];

    public NewOrderAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IToastService toast)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _toast = toast;
    }

    public override Task LoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task CreateOrder()
    {
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            _toast.ShowError("Musteri adi zorunludur");
            return;
        }

        IsSaving = true;
        ResultMessage = string.Empty;

        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new CreateOrderCommand(
                CustomerId: Guid.Empty,
                CustomerName: CustomerName,
                CustomerEmail: string.IsNullOrWhiteSpace(CustomerEmail) ? null : CustomerEmail,
                OrderType: OrderType,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                RequiredDate: RequiredDate?.DateTime), ct);

            if (result.IsSuccess)
            {
                ResultMessage = $"Siparis olusturuldu: {result.OrderNumber}";
                _toast.ShowSuccess(ResultMessage);
                CustomerName = string.Empty;
                CustomerEmail = string.Empty;
                Notes = string.Empty;
            }
            else
            {
                ResultMessage = result.ErrorMessage ?? "Siparis olusturulamadi";
                _toast.ShowError(ResultMessage);
            }
        }, "Siparis olusturulurken hata");

        IsSaving = false;
    }

    [RelayCommand]
    private void Clear()
    {
        CustomerName = string.Empty;
        CustomerEmail = string.Empty;
        Notes = string.Empty;
        RequiredDate = null;
        ResultMessage = string.Empty;
    }
}
