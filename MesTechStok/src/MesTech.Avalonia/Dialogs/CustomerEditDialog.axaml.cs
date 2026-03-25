using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class CustomerEditDialog : Window
{
    public bool Result { get; private set; }
    public string? CustomerName => NameBox.Text;
    public string? Code => CodeBox.Text;
    public string? Email => EmailBox.Text;
    public string? Phone => PhoneBox.Text;
    public string? CustomerType => (TypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    public bool IsVip => VipCheck.IsChecked == true;
    public string? BillingAddress => BillingBox.Text;
    public string? ShippingAddress => ShippingBox.Text;
    public string? City => CityBox.Text;
    public string? State => StateBox.Text;
    public string? PostalCode => PostalCodeBox.Text;
    public string? Country => CountryBox.Text;
    public string? CreditLimit => CreditLimitBox.Text;
    public string? Discount => DiscountBox.Text;
    public string? PaymentTerm => PaymentTermBox.Text;
    public string? Currency => CurrencyBox.Text;

    public CustomerEditDialog() : this("Musteri Duzenle") { }

    public CustomerEditDialog(string title = "Musteri Duzenle",
                              string? name = null,
                              string? code = null,
                              string? email = null,
                              string? phone = null,
                              string? customerType = null,
                              bool isVip = false,
                              string? billingAddress = null,
                              string? shippingAddress = null,
                              string? city = null,
                              string? state = null,
                              string? postalCode = null,
                              string? country = null,
                              string? creditLimit = null,
                              string? discount = null,
                              string? paymentTerm = null,
                              string? currency = null)
    {
        InitializeComponent();
        TitleText.Text = title;

        if (name != null) NameBox.Text = name;
        if (code != null) CodeBox.Text = code;
        if (email != null) EmailBox.Text = email;
        if (phone != null) PhoneBox.Text = phone;
        if (customerType != null) SetCustomerType(customerType);
        VipCheck.IsChecked = isVip;
        if (billingAddress != null) BillingBox.Text = billingAddress;
        if (shippingAddress != null) ShippingBox.Text = shippingAddress;
        if (city != null) CityBox.Text = city;
        if (state != null) StateBox.Text = state;
        if (postalCode != null) PostalCodeBox.Text = postalCode;
        if (country != null) CountryBox.Text = country;
        if (creditLimit != null) CreditLimitBox.Text = creditLimit;
        if (discount != null) DiscountBox.Text = discount;
        if (paymentTerm != null) PaymentTermBox.Text = paymentTerm;
        if (currency != null) CurrencyBox.Text = currency;
    }

    private void SetCustomerType(string type)
    {
        for (int i = 0; i < TypeCombo.ItemCount; i++)
        {
            if (TypeCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == type)
            {
                TypeCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void OnCopyAddress(object? sender, RoutedEventArgs e)
    {
        ShippingBox.Text = BillingBox.Text;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
