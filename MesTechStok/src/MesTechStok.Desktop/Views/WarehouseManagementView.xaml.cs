using System;
using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// Depo Yönetimi View'ı - Zone, Rack, Shelf, Bin CRUD işlemleri
    /// </summary>
    public partial class WarehouseManagementView : UserControl
    {
        public WarehouseManagementView()
        {
            InitializeComponent();
        }

        #region TreeView Events

        /// <summary>
        /// TreeView'da öğe seçimi değiştiğinde
        /// </summary>
        private void WarehouseTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is TreeViewItem selectedItem)
                {
                    // TODO: ViewModel'deki seçim değişikliği logic'ini çağır
                    var header = selectedItem.Header?.ToString() ?? "";
                    MessageBox.Show($"Seçilen öğe: {header}", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Öğe seçimi sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Context Menu Events

        /// <summary>
        /// Sağ tık menüsü açıldığında
        /// </summary>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Context menu logic'i
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Context menu açılırken hata: {ex.Message}");
            }
        }

        #endregion

        #region UserControl Events

        /// <summary>
        /// View yüklendiğinde
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // View yüklendiğinde yapılacak işlemler
                // Örneğin: Depo yapısını yükle, TreeView'ı populate et vb.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WarehouseManagementView yüklenirken hata: {ex.Message}");
            }
        }

        #endregion
    }
}
