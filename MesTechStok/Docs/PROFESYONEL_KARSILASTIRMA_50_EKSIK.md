# MesTech vs Profesyonel Platform — 50 Kritik Eksik Özellik Raporu
# Tarih: 5 Nisan 2026 | Hazırlayan: DEV 6
# Kaynak: 163 özellik araştırması (Trendyol/ikas/Sentos/ChannelAdvisor/Shopify)

---

## MEVCUT DURUM

| Katman | Sayı | Durum |
|--------|------|-------|
| Avalonia View | 172 | İyi temel |
| Blazor Page | 122 | Tam |
| Domain Entity | 138→181 | Güçlü |
| CQRS Handler | 452 | Kapsamlı |
| Endpoint | 99 | Geniş |
| Adapter | 47 | 13 platform |
| Test | 3000+ | Yeterli |

**Mimari sağlam. Ama profesyonel rekabet için 50 kritik özellik eksik.**

---

## P0 — KRİTİK EKSİKLER (15 özellik)

### 1. BuyBox Otomasyonu (DEV3+DEV6+DEV2)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E01 | Rakip fiyat takibi | DEV3 | Trendyol "Diğer Satıcılar" API pull, rakip fiyat/rating DB |
| E02 | Otomatik fiyat eşleme kuralları | DEV1 | Rule engine: "rakipten 1TL ucuz, min %15 margin" |
| E03 | BuyBox kazanma/kaybetme dashboard | DEV2 | Kırmızı/yeşil gösterge, win-rate % |
| E04 | Dinamik repricing motoru | DEV6 | Her 15dk otomatik fiyat endpoint + Hangfire job |

### 2. Bildirim Sistemi (DEV2+DEV6+DEV3)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E05 | Gerçek zamanlı sipariş bildirimi | DEV2 | Toast notification + ses + badge |
| E06 | Stok kritik uyarı push | DEV6 | WebSocket/SignalR → client push |
| E07 | Platform mesaj SLA uyarısı | DEV3 | Trendyol 24 saat cevap süre takibi |
| E08 | Günlük performans özeti | DEV6 | Sabah digest (Telegram/e-posta) endpoint |

### 3. Yasal Uyumluluk (DEV1+DEV6+DEV4)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E09 | E-İrsaliye entegrasyonu | DEV3 | GİB sevk irsaliyesi gönderimi |
| E10 | ETBİS raporlama | DEV6 | Yıllık gelir/işlem raporu endpoint |
| E11 | 30 gün fiyat geçmişi (Omnibus) | DEV1 | "Eski Fiyat" otomatik hesaplama, yanıltıcı indirim önleme |
| E12 | Cayma hakkı takibi (14 gün) | DEV1 | Sipariş bazlı geri sayım |
| E13 | Ba/Bs form otomatik hazırlama | DEV6 | Aylık 5000TL üzeri alış/satış raporlama endpoint |

### 4. Sipariş İş Akışı (DEV2+DEV1)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E14 | Pick/Pack/Ship workflow | DEV2 | Barkod tarayıcı ile toplama→paketleme→kargo akışı |
| E15 | Sipariş SLA takibi | DEV1 | Platform bazlı kargoya verme süre limiti (24/48 saat) |

---

## P1 — ÖNEMLİ EKSİKLER (20 özellik)

### 5. Ürün Bilgi Yönetimi PIM (DEV1+DEV2+DEV3)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E16 | Ürün tamamlılık skoru | DEV1 | Platform bazlı hazırlık puanı (0-100%) |
| E17 | Dijital varlık yönetimi (DAM) | DEV2 | Platform boyut otomatik resize, watermark |
| E18 | SEO optimizasyon paneli | DEV2 | Başlık uzunluğu, anahtar kelime yoğunluğu |
| E19 | Ürün yaşam döngüsü (Draft→Active→Discontinued) | DEV1 | Durum makinesi + Kanban |
| E20 | Özel alan sistemi | DEV1 | Tenant bazlı kullanıcı tanımlı alanlar |

### 6. İade Yönetimi RMA (DEV1+DEV2+DEV3)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E21 | İade inceleme grading (A/B/C/D) | DEV1 | Tekrar satılabilir/hasarlı/defolu/imha |
| E22 | İade oranı analitik | DEV2 | Ürün/kategori/platform/neden bazlı dashboard |
| E23 | Seri iade eden müşteri tespiti | DEV1 | %30+ iade oranı → otomatik bayrak |
| E24 | İade maliyet takibi | DEV1 | Orijinal kargo + iade kargo + değer kaybı |
| E25 | İade faturası (iade fatura) otomatik | DEV6 | Endpoint: iade onayı → GİB'e iade faturası |

