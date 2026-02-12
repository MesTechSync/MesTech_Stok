using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MesTechStok.Desktop.Components
{
    public partial class PaginationComponent : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register("CurrentPage", typeof(int), typeof(PaginationComponent),
                new PropertyMetadata(1, OnCurrentPageChanged));

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register("PageSize", typeof(int), typeof(PaginationComponent),
                new PropertyMetadata(50, OnPageSizeChanged));

        public static readonly DependencyProperty TotalItemsProperty =
            DependencyProperty.Register("TotalItems", typeof(int), typeof(PaginationComponent),
                new PropertyMetadata(0, OnTotalItemsChanged));

        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register("TotalPages", typeof(int), typeof(PaginationComponent),
                new PropertyMetadata(1));

        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        public int TotalItems
        {
            get => (int)GetValue(TotalItemsProperty);
            set => SetValue(TotalItemsProperty, value);
        }

        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            private set => SetValue(TotalPagesProperty, value);
        }

        #endregion

        #region Events

        public event EventHandler<PaginationEventArgs>? PageChanged;
        public event EventHandler<PaginationEventArgs>? PageSizeChanged;

        #endregion

        public PaginationComponent()
        {
            InitializeComponent();
            UpdateUI();
        }

        #region Property Changed Handlers

        private static void OnCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaginationComponent pagination)
            {
                pagination.UpdateUI();
                pagination.PageChanged?.Invoke(pagination, new PaginationEventArgs
                {
                    CurrentPage = pagination.CurrentPage,
                    PageSize = pagination.PageSize,
                    TotalItems = pagination.TotalItems
                });
            }
        }

        private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaginationComponent pagination)
            {
                pagination.CalculateTotalPages();
                pagination.CurrentPage = 1; // Reset to first page when page size changes
                pagination.UpdateUI();
                pagination.PageSizeChanged?.Invoke(pagination, new PaginationEventArgs
                {
                    CurrentPage = pagination.CurrentPage,
                    PageSize = pagination.PageSize,
                    TotalItems = pagination.TotalItems
                });
            }
        }

        private static void OnTotalItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaginationComponent pagination)
            {
                pagination.CalculateTotalPages();
                pagination.UpdateUI();
            }
        }

        #endregion

        #region UI Updates

        private void CalculateTotalPages()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));

            // Ensure current page is within valid range
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        private void UpdateUI()
        {
            UpdateNavigationButtons();
            UpdatePageInfo();
            UpdateItemInfo();
            UpdatePageNumbers();
            UpdatePageSizeSelector();
        }

        private void UpdateNavigationButtons()
        {
            FirstPageButton.IsEnabled = CurrentPage > 1;
            PreviousPageButton.IsEnabled = CurrentPage > 1;
            NextPageButton.IsEnabled = CurrentPage < TotalPages;
            LastPageButton.IsEnabled = CurrentPage < TotalPages;
        }

        private void UpdatePageInfo()
        {
            PageInfoText.Text = $"Sayfa {CurrentPage} / {TotalPages}";
        }

        private void UpdateItemInfo()
        {
            var startItem = (CurrentPage - 1) * PageSize + 1;
            var endItem = Math.Min(CurrentPage * PageSize, TotalItems);

            if (TotalItems == 0)
            {
                ItemInfoText.Text = "Hiç öğe yok";
            }
            else
            {
                ItemInfoText.Text = $"{startItem}-{endItem} / {TotalItems} öğe";
            }
        }

        private void UpdatePageNumbers()
        {
            PageNumbersPanel.Children.Clear();

            if (TotalPages <= 1) return;

            const int maxVisiblePages = 7;
            var startPage = Math.Max(1, CurrentPage - maxVisiblePages / 2);
            var endPage = Math.Min(TotalPages, startPage + maxVisiblePages - 1);

            // Adjust start page if end page is at the limit
            if (endPage - startPage < maxVisiblePages - 1)
            {
                startPage = Math.Max(1, endPage - maxVisiblePages + 1);
            }

            // Add "..." if there are pages before
            if (startPage > 1)
            {
                AddPageButton("1", 1);
                if (startPage > 2)
                {
                    AddEllipsis();
                }
            }

            // Add page numbers
            for (int i = startPage; i <= endPage; i++)
            {
                AddPageButton(i.ToString(), i, i == CurrentPage);
            }

            // Add "..." if there are pages after
            if (endPage < TotalPages)
            {
                if (endPage < TotalPages - 1)
                {
                    AddEllipsis();
                }
                AddPageButton(TotalPages.ToString(), TotalPages);
            }
        }

        private void AddPageButton(string content, int pageNumber, bool isActive = false)
        {
            var button = new Button
            {
                Content = content,
                Style = isActive ?
                    (Style)FindResource("ActivePageButtonStyle") :
                    (Style)FindResource("ModernPaginationButtonStyle"),
                Tag = pageNumber,
                MinWidth = 40
            };

            button.Click += PageNumberButton_Click;
            PageNumbersPanel.Children.Add(button);
        }

        private void AddEllipsis()
        {
            var ellipsis = new TextBlock
            {
                Text = "...",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush")
            };
            PageNumbersPanel.Children.Add(ellipsis);
        }

        private void UpdatePageSizeSelector()
        {
            var currentSelection = PageSizeComboBox.SelectedItem as ComboBoxItem;
            if (currentSelection == null || int.Parse(currentSelection.Tag.ToString()!) != PageSize)
            {
                // Find and select the appropriate page size
                foreach (ComboBoxItem item in PageSizeComboBox.Items)
                {
                    if (int.Parse(item.Tag.ToString()!) == PageSize)
                    {
                        PageSizeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
            }
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        private void PageNumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int pageNumber)
            {
                CurrentPage = pageNumber;
            }
        }

        private void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeComboBox.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Tag.ToString(), out int newPageSize))
            {
                // Güvenli üst sınır
                const int hardMax = 10000;
                if (newPageSize > hardMax) newPageSize = hardMax;
                if (newPageSize != PageSize)
                {
                    PageSize = newPageSize;
                }
            }
        }

        private void JumpToPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(JumpToPageTextBox.Text, out int pageNumber))
                {
                    if (pageNumber >= 1 && pageNumber <= TotalPages)
                    {
                        CurrentPage = pageNumber;
                    }
                    else
                    {
                        // Reset to current page if invalid
                        JumpToPageTextBox.Text = CurrentPage.ToString();
                    }
                }
                else
                {
                    // Reset to current page if invalid
                    JumpToPageTextBox.Text = CurrentPage.ToString();
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetData(int totalItems, int currentPage = 1, int pageSize = 50)
        {
            TotalItems = totalItems;
            PageSize = pageSize;
            CurrentPage = currentPage;
        }

        public (int Skip, int Take) GetPaginationParameters()
        {
            var skip = (CurrentPage - 1) * PageSize;
            var take = PageSize;
            return (skip, take);
        }

        #endregion
    }

    public class PaginationEventArgs : EventArgs
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int StartIndex => (CurrentPage - 1) * PageSize;
        public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalItems - 1);
    }
}