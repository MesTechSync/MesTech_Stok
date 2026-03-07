# EMİRNAME: ENTEGRATÖR YAZILIMI — STOK MODÜLÜ MİMARİ KEŞİF

**Belge No:** ENT-STOK-001  
**Tarih:** 05 Mart 2026  
**Yayıncı:** Komutan (MesTech)  
**Hedef Ajanlar:** Tüm Kontrolör Mühendisler (Backend, Frontend, DevOps, QA)  
**Öncelik:** KRİTİK — Proje başlangıç ön koşulu  
**Durum:** AKTİF

---

## 1. MİSYON TANIMI

Bu emirname, Entegratör Yazılımı Stok Modülü'nün geliştirme sürecine başlamadan önce **mevcut yazılım mimarisinin ve yapısının eksiksiz haritalanması** için verilmiştir. Hiçbir kod yazılmayacak, hiçbir değişiklik yapılmayacaktır. Amaç tamamen **keşif, belgeleme ve raporlama**dır.

---

## 2. DEMİR KURALLAR

Bu emirname boyunca aşağıdaki kurallar istisnasız geçerlidir:

1. **SIFIR KOD DEĞİŞİKLİĞİ** — Bu faz tamamen gözlem ve raporlamadır. Tek bir satır kod değiştirilmez.
2. **KANIT ZORUNLULUĞU** — Her tespit, komut çıktısı / ekran görüntüsü / log ile desteklenir. "Gözlemledim" kabul edilmez.
3. **BAĞIMSIZ DOĞRULAMA** — Her kontrolör kendi başına çalışır, başka ajanın raporuna referans vermez.
4. **TAM ŞEFFAFLIK** — Bulunamayan, erişilemeyen, belirsiz olan her şey açıkça "BİLİNMİYOR" olarak raporlanır.
5. **STANDART FORMAT** — Raporlar bu emirnamedeki şablona birebir uygun olarak teslim edilir.

---

## 3. KEŞİF KAPSAMI

Aşağıdaki 8 ana alan her kontrolör tarafından bağımsız olarak incelenecektir:

### 3.1 PROJE GENEL YAPISI

Toplanacak bilgiler:
- Projenin root dizin yapısı (2 seviye derinlik `tree` çıktısı)
- Monorepo mu yoksa single-repo mu?
- Package manager: npm / yarn / pnpm? Lock dosyası hangisi?
- `package.json` ana bağımlılıklar listesi (dependencies + devDependencies)
- TypeScript mi JavaScript mi? `tsconfig.json` var mı?
- Build sistemi: Vite / Webpack / esbuild / Next.js / diğer?
- `.env` veya `.env.example` dosyaları — ortam değişkenleri yapısı (değerler değil, sadece anahtar isimleri)

**Kanıt komutu:**
```bash
tree -L 2 --dirsfirst
cat package.json | jq '.dependencies, .devDependencies'
ls -la .env* tsconfig* vite.config* webpack.config* next.config*
```

### 3.2 BACKEND MİMARİSİ

Toplanacak bilgiler:
- Framework: Express / NestJS / Fastify / Koa / diğer?
- API yapısı: REST / GraphQL / gRPC / karma?
- Route organizasyonu: Dosya bazlı mı, controller bazlı mı?
- Mevcut route dosyaları listesi (özellikle stok/envanter/ürün ile ilgili olanlar)
- Middleware zinciri (auth, validation, logging, error handling)
- ORM/Veritabanı katmanı: Sequelize / Prisma / TypeORM / Knex / raw SQL?
- Veritabanı: PostgreSQL / MySQL / MongoDB / diğer?
- Migration sistemi var mı? Mevcut migration sayısı ve son migration tarihi

**Kanıt komutu:**
```bash
# Route dosyalarını bul
find . -path '*/routes/*' -o -path '*/controllers/*' -o -path '*/api/*' | head -50
# Stok ile ilgili dosyaları bul
grep -rl "stock\|stok\|inventory\|envanter\|product\|urun\|ürün" --include="*.ts" --include="*.js" -l | head -30
# ORM/DB konfigürasyonu
cat prisma/schema.prisma 2>/dev/null || cat sequelize.config* 2>/dev/null || echo "ORM config bulunamadı"
# Migration listesi
ls -la migrations/ 2>/dev/null || ls -la prisma/migrations/ 2>/dev/null
```

### 3.3 VERİTABANI ŞEMASI

