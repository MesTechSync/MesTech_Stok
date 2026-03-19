# EMR-18 Sandbox Test Script

**Tarih:** 2026-03-19
**Hazirlayan:** DEV 4 (DevOps & Security)
**Amac:** Her platform icin canli baglanti testi curl komutlari

> **UYARI:** Asagidaki komutlarda `<PLACEHOLDER>` degerlerini gercek credential'larla degistirin.
> Production credential'lari bu dosyaya YAZMAYIN.

---

## 1. Trendyol (12 Adim)

```bash
# Degiskenler
TRENDYOL_API_KEY="<API_KEY>"
TRENDYOL_API_SECRET="<API_SECRET>"
TRENDYOL_SUPPLIER_ID="<SUPPLIER_ID>"
TRENDYOL_BASE="https://apigw.trendyol.com"
TRENDYOL_AUTH=$(echo -n "${TRENDYOL_API_KEY}:${TRENDYOL_API_SECRET}" | base64)

# Adim 1: Kategoriler
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  -H "User-Agent: MesTech-Trendyol-Client/3.0" \
  "${TRENDYOL_BASE}/integration/product/product-categories" | tail -1
# Beklenen: 200

# Adim 2: Markalar (arama)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/product/brands?name=test" | tail -1
# Beklenen: 200

# Adim 3: Urunleri Cek (sayfa 0, 1 urun)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/product/sellers/${TRENDYOL_SUPPLIER_ID}/products?page=0&size=1" | tail -1
# Beklenen: 200

# Adim 4: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/order/sellers/${TRENDYOL_SUPPLIER_ID}/orders?page=0&size=1" | tail -1
# Beklenen: 200

# Adim 5: Iade Talepleri
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/order/sellers/${TRENDYOL_SUPPLIER_ID}/claims?page=0&size=1" | tail -1
# Beklenen: 200

# Adim 6: Hesap Ekstresi (son 7 gun)
START_DATE=$(date -d "-7 days" +%s)000
END_DATE=$(date +%s)000
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/finance/sellers/${TRENDYOL_SUPPLIER_ID}/settlement?startDate=${START_DATE}&endDate=${END_DATE}" | tail -1
# Beklenen: 200

# Adim 7: Musteri Sorulari
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/product/sellers/${TRENDYOL_SUPPLIER_ID}/questions?page=0&size=1" | tail -1
# Beklenen: 200

# Adim 8: Kategori Ozellikleri (categoryId=387)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic ${TRENDYOL_AUTH}" \
  "${TRENDYOL_BASE}/integration/product/product-categories/387/attributes" | tail -1
# Beklenen: 200

# Adim 9: API Status (health check)
curl -s -w "\n%{http_code} %{time_total}s" \
  "${TRENDYOL_BASE}/integration/product/api-status" | tail -1
# Beklenen: 200 (auth gereksiz)

# Adim 10: Yanlis Credential Testi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Basic dGVzdDp0ZXN0" \
  "${TRENDYOL_BASE}/integration/product/sellers/00000/products?page=0&size=1" | tail -1
# Beklenen: 401

# Adim 11: Stok/Fiyat Guncelleme (POST - dikkatli kullanin)
# curl -s -w "\n%{http_code} %{time_total}s" \
#   -X POST \
#   -H "Authorization: Basic ${TRENDYOL_AUTH}" \
#   -H "Content-Type: application/json" \
#   -d '{"items":[{"barcode":"TEST-BARCODE","quantity":0}]}' \
#   "${TRENDYOL_BASE}/integration/inventory/sellers/${TRENDYOL_SUPPLIER_ID}/products/price-and-inventory"

# Adim 12: Batch Request Sonucu (batchId gerekli)
# curl -s -w "\n%{http_code} %{time_total}s" \
#   -H "Authorization: Basic ${TRENDYOL_AUTH}" \
#   "${TRENDYOL_BASE}/integration/product/sellers/${TRENDYOL_SUPPLIER_ID}/products/batch-requests/<BATCH_ID>"
```

---

## 2. Hepsiburada (8 Adim)

