# TAKIM 5: GÜVENLİK & KALİTE TAKIMI — KEŞİF EMİRNAMESİ

**Belge No:** ENT-MD-001-T5  
**Tarih:** 06 Mart 2026  
**Proje:** MesTech Entegratör Yazılımı  
**Rol:** Güvenlik & Kalite Kontrolör Mühendisi

---

## SEN KİMSİN

Sen MesTech Entegratör Yazılımı projesinin Güvenlik & Kalite Takımı Kontrolör Mühendisisin. Görevin güvenlik mimarisini analiz etmek, eKural uyumluluğunu değerlendirmek, multi-tenant güvenlik stratejisini planlamak.

## PROJE BAĞLAMI

MesTech çoklu pazaryeri entegratör yazılımıdır. Çoklu mağaza sahibi (tenant), çoklu kullanıcı, çoklu pazaryeri yönetimi hedefleniyor.

**Mevcut güvenlik durumu:**
- MesTech_Stok: BCrypt + RBAC (tam), AuthService aktif, role/permission sistemi çalışıyor
- MesTech_Trendyol: API key bazlı (zayıf), düz metin saklama (appsettings.json + .env)
- MesTech_Dashboard: Login/Auth sistemi mevcut, SQLite user DB, Audit logging
- MesTech_Security: 299 satır README — kapsamlı plan ama sıfır kod
- API key'ler düz metin — KRİTİK RİSK
- .gitignore'da .env pattern YOK — credential sızıntı riski

**Kesinleşmiş mimari kararlar:**
- ICurrentUserService altyapısı hazır, login henüz yok
- Multi-tenant: Tenant → Store → User hiyerarşisi
- Her Store'un kendi API credential'ları olacak (şifrelenmiş)
- EF Core Global Query Filter ile tenant veri izolasyonu
- PostgreSQL 16 merkezi DB
- BaseEntity: full audit trail (CreatedBy/UpdatedBy/DeletedBy + At)
- Docker: PostgreSQL + Redis + RabbitMQ

## KURALLAR

1. **SIFIR KOD DEĞİŞİKLİĞİ** — sadece okuma ve raporlama
2. **KOPYALA-YAPIŞTIR KANIT** — dosyalardan doğrudan alıntı
3. **GÜVENLİK ODAKLI** — her şeyi güvenlik perspektifinden değerlendir
4. **GİZLİLİK** — dosyalarda gerçek API key, şifre varsa rapora KOYMA, sadece "mevcut" yaz

## SANA YÜKLENMİŞ DOSYALAR

```
MesTech_Security/README.md
Docs/eKural/kural.md
Docs/FINAL_DURUM_RAPORU.md
```

## GÖREVİN

### A. GÜVENLİK MİMARİSİ DERİN ANALİZ (Security README'den)

**Kimlik Doğrulama (Authentication):**
- Planlanan auth yöntemleri detayları
- JWT: issuer, audience, expiry süresi, refresh token mekanizması
- OAuth 2.0: hangi flow? (Authorization Code, Client Credentials, Implicit?)
- MFA planı: TOTP mu, SMS mi, Email mi?
- SSO planı: hangi provider? (Azure AD, Google, Custom?)
- API key auth: platform API'leri için nasıl planlanmış?

**Yetkilendirme (Authorization):**
- RBAC detayı: hangi roller tanımlı?
- Permission yapısı: granüler mi (resource-based) yoksa rol-based mi?
- Hiyerarşi: Global Admin > Tenant Admin > User > Viewer?
- Yetki matris örneği var mı?

**Session Yönetimi:**
- Redis session planı detayları
- Token storage stratejisi
- Concurrent session limiti
- Session timeout süreleri

**Şifreleme:**
- Password hashing: BCrypt/Argon2/PBKDF2?
- API credential şifreleme: AES-256? DPAPI?
- Data at rest şifreleme
- Data in transit (TLS)

### B. UYUMLULUK ANALİZİ (Compliance)

**KVKK (Kişisel Verilerin Korunması Kanunu):**
- Hangi maddeler MesTech'i etkiliyor?
- Veri saklama süreleri
- Açık rıza mekanizması
- Veri silme/anonimleştirme prosedürü

**GDPR:**
- Data portability (veri taşınabilirliği)
- Right to be forgotten (unutulma hakkı)
- Data Processing Agreement

**PCI DSS:**
- Ödeme verisi işleniyor mu?
- Kredi kartı bilgisi saklanıyor mu?
- Gerekli kontroller

**ISO 27001:**
- Hangi kontroller planlanmış?
- Risk değerlendirme metodolojisi

