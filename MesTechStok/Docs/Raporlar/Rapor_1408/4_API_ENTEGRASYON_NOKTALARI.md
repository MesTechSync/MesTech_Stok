# 4. API ENTEGRASYON NOKTALARI - .NET HTTP CLİENTS

**Claude Rapor Tarihi:** 14 Ağustos 2025  
**Kaynak:** MesTechStok .NET API Architecture Analysis  
**Teknoloji:** System.Net.Http + Azure SDK + OpenCart API  

---

## 🌐 GERÇEK API ENTEGRASYON MİMARİSİ

### .NET HTTP Client Stack

```csharp
// MesTechStok.Core dependencies for API integration
<PackageReference Include="System.Net.Http" Version="4.3.4" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.6" />
```

---

## 🔗 1. AZURE AI SERVİSLERİ ENTEGRASYONU

### OpenAI API Client (.NET Implementation)

```csharp
// Azure OpenAI Service implementation for product categorization
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;
    
    public AzureOpenAIService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("AzureOpenAI");
        _configuration = config;
    }
    
    public async Task<string> CategorizeProductAsync(string productName, string description)
    {
        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { 
                    role = "system", 
                    content = "Sen bir ürün kategorizasyon uzmanısın. Ürünleri standart kategorilere ayırıyorsun." 
                },
                new { 
                    role = "user", 
                    content = $"Bu ürünü kategorize et: {productName} - {description}" 
                }
            },
            max_tokens = 100,
            temperature = 0.3
        };
        
        try
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            
            _logger.LogInformation("Ürün kategorize edildi: {Product} -> {Category}", 
                productName, result.Choices[0].Message.Content);
                
            return result.Choices[0].Message.Content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI API hatası: {Product}", productName);
            return "Genel"; // Fallback category
        }
    }
}
```

### Azure AI Configuration

```csharp
// appsettings.json - API configuration needed
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "DeploymentName": "gpt-4"
  },
  "Azure": {
    "CognitiveServices": {
      "VisionApiKey": "YOUR_VISION_API_KEY",
      "VisionEndpoint": "https://your-region.api.cognitive.microsoft.com/"
    }
  }
}
```

---

## 🛒 2. OPENCART API ENTEGRASYONU

### OpenCart REST Client Implementation

```csharp
// OpenCart API service for e-commerce integration
public class OpenCartApiService : IOpenCartApiService
{
    private readonly HttpClient _httpClient;
    private readonly OpenCartConfig _config;
    private readonly ILogger<OpenCartApiService> _logger;
    
    public OpenCartApiService(IHttpClientFactory httpClientFactory, IOptions<OpenCartConfig> config)
    {
        _config = config.Value;
        _httpClient = httpClientFactory.CreateClient("OpenCart");
        
        // API authentication setup
        _httpClient.DefaultRequestHeaders.Add("X-Oc-Merchant-Id", _config.MerchantId);
        _httpClient.DefaultRequestHeaders.Add("X-Oc-Merchant-Language", "tr-tr");
    }
    
    public async Task<bool> SyncProductToOpenCartAsync(Product product)
    {
        var openCartProduct = new
        {
            name = new { ["1"] = product.Name }, // Language ID 1 for Turkish
            description = new { ["1"] = product.Description },
            model = product.SKU,
            sku = product.SKU,
            upc = product.UPC,
            ean = product.EAN,
            price = product.SalePrice.ToString("F2"),
            quantity = product.Stock,
            minimum = product.MinimumStock,
            status = product.IsActive ? "1" : "0",
            category = new[] { GetCategoryId(product.Category) }
        };
        
        try
        {
            var json = JsonConvert.SerializeObject(openCartProduct);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/api/rest/products", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OpenCartResponse>(responseJson);
            
            if (result.Success)
            {
                product.OpenCartId = result.Data.ProductId;
                _logger.LogInformation("Ürün OpenCart'a senkronize edildi: {ProductId}", result.Data.ProductId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart sync hatası: {Product}", product.Name);
            return false;
        }
    }
    
    public async Task<IEnumerable<OpenCartOrder>> GetNewOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rest/orders?status=pending");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<OpenCartOrder>>(json);
            
            return orders ?? new List<OpenCartOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart sipariş getirme hatası");
            return new List<OpenCartOrder>();
        }
    }
}
```