```bash
# Degiskenler — OAuth2 token once alinmali
HB_MERCHANT_ID="<MERCHANT_ID>"
HB_API_KEY="<API_KEY>"
# Not: Gercek ortamda HepsiburadaTokenService OAuth2 token alir.
# Test icin static Bearer kullanilabilir.
HB_TOKEN="${HB_MERCHANT_ID}:${HB_API_KEY}"
HB_BASE="https://mpop.hepsiburada.com"

# Adim 1: Listing Sorgula
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${HB_TOKEN}" \
  -H "User-Agent: MesTech-Hepsiburada-Client/3.0" \
  "${HB_BASE}/listings/merchantid/${HB_MERCHANT_ID}?limit=1&offset=0" | tail -1
# Beklenen: 200

# Adim 2: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${HB_TOKEN}" \
  "${HB_BASE}/orders/merchantid/${HB_MERCHANT_ID}?limit=1&offset=0" | tail -1
# Beklenen: 200

# Adim 3: Claim Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${HB_TOKEN}" \
  "${HB_BASE}/claims" | tail -1
# Beklenen: 200

# Adim 4: Komisyon Bilgileri
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${HB_TOKEN}" \
  "${HB_BASE}/finance/commissions?startDate=2026-03-01&endDate=2026-03-19" | tail -1
# Beklenen: 200

# Adim 5-8: Listing aktivasyon, deaktivasyon, fatura, kargo etiketi
# (Veri degistiren islemler -- dikkatli kullanin)
```

---

## 3. Ciceksepeti (6 Adim)

```bash
CS_API_KEY="<API_KEY>"
CS_BASE="https://seller-api.ciceksepeti.com"

# Adim 1: Urun Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-api-key: ${CS_API_KEY}" \
  -H "User-Agent: MesTech-Ciceksepeti-Client/3.0" \
  "${CS_BASE}/api/v1/Products?PageSize=1&Page=1" | tail -1
# Beklenen: 200

# Adim 2: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-api-key: ${CS_API_KEY}" \
  "${CS_BASE}/api/v1/Order?PageSize=1&Page=1" | tail -1
# Beklenen: 200

# Adim 3: Kategori Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-api-key: ${CS_API_KEY}" \
  "${CS_BASE}/api/v1/Categories" | tail -1
# Beklenen: 200

# Adim 4: Iade Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-api-key: ${CS_API_KEY}" \
  "${CS_BASE}/api/v1/Order/Returns?PageSize=1&Page=1" | tail -1
# Beklenen: 200

# Adim 5: Yanlis API Key
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-api-key: INVALID_KEY" \
  "${CS_BASE}/api/v1/Products?PageSize=1&Page=1" | tail -1
# Beklenen: 401 veya 403

# Adim 6: Kargo Takip
# curl -s -w "\n%{http_code} %{time_total}s" \
#   -H "x-api-key: ${CS_API_KEY}" \
#   "${CS_BASE}/api/v1/Order/Tracking?orderId=<ORDER_ID>"
```

---

## 4. N11 (SOAP - 4 Adim)

```bash
N11_APP_KEY="<APP_KEY>"
N11_APP_SECRET="<APP_SECRET>"
N11_BASE="https://api.n11.com"

# Adim 1: Kategori Listesi (SOAP)
curl -s -w "\n%{http_code} %{time_total}s" \
  -X POST \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: \"GetTopLevelCategories\"" \
  -d '<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:cat="http://www.n11.com/ws/schemas">
  <soapenv:Body>
    <cat:GetTopLevelCategoriesRequest>
      <auth>
        <appKey>'"${N11_APP_KEY}"'</appKey>
        <appSecret>'"${N11_APP_SECRET}"'</appSecret>
      </auth>
    </cat:GetTopLevelCategoriesRequest>
  </soapenv:Body>
</soapenv:Envelope>' \
  "${N11_BASE}/ws/CategoryService.wsdl" | tail -1
# Beklenen: 200

# Adim 2: Urun Listesi (SOAP)
# ProductService.wsdl -> GetProductList

# Adim 3: Siparis Listesi (SOAP)
# OrderService.wsdl -> DetailedOrderList

# Adim 4: Marka Listesi (SOAP)
# BrandService.wsdl -> GetBrands
```

---

## 5. Pazarama (OAuth2 - 5 Adim)

