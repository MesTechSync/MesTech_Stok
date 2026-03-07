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
    /// Depo Y�netimi ViewModel'i - Zone, Rack, Shelf, Bin y�netimi
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
        private string _statusMessage = "Depo yap�s� y�kleniyor...";

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
                StatusMessage = "Depo yap�s� yenileniyor...";

                await LoadWarehouseStructureAsync();

                StatusMessage = "Depo yap�s� ba�ar�yla yenilendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depo yap�s� yenilenirken hata olu�tu");
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
                StatusMessage = "Yeni b�l�m ekleniyor...";

                var newZone = new WarehouseZone
                {
                    Name = "Yeni B�l�m",
                    Code = "NEW",
                    WarehouseId = Guid.NewGuid(),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdZone = await _locationService.CreateZoneAsync(newZone);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "B�l�m ba�ar�yla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B�l�m eklenirken hata olu�tu");
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

                StatusMessage = "Raf ba�ar�yla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raf eklenirken hata olu�tu");
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

                StatusMessage = "Seviye ba�ar�yla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seviye eklenirken hata olu�tu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddBinAsync()
        {
            try
            {
                StatusMessage = "Yeni g�z ekleniyor...";

                var newBin = new WarehouseBin
                {
                    Name = "Yeni G�z",
                    Code = "NEW",
                    ShelfId = 1,
                    BinNumber = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var createdBin = await _locationService.CreateBinAsync(newBin);

                // Refresh warehouse structure
                await LoadWarehouseStructureAsync();

                StatusMessage = "G�z ba�ar�yla eklendi";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "G�z eklenirken hata olu�tu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "De�i�iklikler kaydediliyor...";

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
                    StatusMessage = "De�i�iklikler ba�ar�yla kaydedildi";
                    IsEditing = false;

                    // Refresh warehouse structure
                    await LoadWarehouseStructureAsync();
                }
                else
                {
                    StatusMessage = "De�i�iklikler kaydedilemedi";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "De�i�iklikler kaydedilirken hata olu�tu");
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
                StatusMessage = "D�zenleme iptal edildi";

                // Reset selected items to original state
                // TODO: Implement reset logic
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "D�zenleme iptal edilirken hata olu�tu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Edit()
        {
            try
            {
                IsEditing = true;
                StatusMessage = "D�zenleme modu aktif";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "D�zenleme modu aktif edilirken hata olu�tu");
                StatusMessage = $"Hata: {ex.Message}";
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadWarehouseStructureAsync()
        {
            try
            {
                _logger.LogInformation("Depo yap�s� y�kleniyor...");

                WarehouseStructure.Clear();

                // Load zones
                var zones = await _locationService.GetWarehouseZonesAsync(Guid.Empty);

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

                _logger.LogInformation($"Depo yap�s� y�klendi: {zones.Count} b�l�m");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depo yap�s� y�klenirken hata olu�tu");
                throw;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Depo yap�s� node'u (TreeView i�in)
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
    /// Depo node t�r�
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