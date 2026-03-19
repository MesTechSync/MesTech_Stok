# EMR-18 Adapter URL/Auth Dogrulama Raporu

**Tarih:** 2026-03-19
**Hazirlayan:** DEV 4 (DevOps & Security)
**Durum:** Read-only analiz tamamlandi

---

## 1. Pazaryeri Adapter Dogrulama Tablosu

| # | Platform | Beklenen URL | Kodda Bulunan URL | Auth Type | Polly Retry | Circuit Breaker | Sandbox Toggle | Dogru? |
|---|----------|-------------|-------------------|-----------|-------------|-----------------|----------------|--------|
| 1 | **Trendyol** | `apigw.trendyol.com` | `https://apigw.trendyol.com` (TrendyolOptions.ProductionBaseUrl) | Basic Auth (ApiKey:ApiSecret base64) | 5 retry (429) + 3 retry (5xx) | 50% fail / 30s | UseSandbox -> `stage-apigw.trendyol.com` | DOGRU |
| 2 | **Hepsiburada** | `mpop.hepsiburada.com` | BaseUrl credentials uzerinden (default yok, BaseAddress set edilmiyor) | OAuth2 (HepsiburadaTokenService) + fallback static Bearer | 3 retry (5xx) | 50% fail / 30s | BaseUrl override | UYARI: Default BaseUrl yok |
| 3 | **Ciceksepeti** | `seller-api.ciceksepeti.com` | BaseUrl credentials uzerinden (default yok) | x-api-key header | 3 retry (5xx) | 50% fail / 30s | BaseUrl override | UYARI: Default BaseUrl yok |
| 4 | **N11** | `api.n11.com` | `https://api.n11.com` (TestConnectionAsync default) | SOAP (AppKey + AppSecret in SOAP body) | YOK (SimpleSoapClient) | YOK | soapBaseUrl parametresi | DOGRU |
| 5 | **Pazarama** | `isortagimgiris.pazarama.com` | Token: `https://isortagimgiris.pazarama.com/connect/token` | OAuth2 Client Credentials | 3 retry (5xx) | 50% fail / 30s | BaseUrl override | DOGRU |
| 6 | **Amazon TR** | `sellingpartnerapi-eu.amazon.com` | `https://sellingpartnerapi-eu.amazon.com` (EuEndpoint const) | SP-API LWA OAuth2 (refresh_token grant) | 3 retry (5xx) | 50% fail / 30s | BaseUrl + LwaEndpoint override | DOGRU |
| 7 | **Amazon EU** | `sellingpartnerapi-eu.amazon.com` | `https://sellingpartnerapi-eu.amazon.com` (EuEndpoint const) | SP-API LWA OAuth2 (refresh_token grant) | 3 retry (5xx) | 50% fail / 30s | BaseUrl + LwaEndpoint override | DOGRU |
| 8 | **eBay** | `api.ebay.com` | `https://api.ebay.com` (EbayOptions.ProductionBaseUrl) | OAuth2 Client Credentials (Basic auth for token) | YOK (direct HttpClient) | YOK | UseSandbox -> `api.sandbox.ebay.com` | UYARI: Polly yok |
| 9 | **Ozon** | `api-seller.ozon.ru` | `https://api-seller.ozon.ru` (field default) | Client-Id + Api-Key headers | YOK (direct HttpClient) | YOK | BaseUrl override | UYARI: Polly yok |
| 10 | **PttAVM** | `apigw.pttavm.com` | `https://apigw.pttavm.com` (field default) | Username/Password -> Bearer token exchange | YOK (direct HttpClient) | YOK | BaseUrl + TokenEndpoint override | UYARI: Polly yok |
| 11 | **OpenCart** | Kullanici tanimli | BaseUrl credentials ile set | REST API Key (X-Oc-Restadmin-Id header) | 3 retry (5xx + 429) | YOK (sadece retry) | BaseUrl credentials | DOGRU |

---