```bash
PZ_CLIENT_ID="<CLIENT_ID>"
PZ_CLIENT_SECRET="<CLIENT_SECRET>"

# Adim 1: Token Al
PZ_TOKEN=$(curl -s -X POST \
  -d "grant_type=client_credentials&client_id=${PZ_CLIENT_ID}&client_secret=${PZ_CLIENT_SECRET}&scope=merchantgatewayapi.fullaccess" \
  "https://isortagimgiris.pazarama.com/connect/token" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null)
echo "Token: ${PZ_TOKEN:0:20}..."

# Adim 2: Marka Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PZ_TOKEN}" \
  -H "User-Agent: MesTech-Pazarama-Client/4.0" \
  "https://isortagimgiris.pazarama.com/brand/getBrands?Page=1&Size=1" | tail -1
# Beklenen: 200

# Adim 3: Urun Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PZ_TOKEN}" \
  "https://isortagimgiris.pazarama.com/product/products?Approved=true&Page=1&Size=1" | tail -1
# Beklenen: 200

# Adim 4: Kategori Agaci
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PZ_TOKEN}" \
  "https://isortagimgiris.pazarama.com/category/getCategoryTree" | tail -1
# Beklenen: 200

# Adim 5: Iade Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PZ_TOKEN}" \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"pageSize":1,"pageNumber":1,"refundStatus":1}' \
  "https://isortagimgiris.pazarama.com/order/getRefund" | tail -1
# Beklenen: 200
```

---

## 6. Amazon TR (SP-API - 6 Adim)

```bash
AMAZON_CLIENT_ID="<CLIENT_ID>"
AMAZON_CLIENT_SECRET="<CLIENT_SECRET>"
AMAZON_REFRESH_TOKEN="<REFRESH_TOKEN>"
AMAZON_SELLER_ID="<SELLER_ID>"
AMAZON_MARKETPLACE_TR="A33AVAJ2PDY3EV"
AMAZON_BASE="https://sellingpartnerapi-eu.amazon.com"

# Adim 1: LWA Token Al
AMAZON_TOKEN=$(curl -s -X POST \
  -d "grant_type=refresh_token&refresh_token=${AMAZON_REFRESH_TOKEN}&client_id=${AMAZON_CLIENT_ID}&client_secret=${AMAZON_CLIENT_SECRET}" \
  "https://api.amazon.com/auth/o2/token" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null)
echo "Token: ${AMAZON_TOKEN:0:20}..."

# Adim 2: Katalog Ogeler
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-amz-access-token: ${AMAZON_TOKEN}" \
  "${AMAZON_BASE}/catalog/2022-04-01/items?marketplaceIds=${AMAZON_MARKETPLACE_TR}&includedData=summaries&pageSize=1" | tail -1
# Beklenen: 200

# Adim 3: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-amz-access-token: ${AMAZON_TOKEN}" \
  "${AMAZON_BASE}/orders/v0/orders?MarketplaceIds=${AMAZON_MARKETPLACE_TR}&CreatedAfter=$(date -u -d '-7 days' +%Y-%m-%dT%H:%M:%SZ)" | tail -1
# Beklenen: 200

# Adim 4: Seller Bilgisi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "x-amz-access-token: ${AMAZON_TOKEN}" \
  "${AMAZON_BASE}/sellers/v1/marketplaceParticipations" | tail -1
# Beklenen: 200

# Adim 5-6: Feed Document olusturma, XML yukleme (mutasyon -- dikkatli)
```

---

## 7. eBay (OAuth2 - 5 Adim)

```bash
EBAY_CLIENT_ID="<CLIENT_ID>"
EBAY_CLIENT_SECRET="<CLIENT_SECRET>"
EBAY_BASE="https://api.ebay.com"
# Sandbox: https://api.sandbox.ebay.com

# Adim 1: Token Al (Client Credentials)
EBAY_AUTH=$(echo -n "${EBAY_CLIENT_ID}:${EBAY_CLIENT_SECRET}" | base64)
EBAY_TOKEN=$(curl -s -X POST \
  -H "Authorization: Basic ${EBAY_AUTH}" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&scope=https://api.ebay.com/oauth/api_scope" \
  "${EBAY_BASE}/identity/v1/oauth2/token" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null)
echo "Token: ${EBAY_TOKEN:0:20}..."

# Adim 2: Inventory Items
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${EBAY_TOKEN}" \
  "${EBAY_BASE}/sell/inventory/v1/inventory_item?limit=1&offset=0" | tail -1
# Beklenen: 200

# Adim 3: Siparisler (Fulfillment)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${EBAY_TOKEN}" \
  "${EBAY_BASE}/sell/fulfillment/v1/order?limit=1" | tail -1
# Beklenen: 200

# Adim 4: Kategori Agaci (Taxonomy)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${EBAY_TOKEN}" \
  "${EBAY_BASE}/commerce/taxonomy/v1/category_tree/3" | tail -1
# Beklenen: 200 (categoryTreeId=3 Turkey)

# Adim 5: Yanlis Token
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer INVALID_TOKEN" \
  "${EBAY_BASE}/sell/inventory/v1/inventory_item?limit=1" | tail -1
# Beklenen: 401
```

