using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.UpdateCategory;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Application.Queries.GetCategoriesPaged;

namespace MesTechStok.Desktop.Views
{
    public partial class CategoryManagerDialog : Window
    {
        private readonly IMediator _mediator;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalItems = 0;
        private Guid _selectedId = Guid.Empty;

        public CategoryManagerDialog()
        {
            InitializeComponent();
            _mediator = MesTechStok.Desktop.App.ServiceProvider!.GetRequiredService<IMediator>();
            CmbCatPageSize.SelectedIndex = 1; // 50
            _ = LoadAsync();
            this.Activate(); this.Focus();
        }

        private async Task LoadAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                ErrorState.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Collapsed;

                var term = (TxtSearchCat?.Text ?? string.Empty).Trim();
                var result = await _mediator.Send(new GetCategoriesPagedQuery(
                    SearchTerm: string.IsNullOrWhiteSpace(term) ? null : term,
                    Page: _currentPage,
                    PageSize: _pageSize));

                _totalItems = result.TotalCount;
                CategoriesGrid.ItemsSource = result.Items;
                UpdatePagerUi();

                LoadingOverlay.Visibility = Visibility.Collapsed;

                if (result.TotalCount == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                    CategoriesGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyState.Visibility = Visibility.Collapsed;
                    CategoriesGrid.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ErrorState.Visibility = Visibility.Visible;
                ErrorMessageText.Text = $"Veri yüklenemedi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CategoryManager] LoadAsync failed: {ex.Message}");
            }
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadAsync();
        }

        private void UpdatePagerUi()
        {
            try
            {
                int totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalItems / _pageSize));
                _currentPage = Math.Max(1, Math.Min(_currentPage, totalPages));
                TxtPageInfo.Text = $"Sayfa {_currentPage}/{totalPages}";
                TxtTotalInfo.Text = $"{_totalItems} sonuç";
                BtnPrev.IsEnabled = _currentPage > 1;
                BtnNext.IsEnabled = _currentPage < totalPages;
            }
            catch
            {
                // Intentional: UI element update — element may not be loaded during early data binding.
            }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = (TxtCatName.Text ?? "").Trim();
                var code = (TxtCatCode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Kategori adı gerekli."); return; }
                if (string.IsNullOrWhiteSpace(code)) code = name.ToUpperInvariant().Replace(' ', '_');

                var result = await _mediator.Send(new CreateCategoryCommand(name, code, ChkActive.IsChecked == true));

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Hata oluştu.");
                    return;
                }

                TxtCatName.Text = string.Empty; TxtCatCode.Text = string.Empty; ChkActive.IsChecked = true; _currentPage = 1;
                _ = LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private async void UpdateCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedId == Guid.Empty) { MessageBox.Show("Seçim yapın."); return; }
                var name = (TxtCatName.Text ?? "").Trim();
                var code = (TxtCatCode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Kategori adı gerekli."); return; }

                var result = await _mediator.Send(new UpdateCategoryCommand(_selectedId, name, code, ChkActive.IsChecked == true));

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Hata oluştu.");
                    return;
                }

                _ = LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedId == Guid.Empty) { MessageBox.Show("Seçim yapın."); return; }

                var result = await _mediator.Send(new DeleteCategoryCommand(_selectedId));

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Hata oluştu.");
                    return;
                }

                ClearForm();
                _ = LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            try
            {
                _selectedId = Guid.Empty;
                TxtCatName.Text = string.Empty;
                TxtCatCode.Text = string.Empty;
                ChkActive.IsChecked = true;
                CategoriesGrid.SelectedItem = null;
            }
            catch
            {
                // Intentional: UI event handler (form clear) — UI elements may not be initialized.
            }
        }

        private void CategoriesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CategoriesGrid.SelectedItem is CategoryItemDto c)
                {
                    _selectedId = c.Id;
                    TxtCatName.Text = c.Name;
                    TxtCatCode.Text = c.Code;
                    ChkActive.IsChecked = c.IsActive;
                }
            }
            catch
            {
                // Intentional: UI event handler (grid selection) — UI element access may fail during teardown.
            }
        }

        private void TxtSearchCat_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try { _currentPage = 1; _ = LoadAsync(); }
            catch { /* Intentional: search keyup event handler — async load must not crash event chain. */ }
        }

        private void CmbCatPageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbCatPageSize.SelectedItem is System.Windows.Controls.ComboBoxItem cb && int.TryParse(cb.Tag?.ToString(), out var ps))
                {
                    _pageSize = Math.Max(1, Math.Min(ps, 200));
                    _currentPage = 1;
                    _ = LoadAsync();
                }
            }
            catch
            {
                // Intentional: UI event handler (page-size selector) — ComboBox may fire during teardown.
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; _ = LoadAsync(); }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++; _ = LoadAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