## 2. Kargo Adapter Dogrulama Tablosu

| # | Kargo Firma | Auth Type | Polly Retry | URL Kaynagi | Notlar |
|---|-------------|-----------|-------------|-------------|--------|
| 1 | **Yurtici Kargo** | SOAP (Username/Password XML body) | YOK (SimpleSoapClient) | Options.ServiceUrl / credentials ServiceUrl | SandboxServiceUrl toggle mevcut |
| 2 | **Aras Kargo** | Basic Auth (REST) | 3 retry + Circuit Breaker | credentials BaseUrl | Polly TAM |
| 3 | **Surat Kargo** | Basic Auth (REST) | 3 retry + Circuit Breaker | credentials BaseUrl | Polly TAM |
| 4 | **MNG Kargo** | (Polly kullanir) | Evet | credentials BaseUrl | Polly TAM |
| 5 | **HepsiJet** | (Polly kullanir) | Evet | credentials BaseUrl | Polly TAM |
| 6 | **Sendeo** | (Polly kullanir) | Evet | credentials BaseUrl | Polly TAM |
| 7 | **PTT Kargo** | (dosyada Polly yok) | Kontrol gerekli | credentials BaseUrl | - |

---

## 3. E-Fatura Provider Dogrulama Tablosu

| # | Provider | Auth Type | URL Kaynagi | Polly | Notlar |
|---|----------|-----------|-------------|-------|--------|
| 1 | **Sovos** | Bearer token (apiKey) | Configure(apiKey, baseUrl) | YOK | Sari hat: Polly eklenmeli |
| 2 | **e-Logo** | Bearer token (apiKey) | Configure(apiKey, baseUrl) | YOK | Sari hat: Polly eklenmeli |
| 3 | **Parasut** | ParasutERPAdapter uzerinden | ERP adapter | - | Invoice adapter delegasyonu |

---

## 4. ERP Adapter Dogrulama

| # | ERP | Auth Type | URL Kaynagi | Polly |
|---|-----|-----------|-------------|-------|
| 1 | **Parasut** | OAuth2 | credentials BaseUrl | Kontrol gerekli |
| 2 | **Logo** | - | - | Kontrol gerekli |
| 3 | **Netsis** | - | - | Kontrol gerekli |
| 4 | **BizimHesap** | - | - | Kontrol gerekli |

---

## 5. Polly Resilience Durum Ozeti

### TAM (Retry + Circuit Breaker):
- TrendyolAdapter (5 retry 429 + 3 retry 5xx + CB)
- HepsiburadaAdapter (3 retry + CB + 401 token refresh)
- CiceksepetiAdapter (3 retry + CB)
- N11Adapter (SOAP, no Polly -- SimpleSoapClient)
- PazaramaAdapter (3 retry + CB)
- AmazonTrAdapter (3 retry + CB)
- AmazonEuAdapter (3 retry + CB)
- ArasKargoAdapter (3 retry + CB)
- SuratKargoAdapter (3 retry + CB)
- MngKargoAdapter (Polly mevcut)
- HepsiJetCargoAdapter (Polly mevcut)
- SendeoCargoAdapter (Polly mevcut)
- OpenCartAdapter (3 retry, CB yok)

### EKSIK (Polly yok):
- **EbayAdapter** -- direct HttpClient, retry/CB yok
- **OzonAdapter** -- direct HttpClient, retry/CB yok
- **PttAvmAdapter** -- direct HttpClient, retry/CB yok
- **YurticiKargoAdapter** -- SimpleSoapClient, retry yok
- **SovosInvoiceProvider** -- direct HttpClient, retry yok
- **ELogoInvoiceProvider** -- direct HttpClient, retry yok

---

## 6. Guvenlik Notlari

### Hardcoded Secret Kontrolu
- **TEMIZ**: Hicbir adapter dosyasinda hardcoded API key, secret veya password bulunmadi.
- Tum credential'lar `Dictionary<string, string> credentials` parametresi veya `IOptions<T>` uzerinden alinmaktadir.

