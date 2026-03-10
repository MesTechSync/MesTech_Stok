import os
import re
import sys

# Files with potential empty catch blocks
files_to_check = [
    "MesTechStok.Desktop/App.xaml.cs",
    "MesTechStok.Core/Data/AppDbContext.cs",
    "MesTechStok.Desktop/Views/CargoShipmentView.xaml.cs",
    "MesTechStok.Desktop/Views/CategoryManagerDialog.xaml.cs",
    "MesTechStok.Desktop/Views/ProductEditDialog.xaml.cs",
    "MesTechStok.Desktop/MainWindow.xaml.cs",
    "MesTechStok.Desktop/Views/ImageMapWizard.xaml.cs",
    "MesTechStok.Desktop/Services/SqlBackedProductService.cs",
    "MesTechStok.Desktop/Services/BarcodeHardwareService.cs",
    "MesTechStok.Desktop/Views/SettingsView.xaml.cs",
    "MesTech.Tests.Integration/Regression/BuildRegressionTests.cs",
    "MesTech.Tests.Integration/UI/_Shared/DesktopAppFixture.cs",
    "MesTech.Tests.Integration/Smoke/StartupPerformanceTests.cs",
    "MesTechStok.Desktop/Services/EnhancedProductService.cs",
    "MesTechStok.Desktop/Views/CustomerEditPopup.xaml.cs",
    "MesTechStok.Core/Integrations/OpenCart/OpenCartSyncService.cs",
    "MesTechStok.Desktop/Views/ProductImageViewer.xaml.cs",
    "MesTechStok.Desktop/Views/OrdersView.xaml.cs",
    "MesTechStok.Desktop/Views/OpenCartView.xaml.cs",
    "MesTechStok.Desktop/Views/MarginToBrushConverter.cs",
    "MesTechStok.Desktop/Views/InventoryView.xaml.cs",
    "MesTechStok.Desktop/Views/IndexEqualsVisibilityConverter.cs",
    "MesTechStok.Desktop/Views/ExportsView.xaml.cs",
    "MesTechStok.Desktop/Views/CustomersView.xaml.cs",
    "MesTechStok.Desktop/Utils/GlobalLogger.cs",
    "MesTechStok.Desktop/Utils/EventBus.cs",
    "MesTechStok.Desktop/Services/ProductUploadWindowManager.cs",
    "MesTechStok.Core/Services/SyncRetryService.cs",
    "MesTechStok.Core/Services/Security/TokenRotationService.cs",
    "MesTechStok.Core/Integrations/Barcode/HidBarcodeListener.cs",
    "MesTechStok.Core/Integrations/Barcode/BarcodeScannerService.cs",
]

results = {}

for file_path in files_to_check:
    full_path = os.path.join(os.getcwd(), file_path)
    if not os.path.exists(full_path):
        continue
    
    try:
        with open(full_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Pattern 1: catch { }
        pattern1 = r'catch\s*\{\s*\}'
        # Pattern 2: catch ( ... ) { }
        pattern2 = r'catch\s*\([^)]*\)\s*\{\s*\}'
        
        matches1 = len(re.findall(pattern1, content))
        matches2 = len(re.findall(pattern2, content))
        total = matches1 + matches2
        
        if total > 0:
            results[file_path] = total
    except:
        pass

# Print results
for file_path, count in sorted(results.items(), key=lambda x: -x[1]):
    print(f"{count}|{file_path}")