---

## 🔧 3. HTTP CLIENT FACTORY KONFİGÜRASYONU

### Dependency Injection Setup

```csharp
// App.xaml.cs - HTTP client configuration
public void ConfigureServices(IServiceCollection services)
{
    // Azure OpenAI HTTP Client
    services.AddHttpClient("AzureOpenAI", client =>
    {
        client.BaseAddress = new Uri(configuration["AzureOpenAI:Endpoint"]);
        client.DefaultRequestHeaders.Add("api-key", configuration["AzureOpenAI:ApiKey"]);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    
    // OpenCart API HTTP Client  
    services.AddHttpClient("OpenCart", client =>
    {
        client.BaseAddress = new Uri(configuration["OpenCart:ApiUrl"]);
        client.DefaultRequestHeaders.Add("X-Oc-Rest-Key", configuration["OpenCart:RestKey"]);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    
    // Polly retry policy for resilience
    services.AddHttpClient<OpenCartApiService>()
        .AddPolicyHandler(GetRetryPolicy());
    
    // API service registrations
    services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
    services.AddScoped<IOpenCartApiService, OpenCartApiService>();
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"API retry {retryCount} after {timespan} seconds");
            });
}
```

---

## 📊 4. VERİ FORMATLARI VE SERİALİZASYON

### Product Sync Data Models

```csharp
// API request/response models
public class OpenCartProduct
{
    [JsonProperty("product_id")]
    public int ProductId { get; set; }
    
    [JsonProperty("name")]
    public Dictionary<int, string> Name { get; set; } = new();
    
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonProperty("price")]
    public string Price { get; set; } = "0.00";
    
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; } = "1";
}

public class AzureVisionAnalysis
{
    [JsonProperty("categories")]
    public List<Category> Categories { get; set; } = new();
    
    [JsonProperty("description")]
    public Description Description { get; set; } = new();
    
    [JsonProperty("requestId")]
    public string RequestId { get; set; } = string.Empty;
}
```

---

## 🚨 GERÇEK DURUM ANALİZİ

### **Mevcut API Altyapısı:**
- ✅ **HTTP Client dependencies** mevcut
- ✅ **Newtonsoft.Json** serialization ready
- ✅ **IHttpClientFactory** pattern implementable
- ❌ **API service implementations** eksik
- ❌ **Configuration** tamamen eksik
- ❌ **Authentication** setup yok

### **MainViewModel'de API References:**
```csharp
// MainViewModel.cs - OpenCart integration planned but not implemented
[ObservableProperty]
private string openCartUrl = "https://example.com/api";

[ObservableProperty] 
private string openCartApiKey = "your_api_key_here";

[ObservableProperty]
private string openCartStatus = "Bağlı değil";
```

---

## 🎯 API ENTEGRASYON ÖNCELİKLERİ

| API Service | Öncelik | Implementation Status | Required Action |
|-------------|---------|----------------------|-----------------|
| **OpenCart REST** | Critical | Planned, not implemented | Full implementation needed |
| **Azure OpenAI** | High | Framework ready | API key + implementation |
| **Azure Vision** | Medium | Dependencies ready | Service implementation |
| **Barcode Validation** | High | System.IO.Ports ready | Serial integration |

---

## 📋 IMPLEMENTASYOeN PLANI

1. **Phase 1:** OpenCart API client implementation
2. **Phase 2:** Azure AI services integration
3. **Phase 3:** Error handling + retry policies
4. **Phase 4:** Real-time sync mechanisms
5. **Phase 5:** API monitoring + logging

Bu API entegrasyon analizi, projenin **mevcut HTTP altyapısını** ve **eksik implementasyonları** detaylandırmaktadır.
