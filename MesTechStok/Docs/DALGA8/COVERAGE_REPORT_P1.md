# P1 Coverage Raporu

**Tarih:** 2026-03-15
**Sprint:** Paralel Paket P1 (PA-DEV-C)
**Sorumlu:** PA-DEV-C

---

## Test Sayisi

| Katman | Test Sayisi | Durum |
|--------|-------------|-------|
| Unit Tests (Category!=UIAutomation) | ~832 (Dalga 6 H28 baz) | GREEN |
| Architecture Tests (CleanArchitectureTests) | 3 | GREEN |
| Architecture Tests (CrmLayerDependencyTests) | 7 | GREEN |
| Architecture Tests (NamingConventionTests) | 6 | GREEN |
| **Architecture Tests Toplam** | **16** | **16/16 GREEN** |

> Not: Unit test sayisi Dalga 6 H28 sprint sonucuna gore ~832 olarak alinmistir.
> Yeni eklenen arch testler (13 yeni) bu sayiya dahil degildir.

---

## Mimari Test Kapsami (PA-DEV-C Katkisi)

### CrmLayerDependencyTests (7 test)
1. `CrmEntities_ShouldNotReferenceInfrastructure` — CRM entity'leri Infrastructure'a bagimli olmamali
2. `CrmEntities_ShouldNotReferenceApplication` — CRM entity'leri Application'a bagimli olmamali
3. `TaskEntities_ShouldNotReferenceOuterLayers` — Task entity'leri dis katmanlara bagimli olmamali
4. `TaskHandlers_ShouldOnlyDependOnDomainAndApplication` — Task handler'lari Infrastructure'a bagimli olmamali
5. `PlatformAdapters_ShouldImplementIIntegratorAdapter` — Platform adapter'lari IIntegratorAdapter implement etmeli
6. `CrmRepositories_ShouldResideInInfrastructure` — CRM repo'lari Infrastructure katmaninda olmali
7. `CrmDomainEvents_ShouldResideInDomainAssembly` — CRM domain event'leri sadece Domain'de olmali

### NamingConventionTests (6 test)
1. `DomainEvents_ShouldEndWithEventSuffix` — Domain event'leri "Event" ile bitmeli
2. `Commands_ShouldEndWithCommandSuffix` — Command'lar "Command" ile bitmeli
3. `Handlers_ShouldEndWithHandlerSuffix` — Handler'lar "Handler" ile bitmeli
4. `Repositories_ShouldEndWithRepositorySuffix` — Repository siniflar "Repository" ile bitmeli
5. `TaskDomainEvents_ShouldEndWithEventSuffix` — Task sub-namespace event kontrolu
6. `Handlers_ShouldResideInApplicationAssembly` — Handler'lar Application assembly'de olmali

---

## Bug Fix (PA-DEV-C)

`CrmHangfireJobs.cs` — eksik `using MesTech.Domain.Interfaces;` eklendi.
Build hatasiydi: `ICrmLeadRepository`, `IWorkTaskRepository`, `IUnitOfWork` bulunamiyordu.

---

## Hedef

H28 sonunda Domain + Application katmanlari icin **%80+ coverage** hedeflenmektedir.

Oncelikli Coverage Aciklarimiz:
- Domain/Entities/Crm/** — unit test yok (entity davranis testleri eklenmeli)
- Domain/Entities/Tasks/** — unit test yok
- Application/Features/Tasks/** — handler unit testleri eklenmeli
- Infrastructure/Persistence/Repositories/Crm/* — integration test gerektiriyor
