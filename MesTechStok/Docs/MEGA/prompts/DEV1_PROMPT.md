CLAUDE.md'yi oku. Docs/MEGA/ klasöründeki 3 dosyayı oku (MEGA_EMIRNAME, MEGA_DELTA_RAPORU, DETAY_ATLASI).

Sen DEV 1'sin — Backend & Domain sorumlusu.
Sadece şu dosyalara dokunabilirsin:
- src/MesTech.Domain/**
- src/MesTech.Application/**
- src/MesTech.Infrastructure/Persistence/**
- src/MesTech.WebApi/**
- src/MesTechStok.Core/** (sadece AppDbContext elimination)
- src/MesTechStok.Desktop/** (sadece AppDbContext ref kaldırma)

Başka DEV'in alanına DOKUNMA.

ÖNCELİK SIRASI İLE GÖREVLERİN:

P0-01: Hardcoded admin/admin kaldır (8 yer)
- ÖNCE: grep -rn 'admin.*admin\|"admin"\|Password.*=.*"' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l
- Her birini BCrypt hash veya configuration'a çevir
- SONRA: aynı grep → 0 olmalı
- Commit: fix(security): remove hardcoded admin credentials [MEGA-P0-01]

P0-05: Core.AppDbContext referanslarını CQRS'e taşı (120 ref)
- ÖNCE: grep -rn 'AppDbContext\|Core\.Data' src/ --include='*.cs' | grep -v bin | grep -v obj | grep -v Test | wc -l
- 20'lik batch'ler halinde çalış
- Her batch'te: ilgili AppDbContext kullanımını MediatR Send/Query'ye çevir
- Her batch sonrası: dotnet build → 0 error, dotnet test → 0 failed
- SONRA: aynı grep → sayı azalmış olmalı
- Commit: refactor(core-elimination): migrate batch N AppDbContext→MediatR [MEGA-P0-05]

P2-12: InvoiceType enum'a 3 değer ekle
- EWaybill, ESelfEmployment, EExport
- Commit: feat(domain): add EWaybill/ESelfEmployment/EExport to InvoiceType [MEGA-P2-12]

P2-13: Loyalty + Campaign entity oluştur
- ÖNCE: find src/MesTech.Domain/ -name "*Loyalty*" -o -name "*Campaign*" | wc -l → 0
- LoyaltyProgram entity (BaseEntity, TenantId, Name, PointsPerPurchase, MinRedeemPoints)
- LoyaltyTransaction entity (BaseEntity, TenantId, CustomerId, Points, Type)
- Campaign entity (BaseEntity, TenantId, Name, StartDate, EndDate, DiscountPercent, PlatformType)
- CampaignProduct entity (BaseEntity, CampaignId, ProductId)
- Commit: feat(domain): add Loyalty+Campaign entities for CRM expansion [MEGA-P2-13]

P2-15: Finance endpoint 3 yeni ekle
- GET /api/v1/finance/profit-loss
- GET /api/v1/finance/cash-flow
- GET /api/v1/finance/budget-summary
- Commit: feat(webapi): add profit-loss/cash-flow/budget finance endpoints [MEGA-P2-15]

Her görev bitince bana ÖNCE/SONRA sayılarını göster.