---

## 8. Ozon (Header Auth - 4 Adim)

```bash
OZON_CLIENT_ID="<CLIENT_ID>"
OZON_API_KEY="<API_KEY>"
OZON_BASE="https://api-seller.ozon.ru"

# Adim 1: Seller Bilgisi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Client-Id: ${OZON_CLIENT_ID}" \
  -H "Api-Key: ${OZON_API_KEY}" \
  -H "Content-Type: application/json" \
  -X POST -d '{}' \
  "${OZON_BASE}/v1/seller/info" | tail -1
# Beklenen: 200

# Adim 2: Urun Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Client-Id: ${OZON_CLIENT_ID}" \
  -H "Api-Key: ${OZON_API_KEY}" \
  -H "Content-Type: application/json" \
  -X POST -d '{"filter":{"visibility":"ALL"},"last_id":"","limit":1}' \
  "${OZON_BASE}/v2/product/list" | tail -1
# Beklenen: 200

# Adim 3: Siparis Listesi (FBS)
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Client-Id: ${OZON_CLIENT_ID}" \
  -H "Api-Key: ${OZON_API_KEY}" \
  -H "Content-Type: application/json" \
  -X POST -d '{"dir":"asc","filter":{"since":"2026-03-01T00:00:00.000Z","to":"2026-03-19T23:59:59.999Z","status":""},"limit":1,"offset":0}' \
  "${OZON_BASE}/v3/posting/fbs/list" | tail -1
# Beklenen: 200

# Adim 4: Kategori Agaci
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Client-Id: ${OZON_CLIENT_ID}" \
  -H "Api-Key: ${OZON_API_KEY}" \
  -H "Content-Type: application/json" \
  -X POST -d '{"language":"DEFAULT"}' \
  "${OZON_BASE}/v1/description-category/tree" | tail -1
# Beklenen: 200
```

---

## 9. PttAVM (Bearer Token - 4 Adim)

```bash
PTTAVM_USERNAME="<USERNAME>"
PTTAVM_PASSWORD="<PASSWORD>"
PTTAVM_BASE="https://apigw.pttavm.com"

# Adim 1: Token Al
PTTAVM_TOKEN=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${PTTAVM_USERNAME}\",\"password\":\"${PTTAVM_PASSWORD}\"}" \
  "${PTTAVM_BASE}/api/auth/login" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])" 2>/dev/null)
echo "Token: ${PTTAVM_TOKEN:0:20}..."

# Adim 2: Urun Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PTTAVM_TOKEN}" \
  "${PTTAVM_BASE}/api/product/list?page=1&size=1" | tail -1
# Beklenen: 200

# Adim 3: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PTTAVM_TOKEN}" \
  "${PTTAVM_BASE}/api/orders?startDate=2026-03-01&page=1&size=1" | tail -1
# Beklenen: 200

# Adim 4: Kategori Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "Authorization: Bearer ${PTTAVM_TOKEN}" \
  "${PTTAVM_BASE}/api/category/list" | tail -1
# Beklenen: 200
```

---

## 10. OpenCart (REST API Key - 3 Adim)

```bash
OC_BASE="<OPENCART_BASE_URL>"
OC_TOKEN="<API_TOKEN>"

# Adim 1: Urun Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "X-Oc-Restadmin-Id: ${OC_TOKEN}" \
  "${OC_BASE}/api/rest/products?limit=1" | tail -1
# Beklenen: 200

# Adim 2: Siparis Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "X-Oc-Restadmin-Id: ${OC_TOKEN}" \
  "${OC_BASE}/api/rest/orders?limit=1&sort=o.date_added&order=DESC" | tail -1
# Beklenen: 200

# Adim 3: Kategori Listesi
curl -s -w "\n%{http_code} %{time_total}s" \
  -H "X-Oc-Restadmin-Id: ${OC_TOKEN}" \
  "${OC_BASE}/api/rest/categories" | tail -1
# Beklenen: 200
```

---

## Test Sonuc Tablosu (doldurulacak)

| # | Platform | Adim | HTTP Kodu | Yanit Suresi | Basarili? | Not |
|---|----------|------|-----------|-------------|----------|-----|
| 1 | Trendyol | 1 | | | | |
| 2 | Trendyol | 2 | | | | |
| 3 | Trendyol | 3 | | | | |
| ... | ... | ... | ... | ... | ... | ... |

---

*Bu script read-only analiz sonucu olusturulmustur. Credential placeholder'lari production degerleriyle ASLA commit etmeyin.*
