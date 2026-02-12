using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MesTechStok.Desktop.Views
{
    public partial class CategoryManagerDialog : Window
    {
        private readonly AppDbContext _db;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalItems = 0;
        private int _selectedId = 0;

        public CategoryManagerDialog()
        {
            InitializeComponent();
            _db = MesTechStok.Desktop.App.ServiceProvider!.GetRequiredService<AppDbContext>();
            CmbCatPageSize.SelectedIndex = 1; // 50
            LoadAsync();
            this.Activate(); this.Focus();
        }

        private async void LoadAsync()
        {
            try
            {
                var term = (TxtSearchCat?.Text ?? string.Empty).Trim();
                var q = _db.Categories.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    q = q.Where(c => c.Name.Contains(term) || c.Code.Contains(term));
                }
                _totalItems = await q.CountAsync();
                var items = await q.OrderBy(c => c.Name)
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToListAsync();
                CategoriesGrid.ItemsSource = items;
                UpdatePagerUi();
            }
            catch { }
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
            catch { }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = (TxtCatName.Text ?? "").Trim();
                var code = (TxtCatCode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Kategori adı gerekli."); return; }
                if (string.IsNullOrWhiteSpace(code)) code = name.ToUpperInvariant().Replace(' ', '_');
                if (await _db.Categories.AnyAsync(c => c.Name == name || c.Code == code))
                {
                    MessageBox.Show("Aynı ad veya kodda kategori mevcut.");
                    return;
                }
                _db.Categories.Add(new Category
                {
                    Name = name,
                    Code = code,
                    IsActive = ChkActive.IsChecked == true,
                    CreatedDate = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                TxtCatName.Text = string.Empty; TxtCatCode.Text = string.Empty; ChkActive.IsChecked = true; _currentPage = 1;
                LoadAsync();
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
                if (_selectedId <= 0) { MessageBox.Show("Seçim yapın."); return; }
                var name = (TxtCatName.Text ?? "").Trim();
                var code = (TxtCatCode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Kategori adı gerekli."); return; }
                var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == _selectedId);
                if (cat == null) return;
                // adı/kodu çakışmasın
                if (await _db.Categories.AnyAsync(c => (c.Name == name || (!string.IsNullOrWhiteSpace(code) && c.Code == code)) && c.Id != _selectedId))
                {
                    MessageBox.Show("Aynı ad/kod mevcut."); return;
                }
                cat.Name = name;
                cat.Code = string.IsNullOrWhiteSpace(code) ? name.ToUpperInvariant().Replace(' ', '_') : code;
                cat.IsActive = ChkActive.IsChecked == true;
                cat.ModifiedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                LoadAsync();
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
                if (_selectedId <= 0) { MessageBox.Show("Seçim yapın."); return; }
                var cat = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == _selectedId);
                if (cat == null) return;
                if (cat.Products.Any())
                {
                    // Ürün var: pasif et
                    cat.IsActive = false;
                }
                else
                {
                    _db.Categories.Remove(cat);
                }
                await _db.SaveChangesAsync();
                ClearForm();
                LoadAsync();
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
                _selectedId = 0;
                TxtCatName.Text = string.Empty;
                TxtCatCode.Text = string.Empty;
                ChkActive.IsChecked = true;
                CategoriesGrid.SelectedItem = null;
            }
            catch { }
        }

        private void CategoriesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CategoriesGrid.SelectedItem is Category c)
                {
                    _selectedId = c.Id;
                    TxtCatName.Text = c.Name;
                    TxtCatCode.Text = c.Code;
                    ChkActive.IsChecked = c.IsActive;
                }
            }
            catch { }
        }

        private void TxtSearchCat_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try { _currentPage = 1; LoadAsync(); } catch { }
        }

        private void CmbCatPageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbCatPageSize.SelectedItem is System.Windows.Controls.ComboBoxItem cb && int.TryParse(cb.Tag?.ToString(), out var ps))
                {
                    _pageSize = Math.Max(1, Math.Min(ps, 200));
                    _currentPage = 1;
                    LoadAsync();
                }
            }
            catch { }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; LoadAsync(); }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++; LoadAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}