Toplanacak bilgiler:
- Mevcut tablo listesi (tüm tablolar)
- Stok/envanter/ürün ile ilgili tabloların tam şema yapısı (sütunlar, tipler, constraintler)
- Foreign key ilişkileri (özellikle stok tabloları arası)
- Index'ler (stok tablolarındaki)
- View'lar varsa listesi
- Stored procedure / function varsa listesi

**Kanıt komutu:**
```bash
# Tablo listesi (PostgreSQL varsayımı - DB tipine göre adapte edin)
psql -h HOST -p PORT -U USER -d DBNAME -c "\dt"
# Stok tablosu şeması
psql -c "\d+ stock_table_name"
# Foreign key'ler
psql -c "SELECT tc.table_name, kcu.column_name, ccu.table_name AS foreign_table
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_name LIKE '%stock%' OR tc.table_name LIKE '%product%';"
```

### 3.4 FRONTEND MİMARİSİ

Toplanacak bilgiler:
- Framework: React / Vue / Angular / Svelte / diğer?
- State management: Redux / Zustand / Pinia / Context API / diğer?
- UI kütüphanesi: MUI / Ant Design / Tailwind / Bootstrap / diğer?
- Sayfa/route yapısı (router konfigürasyonu)
- Mevcut stok ile ilgili sayfalar/bileşenler
- API çağrı katmanı: Axios / fetch / React Query / SWR / diğer?
- Form yönetimi: React Hook Form / Formik / diğer?

**Kanıt komutu:**
```bash
# Frontend dizin yapısı
find src -type f -name "*.tsx" -o -name "*.vue" -o -name "*.jsx" | head -40
# Router konfigürasyonu
grep -rl "Route\|route\|router\|Router" --include="*.tsx" --include="*.jsx" --include="*.ts" -l | head -10
# Stok sayfaları
find src -type f \( -name "*stock*" -o -name "*stok*" -o -name "*inventory*" -o -name "*product*" -o -name "*urun*" \)
```

### 3.5 API ENTEGRASYON KATMANI

Bu bölüm kritiktir — entegratör yazılımın dış sistemlerle nasıl konuştuğunu anlamalıyız.

Toplanacak bilgiler:
- Mevcut dış API entegrasyonları listesi (ERP, e-ticaret, muhasebe vb.)
- API iletişim yöntemi: REST çağrıları / webhook / message queue / diğer?
- Kimlik doğrulama: API key / OAuth / JWT / diğer?
- Rate limiting / retry mekanizması var mı?
- Hata yönetimi stratejisi (dış API hataları nasıl işleniyor?)
- Veri dönüşüm katmanı (mapping/transformer dosyaları)
- Mevcut entegrasyon konfigürasyonları (hangi dış sistemler bağlı?)

**Kanıt komutu:**
```bash
# Entegrasyon dosyaları
find . -path '*/integrations/*' -o -path '*/connectors/*' -o -path '*/adapters/*' -o -path '*/services/external/*' | head -30
# HTTP client konfigürasyonu
grep -rl "axios\|fetch\|request\|HttpService" --include="*.ts" --include="*.js" -l | head -20
# Webhook handler'ları
grep -rl "webhook\|Webhook\|WEBHOOK" --include="*.ts" --include="*.js" -l | head -10
```

### 3.6 STOK MODÜLÜ MEVCUT DURUM

Toplanacak bilgiler:
- Stok modülü var mı, yoksa sıfırdan mı yazılacak?
- Varsa: mevcut stok işlevleri neler? (CRUD, transfer, sayım, hareket, alarm vb.)
- Stok birimi yapısı (SKU, barkod, lot/seri takibi var mı?)
- Depo/lokasyon yapısı (çoklu depo desteği var mı?)
- Stok hareket tipleri (giriş, çıkış, transfer, fire, sayım farkı vb.)
- Fiyatlama entegrasyonu (maliyet, satış fiyatı, döviz)
- Raporlama mevcut mu? Hangi raporlar var?

**Kanıt komutu:**
```bash
# Stok ile ilgili tüm dosyalar
find . -type f \( -name "*stock*" -o -name "*stok*" -o -name "*inventory*" -o -name "*warehouse*" -o -name "*depo*" \) | sort
# Stok servisleri
grep -rl "class.*Stock\|class.*Inventory\|class.*Warehouse\|StockService\|InventoryService" --include="*.ts" --include="*.js" -l
# Stok endpoint'leri
grep -rn "stock\|stok\|inventory" --include="*.ts" --include="*.js" routes/ controllers/ 2>/dev/null
```