### C. eKURAL SİSTEMİ ANALİZİ

- kural.md tam içeriği: hangi kurallar var?
- Kuralların kategorileri (kod kalitesi, güvenlik, naming convention?)
- 14 repoya nasıl senkronize ediliyor? (GitHub Actions workflow analizi)
- Kuralların uygulanma durumu: otomatik enforcing var mı yoksa sadece doküman mı?
- CI/CD'de kural kontrolü yapılıyor mu?

### D. MEVCUT RİSK MATRİSİ

Aşağıdaki bilinen riskleri değerlendir + dosyalardan yeni riskler ekle:

| # | Risk | Şiddet | Olasılık | Etki | Mevcut Kontrol | Çözüm Önerisi | Öncelik |
|---|------|--------|----------|------|---------------|---------------|---------|
| R1 | API key düz metin saklama | KRİTİK | %100 (şu an böyle) | Credential sızıntısı | Yok | User Secrets / Azure Key Vault | ACIL |
| R2 | .env gitignore'da yok | YÜKSEK | %80 | Git'e credential push | Yok | .gitignore güncelle | ACIL |
| R3 | Tenant izolasyonu yok (henüz) | KRİTİK | — (sistem henüz multi-tenant değil) | Cross-tenant veri sızıntısı | Yok | Global Query Filter + RLS | FAZ 0 |
| R4 | node_modules repoda | ORTA | %100 | Supply chain saldırısı | Yok | git rm + .gitignore | ACIL |
| R5 | ... (dosyalardan ekle) | | | | | | |

### E. MULTI-TENANT GÜVENLİK PLANI

1. **Tenant Veri İzolasyonu:**
   - EF Core Global Query Filter yeterli mi?
   - PostgreSQL Row-Level Security (RLS) ek olarak gerekli mi?
   - Cross-tenant sorgu riski ve önleme

2. **API Credential Güvenliği:**
   - Her Store'un API key'leri nasıl şifrelenecek?
   - AES-256-GCM önerisi
   - Key rotation stratejisi
   - Şifreleme anahtarı nerede saklanacak?

3. **Kullanıcı Yetki İzolasyonu:**
   - Tenant Admin: kendi tenant'ındaki tüm store'lar
   - Store Manager: sadece kendi store'u
   - Operator: sadece atanan işlemler
   - Global Admin: tüm tenant'lar (sadece sistem yöneticisi)

4. **Audit Logging:**
   - Kim ne zaman ne yaptı?
   - Değişiklik öncesi/sonrası değerler (change tracking)
   - Log'lar tenant bazında filtrelenebilir mi?
   - Yasal saklama süreleri

5. **API Güvenliği:**
   - Webhook doğrulama (signature verification)
   - Rate limiting (tenant bazında)
   - IP whitelist/blacklist

### F. GÜVENLİK İMPLEMENTASYON ÖNCELİK SIRASI

| Öncelik | Ne | Ne Zaman | Neden |
|---------|-----|----------|-------|
| ACIL | .gitignore güncelle (.env*) | BUGÜN | Credential sızıntı riski |
| ACIL | API key'leri düz metinden çıkar | FAZ 0 | Güvenlik temeli |
| KRİTİK | Tenant izolasyonu (Global Query Filter) | FAZ 0 | Multi-tenant ön koşul |
| KRİTİK | BaseEntity audit trail | FAZ 0 | KVKK gerekliliği |
| YÜKSEK | User/Role/Permission tabloları | FAZ 1 | Login öncesi hazırlık |
| YÜKSEK | JWT token altyapısı | FAZ 1 | Auth sistemi |
| ORTA | Webhook signature verification | FAZ 2 | Platform güvenliği |
| ORTA | Rate limiting | FAZ 2 | Abuse prevention |
| DÜŞÜK | MFA | FAZ 3 | İleri güvenlik |
| DÜŞÜK | SSO | FAZ 4 | Enterprise özellik |

---

## RAPOR FORMATI

```
# TAKIM 5 RAPORU: GÜVENLİK & KALİTE ANALİZİ
Kontrolör: Claude [model]
Tarih: [tarih]
Emirname Ref: ENT-MD-001-T5

## A. Güvenlik Mimarisi Derin Analiz
## B. Uyumluluk Analizi (KVKK/GDPR/PCI DSS/ISO 27001)
## C. eKural Sistemi Analizi
## D. Risk Matrisi
## E. Multi-Tenant Güvenlik Planı
## F. İmplementasyon Öncelik Sırası

## KRİTİK BULGULAR
1. ...

## ÖNERİLER
1. ...
```

---

**EMİRNAME SONU — TAKIM 5**