### 7. Fiyatlandırma Motoru (DEV1+DEV6)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E26 | Platform bazlı fiyat kuralları | DEV1 | Komisyon farkına göre otomatik fiyat hesaplama |
| E27 | Zaman bazlı fiyat | DEV1 | Hafta sonu/gece farklı fiyat kuralları |
| E28 | Stok bazlı fiyat | DEV1 | <5 adet → prim, >200 → tasfiye |
| E29 | Kampanya yöneticisi | DEV2+DEV6 | Tarih aralığı, etkilenen ürünler, indirim tipi |
| E30 | Margin koruma tabanı | DEV6 | Endpoint: minimum fiyat altına düşülmesini engelle |

### 8. Depo Yönetimi WMS (DEV1+DEV2)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E31 | Raf/koridor/göz lokasyon sistemi | DEV1 | Hiyerarşik depo haritası |
| E32 | Dalga toplama (wave picking) | DEV1 | Birden fazla siparişi optimize yol ile toplama |
| E33 | Stok rezervasyon sistemi | DEV1 | Platform/kampanya bazlı stok ayırma |
| E34 | Ölü stok analizi (30/60/90/180 gün) | DEV1 | Yaşlandırma raporu + tasfiye önerisi |
| E35 | Desi hesaplama aracı | DEV2 | En×Boy×Yükseklik/3000 = desi, kargo maliyet karşılaştırma |

### 9. Müşteri Analitik CRM (DEV1+DEV2)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E36 | RFM segmentasyonu | DEV1 | Recency/Frequency/Monetary puanlama |
| E37 | Müşteri yaşam boyu değeri (CLV) | DEV1 | Gelecek gelir tahmini |
| E38 | Platform mesaj birleştirme (unified inbox) | DEV2 | Tüm platform mesajları tek ekranda |
| E39 | Yorum/değerlendirme yönetimi | DEV2+DEV3 | Tüm platformlardan yorumlar + duygu analizi |
| E40 | Coğrafi müşteri haritası | DEV2 | İl/ilçe bazlı sipariş yoğunluk haritası |

---

## P2 — GELİŞTİRME (15 özellik)

### 10. Dropshipping Gelişmiş (DEV3+DEV1)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E41 | Tedarikçi otomatik sipariş | DEV3 | Müşteri siparişi → otomatik tedarikçi PO |
| E42 | Tedarikçi performans kartı | DEV1 | Zamanında teslimat oranı, hata oranı |
| E43 | Kör gönderi (blind shipping) | DEV3 | Kendi markanla gönder, tedarikçi markası gizle |

### 11. Mobil Uygulama Hazırlık (DEV2+DEV4)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E44 | Mobil sipariş yönetimi | DEV2 | React Native / .NET MAUI sipariş listesi |
| E45 | Mobil barkod tarayıcı | DEV2 | Telefon kamera ile stok sayım |
| E46 | Mobil gelir dashboard | DEV2 | Günlük/haftalık KPI kartları |

### 12. Entegrasyon Derinleştirme (DEV3+DEV6)
| # | Özellik | DEV | Açıklama |
|---|---------|-----|----------|
| E47 | Trendyol reklam entegrasyonu | DEV3 | Sponsorlu ürün, ROAS takibi |
| E48 | Google Merchant Center feed | DEV3 | Ürün feed XML + performans |
| E49 | WhatsApp Business API | DEV3 | Sipariş bildirimi + müşteri iletişimi |
| E50 | Muhasebe otomatik kapanış | DEV6 | Aylık kapanış endpoint (KDV beyan, Ba/Bs, e-defter) |

---

## DEV GÖREV DAĞILIMI

| DEV | Görev Sayısı | Ana Alan |
|-----|-------------|----------|
| DEV 1 | 22 | Domain: rule engine, RFM, CLV, WMS, RMA, lifecycle |
| DEV 2 | 16 | UI/UX: dashboard, workflow, analytics, mobile prep |
| DEV 3 | 9 | Adapter: rakip fiyat, e-irsaliye, reklam, WhatsApp |
| DEV 4 | 1 | DevOps: mobil CI/CD pipeline |
| DEV 5 | Tümü | Her P0/P1 için test yazacak |
| DEV 6 | 8 | Endpoint: repricing, bildirim push, Ba/Bs, ETBİS, kampanya, margin |

---

## SONUÇ

MesTech mimari olarak %62 kapsama sahip (75/121 temel özellik).
Profesyonel rekabet için %85+ gerekli (ikas/Sentos seviyesi).
Bu rapordaki 50 özellik eklenirse: **%92 kapsam = Türkiye pazar lideri**.

**En acil 5:**
1. E14 Pick/Pack/Ship workflow — sipariş işleme hızı
2. E01-E04 BuyBox otomasyonu — gelir kaybı
3. E05-E08 Bildirim sistemi — SLA cezaları
4. E09-E13 Yasal uyumluluk — hukuki risk
5. E21-E25 İade yönetimi — maliyet kontrolü
