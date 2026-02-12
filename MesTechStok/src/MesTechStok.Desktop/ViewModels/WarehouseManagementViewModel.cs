using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Services.Abstract;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.ViewModels
{
    /// <summary>
    /// Depo Yönetimi ViewModel'i - Zone, Rack, Shelf, Bin yönetimi
    /// </summary>
    public partial class WarehouseManagementViewModel : ObservableObject
    {
        private readonly ILogger<WarehouseManagementViewModel> _logger;
        private readonly ILocationService _locationService;

        public WarehouseManagementViewModel(
            ILogger<WarehouseManagementViewModel> logger,
            ILocationService locationService)
        {
            _logger = logger;
            _locationService = locationService;

            // Initialize collections
            WarehouseStructure = new ObservableCollection<WarehouseNode>();

            // Load initial data
            _ = LoadWarehouseStructureAsync();
        }

        #region Observable Properties

        [ObservableProperty]
        private string _statusMessage = "Depo yapýsý yükleniyor...";

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private bool _isItemSelected = false;

        [ObservableProperty]
        private string _selectedItemName = "";

        [ObservableProperty]
        private string _selectedItemType = "";

        [ObservableProperty]
        private bool _isEditing = false;

        #endregion

        #region Collections

        public ObservableCollection<WarehouseNode> WarehouseStructure { get; }

        #endregion

        #region Selected Items

        [ObservableProperty]
        private WarehouseZone? _selectedZone;

        [ObservableProperty]
        private WarehouseRack? _selectedRack;

        [ObservableProperty]
        private WarehouseShelf? _selectedShelf;

        [ObservableProperty]
        private WarehouseBin? _selectedBin;

        #endregion

        #region Visibility Properties

        public bool ZoneDetailsVisibility => SelectedZone != null && !IsEditing;
        public bool RackDetailsVisibility => SelectedRack != null && !IsEditing;
        public bool ShelfDetailsVisibility => SelectedShelf != null && !IsEditing;
        public bool BinDetailsVisibility => SelectedBin != null && !IsEditing;
        public bool NoSelectionVisibility => !IsItemSelected;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task RefreshWarehouseStructureAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Depo yapýsý yenileniyor...";

                await LoadWarehouseStructureAsync();

                StatusMessage = "Depo yapýsý baþarýyla yenilendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depo yapýsý yenilenirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddZoneAsync()
        {
            try
            {
                StatusMessage = "Yeni bölüm ekleniyor...";

                var newZone = new WarehouseZone
                {
                    Name = "Yeni Bölüm",
                    Code = "NEW",
                    WarehouseId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdZone = await _locationService.CreateZoneAsync(newZone);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "Bölüm baþarýyla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bölüm eklenirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddRackAsync()
        {
            try
            {
                StatusMessage = "Yeni raf ekleniyor...";

                var newRack = new WarehouseRack
                {
                    Name = "Yeni Raf",
                    Code = "NEW",
                    ZoneId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdRack = await _locationService.CreateRackAsync(newRack);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "Raf baþarýyla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raf eklenirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddShelfAsync()
        {
            try
            {
                StatusMessage = "Yeni seviye ekleniyor...";

                var newShelf = new WarehouseShelf
                {
                    Name = "Yeni Seviye",
                    Code = "NEW",
                    RackId = 1,
                    LevelNumber = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdShelf = await _locationService.CreateShelfAsync(newShelf);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "Seviye baþarýyla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seviye eklenirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddBinAsync()
        {
            try
            {
                StatusMessage = "Yeni göz ekleniyor...";

                var newBin = new WarehouseBin
                {
                    Name = "Yeni Göz",
                    Code = "NEW",
                    ShelfId = 1,
                    BinNumber = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdBin = await _locationService.CreateBinAsync(newBin);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "Göz baþarýyla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Göz eklenirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Deðiþiklikler kaydediliyor...";

                bool success = false;

                if (SelectedZone != null)
                {
                    success = await _locationService.UpdateZoneAsync(SelectedZone);
                }
                else if (SelectedRack != null)
                {
                    success = await _locationService.UpdateRackAsync(SelectedRack);
                }
                else if (SelectedShelf != null)
                {
                    success = await _locationService.UpdateShelfAsync(SelectedShelf);
                }
                else if (SelectedBin != null)
                {
                    success = await _locationService.UpdateBinAsync(SelectedBin);
                }

                if (success)
                {
                    StatusMessage = "Deðiþiklikler baþarýyla kaydedildi";
                    IsEditing = false;

                    // Refresh warehouse structure
                    await LoadWarehouseStructureAsync();
                }
                else
                {
                    StatusMessage = "Deðiþiklikler kaydedilemedi";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deðiþiklikler kaydedilirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            try
            {
                IsEditing = false;
                StatusMessage = "Düzenleme iptal edildi";

                // Reset selected items to original state
                // TODO: Implement reset logic
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düzenleme iptal edilirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Edit()
        {
            try
            {
                IsEditing = true;
                StatusMessage = "Düzenleme modu aktif";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düzenleme modu aktif edilirken hata oluþtu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadWarehouseStructureAsync()
        {
            try
            {
                _logger.LogInformation("Depo yapýsý yükleniyor...");

                WarehouseStructure.Clear();

                // Load zones
                var zones = await _locationService.GetWarehouseZonesAsync(1);

                foreach (var zone in zones)
                {
                    var zoneNode = new WarehouseNode
                    {
                        Id = zone.Id,
                        Name = zone.Name,
                        Code = zone.Code,
                        Type = WarehouseNodeType.Zone,
                        Icon = "???",
                        Children = new ObservableCollection<WarehouseNode>()
                    };

                    // Load racks for this zone
                    var racks = await _locationService.GetRacksByZoneAsync(zone.Id);

                    foreach (var rack in racks)
                    {
                        var rackNode = new WarehouseNode
                        {
                            Id = rack.Id,
                            Name = rack.Name,
                            Code = rack.Code,
                            Type = WarehouseNodeType.Rack,
                            Icon = "??",
                            Children = new ObservableCollection<WarehouseNode>()
                        };

                        // Load shelves for this rack
                        var shelves = await _locationService.GetShelvesByRackAsync(rack.Id);

                        foreach (var shelf in shelves)
                        {
                            var shelfNode = new WarehouseNode
                            {
                                Id = shelf.Id,
                                Name = shelf.Name,
                                Code = shelf.Code,
                                Type = WarehouseNodeType.Shelf,
                                Icon = "??",
                                Children = new ObservableCollection<WarehouseNode>()
                            };

                            // Load bins for this shelf
                            var bins = await _locationService.GetBinsByShelfAsync(shelf.Id);

                            foreach (var bin in bins)
                            {
                                var binNode = new WarehouseNode
                                {
                                    Id = bin.Id,
                                    Name = bin.Name,
                                    Code = bin.Code,
                                    Type = WarehouseNodeType.Bin,
                                    Icon = "???",
                                    Children = new ObservableCollection<WarehouseNode>()
                                };

                                shelfNode.Children.Add(binNode);
                            }

                            rackNode.Children.Add(shelfNode);
                        }

                        zoneNode.Children.Add(rackNode);
                    }

                    WarehouseStructure.Add(zoneNode);
                }

                _logger.LogInformation($"Depo yapýsý yüklendi: {zones.Count} bölüm");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depo yapýsý yüklenirken hata oluþtu");
                throw;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Depo yapýsý node'u (TreeView için)
    /// </summary>
    public class WarehouseNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public WarehouseNodeType Type { get; set; }
        public string Icon { get; set; } = string.Empty;
        public ObservableCollection<WarehouseNode> Children { get; set; } = new ObservableCollection<WarehouseNode>();
    }

    /// <summary>
    /// Depo node türü
    /// </summary>
    public enum WarehouseNodeType
    {
        Zone,
        Rack,
        Shelf,
        Bin
    }

    #endregion
}