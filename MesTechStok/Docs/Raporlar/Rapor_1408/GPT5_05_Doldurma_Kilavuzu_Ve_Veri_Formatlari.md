## Ürün (Product) Örneği
```json
{
  "Name": "Basic T-Shirt L Siyah",
  "SKU": "MS-1001",
  "Barcode": "8690000000001",
  "CategoryId": 1,
  "WarehouseId": 1,
  "PurchasePrice": 120.00,
  "SalePrice": 189.90,
  "TaxRate": 18,
  "Stock": 45,
  "MinimumStock": 5,
  "ReorderLevel": 12,
  "Brand": "MesChain",
  "Color": "Siyah",
  "Size": "L",
  "SyncWithOpenCart": true
}
```

## Sipariş (Order) Örneği
```json
{
  "OrderNumber": "MS-202508-20001",
  "CustomerId": 1,
  "Status": "Processing",
  "OrderDate": "2025-08-12T10:43:21Z",
  "Items": [
    { "ProductId": 15, "Quantity": 2, "UnitPrice": 249.90, "TaxRate": 18 }
  ]
}
```

## Telemetri Kayıtları
- `ApiCallLogs`: `Endpoint`, `Method`, `Success`, `StatusCode`, `Category`, `DurationMs`, `TimestampUtc`, `CorrelationId`.
- `CircuitStateLogs`: `PreviousState`, `NewState`, `Reason`, `FailureRate`, `WindowTotalCalls`, `TransitionTimeUtc`.

## Konfigürasyon Örneği
```json
{
  "OpenCartSettings": {
    "ApiUrl": "https://allmes.store",
    "ApiKey": "<gizli>",
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30
  },
  "Resilience": {
    "CircuitBreaker": {
      "FailRateThreshold": 0.2,
      "SlidingWindowSeconds": 60,
      "OpenStateDurationSeconds": 120,
      "HalfOpenMaxCalls": 10,
      "MinimumThroughput": 20
    },
    "Retry": { "BackoffSeconds": [1,2,4,8,16] }
  }
}
```