### 3.7 DOCKER & ALTYAPI

Toplanacak bilgiler:
- Docker kullanılıyor mu? `docker-compose.yml` / `Dockerfile` yapısı
- Servis listesi (container'lar)
- Ağ yapısı (portlar, internal network)
- Volume'lar (veri kalıcılığı)
- CI/CD pipeline var mı? (GitHub Actions / GitLab CI / Jenkins)
- Ortam ayrımı (dev / staging / production)

**Kanıt komutu:**
```bash
cat docker-compose.yml 2>/dev/null
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null
cat .github/workflows/*.yml 2>/dev/null || cat .gitlab-ci.yml 2>/dev/null
```

### 3.8 GÜVENLİK & KONFİGÜRASYON

Toplanacak bilgiler:
- Authentication sistemi (JWT / Session / OAuth)
- Kullanıcı rolleri ve yetkilendirme yapısı (RBAC / ABAC)
- .env yapısı (sadece key isimleri, değerler ASLA paylaşılmaz)
- Secret management (vault / env / config dosyası)
- CORS konfigürasyonu
- Rate limiting

---

## 4. RAPOR ŞABLONU

Her kontrolör aşağıdaki formatta rapor teslim edecektir:

```
=== ENTEGRATÖR STOK MİMARİ KEŞİF RAPORU ===
Kontrolör: [Ajan İsmi]
Tarih: [Rapor Tarihi]
İncelenen Repo: [Repo adı/path]

--- BÖLÜM 3.1: PROJE GENEL YAPISI ---
[Bulgular + komut çıktıları]

--- BÖLÜM 3.2: BACKEND MİMARİSİ ---
[Bulgular + komut çıktıları]

--- BÖLÜM 3.3: VERİTABANI ŞEMASI ---
[Bulgular + komut çıktıları]

[... her bölüm için tekrar ...]

--- ÖZET MATRIS ---
| Alan | Durum | Teknoloji | Notlar |
|------|-------|-----------|--------|
| Backend | ✅ Mevcut / ⚠️ Kısmi / ❌ Yok | Express/NestJS/... | ... |
| Frontend | ... | ... | ... |
| Veritabanı | ... | ... | ... |
| Stok Modülü | ... | ... | ... |
| Entegrasyonlar | ... | ... | ... |
| Docker | ... | ... | ... |
| CI/CD | ... | ... | ... |
| Güvenlik | ... | ... | ... |

--- KRİTİK BULGULAR ---
1. [En önemli bulgu]
2. [İkinci önemli bulgu]
3. [...]

--- BİLİNMEYENLER ---
1. [Erişilemeyen/bulunamayan bilgi]
2. [...]

--- ÖNERİLER ---
1. [Stok modülü için mimari öneri]
2. [...]
```

---

## 5. TESLİMAT KURALLARI

- **Teslim formatı:** Yukarıdaki şablona birebir uygun metin rapor
- **Kanıt formatı:** Her tespit için çalıştırılan komut VE çıktısı arka arkaya gösterilecek
- **Zaman sınırı:** Tek oturumda tamamlanacak
- **Gizlilik:** .env değerleri, şifreler, API key'ler ASLA rapora dahil edilmez — sadece key isimleri
- **Belirsizlik yönetimi:** Emin olunmayan tespitler `[?]` işareti ile belirtilir

---

## 6. KOMUTAN NOTU

Bu emirname, Entegratör Yazılımı Stok Modülü'nün **sağlam temeller üzerine inşa edilmesi** için kritik öneme sahiptir. Mimariyi anlamadan kod yazmak, MESA OS'ta tecrübe ettiğimiz gibi, sonradan pahalıya mal olan teknik borç biriktirir.

Kontrolörler bu emirnameyi aldıktan sonra:
1. Projenin kaynak koduna erişim sağlayacak
2. Emirnamedeki her bölümü sırayla inceleyecek
3. Kanıtları toplayacak
4. Raporu şablona uygun olarak teslim edecek

Raporlar toplandıktan sonra çapraz analiz yapılacak ve **Stok Modülü Teknik Tasarım Dokümanı** hazırlanacaktır.

---

**EMİRNAME SONU**  
**Komutan MesTech — "Kanıtsız tespit, tespitsiz geliştirme yoktur."**