### Sandbox/Production Ayirimi
- **Trendyol**: `TrendyolOptions.UseSandbox` + credentials `UseSandbox=true` kisa yolu
- **eBay**: `EbayOptions.UseSandbox` + credentials `UseSandbox=true` kisa yolu
- **Amazon TR/EU**: `BaseUrl` + `LwaEndpoint` override
- **N11**: `soapBaseUrl` parametresi
- **Diger**: `BaseUrl` credentials uzerinden override

### Rate Limiting
| Adapter | SemaphoreSlim Limiti | Aciklama |
|---------|---------------------|----------|
| Trendyol | 100 + 10ms delay | 50 req/10s API limiti |
| Hepsiburada | 20 | Orta hiz |
| Ciceksepeti | 10 | Dusuk hiz |
| Pazarama | 10 | Dusuk hiz |
| Aras Kargo | 15 | Orta hiz |
| Surat Kargo | 10 | Dusuk hiz |
| OpenCart | 5 (batch) | Toplu islemlerde |

---

## 7. Bulgular ve Oneriler

### KRITIK (Hemen Aksiyon):
1. **Hepsiburada ve Ciceksepeti adapterleri default BaseUrl tanimlamiyor.** HttpClient.BaseAddress null kalabilir. Relative URI kullanildiginda calisir ama acik bir default URL ayarlanmali.

### ORTA (Sonraki Dalga):
2. **eBay, Ozon, PttAVM adapterlerinde Polly yok.** Production'da 5xx/timeout durumlarinda retry yapilmiyor. Ayni Polly pipeline pattern'i eklenmeli.
3. **Sovos ve e-Logo e-fatura providerlarinda Polly yok.** Fatura gonderim hatalari retry edilmiyor.
4. **N11 SOAP adapter SimpleSoapClient uzerinden calisiyor, retry yok.** SOAP timeout/5xx durumlarinda tekrar deneme yapilmiyor.

### DUSUK (Iyilestirme):
5. Trendyol 429 retry'da Retry-After header parse ediliyor (cok iyi).
6. Hepsiburada 401 token refresh + retry mevcut (K1c-04 pattern, cok iyi).
7. Tum adapterlerde `EnsureConfigured()` guard metodu var (guvenli).
8. `IPingableAdapter` implementasyonu 5 platformda mevcut (Trendyol, Amazon TR/EU, eBay, Ozon, PttAVM).

---

## 8. Credential Key Referansi

| Platform | Credential Key | Zorunlu? | Aciklama |
|----------|---------------|----------|----------|
| Trendyol | ApiKey, ApiSecret, SupplierId | Evet | Basic Auth |
| Trendyol | BaseUrl, UseSandbox | Hayir | URL override |
| Hepsiburada | MerchantId, ApiKey | Evet | Bearer / OAuth2 |
| Ciceksepeti | ApiKey | Evet | x-api-key |
| N11 | N11AppKey, N11AppSecret, N11BaseUrl | Evet | SOAP body |
| Pazarama | PazaramaClientId, PazaramaClientSecret | Evet | OAuth2 CC |
| Amazon TR/EU | RefreshToken, ClientId, ClientSecret, SellerId | Evet | LWA OAuth2 |
| Amazon EU | MarketplaceCountry | Hayir | DE/FR/IT/ES/NL/SE/PL |
| eBay | ClientId, ClientSecret | Evet | OAuth2 CC |
| eBay | TokenEndpoint, BaseUrl, UseSandbox | Hayir | URL override |
| Ozon | ClientId, ApiKey | Evet | Header auth |
| PttAVM | Username, Password | Evet | Bearer token exchange |
| OpenCart | ApiToken, BaseUrl | Evet | X-Oc-Restadmin-Id |

---

*Bu rapor read-only analiz sonucu olusturulmustur. Hicbir dosya degistirilmemistir.*
