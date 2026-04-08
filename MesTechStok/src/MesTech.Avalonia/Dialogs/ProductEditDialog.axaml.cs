using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ProductEditDialog : Window
{
    public bool Result { get; private set; }
    public string? ProductName => NameBox.Text;
    public string? Sku => SkuBox.Text;
    public string? Barcode => BarcodeBox.Text;
    public string? Price => PriceBox.Text;
    public Guid SelectedCategoryId { get; private set; }
    public string? Description => DescriptionBox.Text;

    private readonly List<CategoryOption> _categories = [];

    public ProductEditDialog() : this("Urun Duzenle") { }

    public ProductEditDialog(string title = "Urun Duzenle",
                             string? name = null,
                             string? sku = null,
                             string? barcode = null,
                             string? price = null,
                             string? category = null,
                             string? description = null,
                             IReadOnlyList<(Guid Id, string Name)>? categories = null,
                             Guid? selectedCategoryId = null)
    {
        InitializeComponent();
        TitleText.Text = title;

        if (name != null) NameBox.Text = name;
        if (sku != null) SkuBox.Text = sku;
        if (barcode != null) BarcodeBox.Text = barcode;
        if (price != null) PriceBox.Text = price;
        if (description != null) DescriptionBox.Text = description;

        // Populate category ComboBox
        if (categories is { Count: > 0 })
        {
            foreach (var (id, catName) in categories)
                _categories.Add(new CategoryOption(id, catName));
        }
        else
        {
            _categories.Add(new CategoryOption(Guid.Empty, "Kategorisiz"));
        }

        CategoryBox.ItemsSource = _categories;
        CategoryBox.DisplayMemberBinding = new global::Avalonia.Data.Binding("Name");

        // Pre-select category
        if (selectedCategoryId.HasValue && selectedCategoryId != Guid.Empty)
        {
            var match = _categories.FindIndex(c => c.Id == selectedCategoryId.Value);
            if (match >= 0) CategoryBox.SelectedIndex = match;
        }
        else if (category != null)
        {
            var match = _categories.FindIndex(c => c.Name == category);
            if (match >= 0) CategoryBox.SelectedIndex = match;
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        SelectedCategoryId = (CategoryBox.SelectedItem as CategoryOption)?.Id ?? Guid.Empty;
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

internal record CategoryOption(Guid Id, string Name)
{
    public override string ToString() => Name;
}
