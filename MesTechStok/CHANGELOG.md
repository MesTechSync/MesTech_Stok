# Changelog

All notable changes to the MesTechStok project will be documented in this file.
Format based on [Keep a Changelog](https://keepachangelog.com/).

## [1.0.25] - 2026-03-29
## [1.0.28] - 2026-03-29### DEV3 v3.8 TUR9 — DlqEndpoints güvenlik düzeltmesi- fix(security): DlqEndpoints X-Admin-Key auth filter eklendi (STRIDE Elevation of Privilege)- 14/14 webhook signature validator GERÇEK HMAC implementasyon doğrulandı (0 stub)
## [1.0.27] - 2026-03-29### DEV3 v3.8 TUR8 — Webhook event alias 21→35 platform kapsam genişletme- feat: Shopify orders/paid+orders/updated, eBay ITEM.SOLD+ORDER.DELIVERY_UPDATE- feat: Ozon order_status_changed+stock_changed+item_price_changed- feat: Generic return.requested, claim.created, order.updated
## [1.0.26] - 2026-03-29

### DEV3 v3.8 TUR6 — WebhookEventRouter 16 platform mapping tamamlandı
- fix: ResolvePlatformType 8→16 platform (AmazonEu, Ozon, Etsy, Zalando, Pazarama, PttAVM, OpenCart, Bitrix24)
- fix: Default fallback Enum.TryParse ile güvenli çözümleme


### DEV3 v3.8 TUR4 — DLQ + Webhook 3 bug fix
- fix: WebhookEventRouter Shopify/WooCommerce PlatformType.OpenCart → doğru enum (veri bütünlüğü)
- fix: DlqReprocessService hardcoded https → RabbitMQ:ManagementScheme config (HTTP/HTTPS tutarsızlık)
- perf: WebhookEventRouter payload Nx parse → 1x parse (JsonElement.Clone)

## [1.0.24] - 2026-03-29

### DEV3 v3.8 TUR3 — WebhookProcessor STRIDE güvenlik düzeltmesi
- fix(security): WebhookProcessor unsigned webhook reject (G472 KAPANDI, FMEA RPN 105→35)
- Backward-compatible: Webhooks:AllowUnsigned:{platform}=true ile legacy bypass

## [1.0.23] - 2026-03-29

### DEV3 v3.8 TUR2 — sözleşme doğruluk kontrolü + STRIDE güvenlik tarama
- docs(gorev): G468-G472 — v3.8 tarama scripti 16 dizin eksik, borç tablosu 3 hata, alan çakışması, webhook STRIDE
- scan: v3.8 DEV3 tarama scripti kapsamı %29 (62/214 dosya) — 152 dosya taranmıyor
- security: WebhookProcessor signature=null KABUL riski (FMEA RPN=105)

## [1.0.22] - 2026-03-29

### DEV3 Entegrasyon & Adapter TUR 1-3 — alan taraması + v3.8 hata raporu
- fix(adapter): PazaramaAdapter token endpoint configurable via PazaramaTokenEndpoint credential key
- docs(gorev): G456-G458 cross-DEV bulgu — IDealRepository/IOrderRepository/ILeadRepository genişletme
- docs(gorev): v3.7 BORÇ TABLOSU 5 hata düzeltme önerisi (NotImpl=0, boşCatch=0, GetCategories=0)
- scan: 214 dosya, 55714 satır — NotImpl=0, TODO=0, stub=0, boşCatch=0

## [1.0.21] - 2026-03-28

### DEV5 Test & Kalite TUR 9-11 — handler %100 + VM test %77
- fix(tests): fix 3 Avalonia VM constructor mismatches — CashFlowReport, DropshipOrders, EInvoice (G286 P0 KAPANDI)
- test(event-handlers): 20 notification event handler tests — LowStock, CRM 7 method, ShipmentCost, StockSync, OrderCancelled
- test(dashboard): 8 dashboard query handler tests — SalesChart, LowStockAlerts, PendingInvoices, RecentOrders
- test(auth): 5 DisableMfa security tests — OWASP ASVS V2.8 coverage
- test(accounting): 13 GL event handler tests — CommissionCharged, InvoiceApproved, InvoiceCancelled, OrderConfirmedRevenue, OrderShippedCost, ReturnJournalReversal
- test(erp): 6 ERPNext event handler tests — Customer, SalesInvoice, StockEntry
- test(stock+notif): 13 stock/notification handler tests — OrderPlacedStockDeduction, PriceLossDetected, ReturnApproved, ZeroStock, Subscription, SyncError
- test(vm): 47 Avalonia ViewModel tests — Settings, OnboardingWizard, MfaSetup, Stock, Leads, Kanban, Contacts, Barcode, TrialBalance, CommissionRates, Calendar, Expenses, Billing, Backup, Documents + 20 more

**Metrics:** 15 commits, 112 yeni test, handler coverage %100 (399/399), VM coverage %77 (128/166), G286 P0 KAPANDI, G367 cross-DEV acildi

## [1.0.20] - 2026-03-28

### DEV6 Business Logic & WebApi — PAYLAŞIM-DEV3
- fix(infra): G078 P1 — SafeHandleRequestAsync wrapper for 3 fire-and-forget self-hosted endpoints
- fix(services): G079 P1 — remove Task.Run threadpool starvation in XmlImportService

## [1.0.19] - 2026-03-28

### DEV3 Integration TUR 7 — Timeout Options + Settlement S3 chain
- refactor(adapter): HttpTimeout→Options for Trendyol(15s)+AmazonTR(30s)+AmazonEU(30s) — configurable via appsettings (G189 partial)
- feat(accounting): ParseAndImportSettlementHandler — raw file→ISettlementParserFactory→DB (G186 KAPANDI)
- feat(api): /settlements/parse-and-import endpoint (8 parser bağlandı)

### DEV6 Business Logic & WebApi — PAYLAŞIM-DEV3
- fix(infra): G078 P1 — SafeHandleRequestAsync wrapper for HealthCheck, RealtimeDashboard, MesaStatus fire-and-forget endpoints

## [1.0.18] - 2026-03-28

### DEV2 Frontend & UI TUR 11-15 — security fixes + keyboard UX + Blazor paylaşım
- fix(security): G069+G070 P0 XSS — innerHTML escape in bravo_notification_center + alpha_stats/notification + ComponentLoader (Trendyol)
- fix(avalonia): G089 P1 — Dispose previous ViewModel on navigation (timer memory leak)
- fix(blazor): G074 P1 — NotificationBell ObjectDisposedException race condition guard [PAYLAŞIM-DEV2]
- fix(avalonia): G058 P2 — TrendyolVM OperationCanceledException context comment
- fix(avalonia): G090 P2 — HealthVM OnDispose timer stop
- fix(security): G075 P2 — alpha_core_navigation breadcrumb XSS escape
- feat(html): G109 P2 — CRM deals+leads API fetch with mock fallback
- feat(avalonia): Ctrl+F search focus + Escape clear — Products, OrderList, Inventory, Customer views

**Metrics:** ~16 commits, 2 P0 XSS fixed, 2 P1 fixed, 5 P2 fixed, 4 keyboard shortcuts, GOREV_HAVUZU DEV2 9/9 closed (100%)

## [1.0.17] - 2026-03-28

### DEV5 Test & Kalite TUR 1-10 — handler + validator + event handler test kapsamı
- test(accounting): 22 query happy path + 14 reconciliation + 18 expense/income handler tests
- test(stock): 28 stock/order/dashboard + 13 order/warehouse + 12 product extended + 3 bulk handler tests
- test(validators): 69 accounting validator tests (FixedExpense, Salary, Tax, Counterparty, Penalty, FinancialGoal, Update/Delete, Campaign, Warehouse, Store, Product, Order, Billing, Cari, Barcode)
- test(crm): 8 CRM + 12 extended CRM handler tests (deals, pipeline, messages, contacts, loyalty)
- test(platform): 13 report + 6 dashboard + 1 trigger sync + 8 notification/settings + 7 ERP handler tests
- test(invoice): 9 e-invoice + invoice handler tests
- test(events): 9 domain event (Z2,Z3,Z6,Z7,Z8,Z10 chain) + 10 notification event handler tests
- test(bulk): 27 bulk null-guard tests (category, stock, cari, quotation, return, tenant, HR, finance)
- Unique handler kapsam: 100→293 (%74.7), Validator test: 33→48, Toplam: 256→325 .cs (+69 dosya, ~442 test metot)

## [1.0.16] - 2026-03-28

### DEV3 Integration TUR 6 — S3 KOPUK fixes + PAYLAŞIM
- feat(accounting): ParseAndImportSettlementHandler — raw file → ISettlementParserFactory → DB (G186 S3 KOPUK→BAĞLI)
- feat(api): /settlements/parse-and-import endpoint (8 platform parser bağlandı)
- feat(api): PaymentEndpoints multi-provider factory — PayTR+Stripe+Iyzico (G187 KAPANDI)

### DEV6 Business Logic & WebApi
- refactor(webapi): standardize 33 BadRequest→ProblemDetails RFC 7807 — G114 complete (Report, Invoice, Warehouse, SupplierFeeds, Shipment, Order, Calendar)

## [1.0.15] - 2026-03-28

### DEV6 Business Logic & WebApi
- refactor(webapi): standardize 20 BadRequest→ProblemDetails RFC 7807 (BulkProduct, SocialFeed, ProductImage, BaBs, EInvoice)
- docs(gorev): close G062(Blazor endpoints), G075(XSS file removed) + add G113-G114

## [1.0.14] - 2026-03-28

### DEV2 Frontend & UI (TUR 1-11)

- feat(avalonia): 6 new views — Penalty, FixedExpense, TaxRecord, FixedAsset, Quotation, Billing
- fix(avalonia): 19 orphaned buttons bound to Commands, 18 ToolTip, 4 DataGrid a11y, 17 hardcoded colors
- fix(security): G069+G070 P0 XSS — innerHTML escaping in bravo_notification_center + alpha_stats/notification + ComponentLoader
- fix(avalonia): G089 P1 memory leak — Dispose previous ViewModel on navigation (timer leak)
- fix(avalonia): BarcodeScannerVM empty catch → HasError (KÇ-07)

**Metrics:** 27 commits, 6 new views, 0 P0/P1 borç, quality A+ (9.5/10), 8/8 user journey verified

## [1.0.13] - 2026-03-28

### DEV3 Integration & Adapter Hardening (TUR 1-4)

- fix(test): N11Adapter tests HttpClient→IHttpClientFactory constructor (4 files)
- fix(test): EInvoiceProviderHardeningTests IHttpClientFactory + .Object fix (2 commits)
- fix(test): CargoAdapterHardeningTests parameter alignment (16 errors→0)
- fix(test): ERPNext adapter+handler tests — readonly DTO + API method names
- fix(api): ErpEndpoints + AuthEndpoints premature brace scope fix
- fix(settlement): TenantId enforcement in 7 settlement parsers — Guid.Empty→throw (BORÇ-N)
- fix(di): StripePaymentGateway registered as IPaymentGateway
- feat(payment): StripePaymentProviderAdapter — IPaymentGateway→IPaymentProvider bridge (S3 KOPUK→BAĞLI)
- fix(di): MockInvoiceProvider/Adapter conditional registration — dev/test only
- fix(adapter): Amazon LWA token null-forgiving→TryGetProperty (3 files)
- fix(invoice): ELogo+Sovos null-forgiving→TryGetProperty defensive parsing

**Metrics:** 13 commits, 50 build errors→0, 7 BORÇ-N→0, 5 null crash risks→0, 1 S3 chain fixed

## [1.0.0] - 2026-03-15

### Dalga 13: v1.0.0 Final

- Core.AppDbContext SIFIR — ImageMapWizard son referans CQRS'e gecti
- Avalonia tam gecis — 75/75 WPF View port edildi (%100 kapsama)
- Playwright E2E genisletme — 12 gercek test (Dashboard, CRM, Finance, Auth)
- Netsis ERP adapter — 4. ERP secenegi (Basic Auth, Enterprise/Wings)
- Netsis contract tests (WireMock, 5 test) + unit tests (4 test)
- 3370+ test (Netsis:9 + E2E:12 yeni)
- Denetci skor: A (9.0+)

## [1.0.0-rc1] - 2026-03-15

### Added (Dalga 8-12 Cumulative)

#### CRM Module (Dalga 8)
- Leads, Deals, Pipeline, PipelineStage, CrmContact, Activity entities
- Kanban board view (WPF + Blazor + Avalonia)
- CRM-to-Order bridge service (`CrmOrderBridgeService`)

#### HR Module (Dalga 8)
- Employee, Department, Leave, WorkSchedule entities
- Leave approval workflow with domain events (LeaveApprovedEvent, LeaveRejectedEvent)
- HrEmployeesView (WPF), Employees + LeaveRequests (Blazor + Avalonia)

#### Finance Module (Dalga 9)
- FinanceExpense, BankAccount, GLTransaction entities
- Expense management with approval workflow
- ProfitLoss reporting view (WPF + Blazor + Avalonia)

#### Accounting Module (Dalga 9)
- Full double-entry bookkeeping: ChartOfAccounts, JournalEntry, JournalLine
- Counterparty, LegalEntity, SettlementBatch, BankTransaction entities
- ReconciliationMatch, CargoExpense, TaxRecord, TaxWithholding entities
- CashFlowEntry, ProfitReport, FinancialGoal, PersonalExpense entities
- AccountingDocument, ExpenseCategory, AccountingSupplierAccount
- 16 CQRS accounting commands + 14 accounting queries
- Settlement parsers: Trendyol, Hepsiburada, Amazon, Ciceksepeti, N11, Pazarama, OpenCart
- Platform commission rate provider (real rates from platforms)
- CariHesaplar, BankaHesaplari, Belgeler, Mutabakat, GelirGider, KarZarar views (WPF)

#### Document Management (Dalga 8)
- MinIO object storage integration (S3-compatible)
- DocumentManagerView with categories and versioning (WPF)
- Documents page (Blazor + Avalonia)

#### E-Invoice System (Dalga 9)
- 9 e-fatura providers: Sovos, ELogo, Parasut, TrendyolEFaturam, BirFatura,
  DijitalPlanet, HBFatura, GibPortal, Mock
- UBL-TR XML builder for compliant invoice generation
- GIB Mukellef VKN lookup service
- E-invoice create/send/cancel commands + list/detail/VKN-check queries
- EInvoiceCreateView + EInvoiceListView (WPF)
- InvoiceProviderFactory + InvoiceAdapterFactory (adapter/provider separation)

#### Dropshipping Module (Dalga 8)
- DropshippingPool, DropshippingPoolProduct, FeedImportLog, SupplierFeed entities
- 14 CQRS commands + 9 queries for pool management
- 4 feed parsers: XML, CSV, JSON, Excel
- Feed health check, delta detection, fuzzy matching, category auto-mapper
- Image download service for product images
- Feed credential protector (encrypted storage)
- 4 WPF views: Dashboard, Pool, Import, Export, Supplier

#### Marketplace Adapters (Dalga 8-12)
- 11 marketplace adapters implementing IIntegratorAdapter:
  - Trendyol, Hepsiburada, Ciceksepeti, N11, Pazarama, OpenCart
  - AmazonTR, AmazonEU, PttAVM, Ozon, eBay
- IOrderCapableAdapter, IShipmentCapableAdapter, IWebhookCapableAdapter interfaces
- Webhook receiver service for real-time platform notifications

#### Cargo Providers (Dalga 9-10)
- 7 cargo adapters: Yurtici, Aras, Surat, HepsiJet, Sendeo, MNG, PTT
- CargoProviderFactory + CargoProviderSelector (smart routing)
- CargoShipmentView, BulkCargoLabelDialog (WPF)

#### ERP Integration (Dalga 11)
- 3 ERP adapters: Parasut, Logo, BizimHesap
- ERPAdapterFactory for runtime adapter selection
- Canonical finance mapper for cross-ERP normalization
- ParasutTokenService, LogoTokenService for OAuth2 token management

#### Fulfillment (Dalga 11)
- Amazon FBA adapter
- Hepsilojistik adapter
- FulfillmentProviderFactory

#### Social Media Feeds (Dalga 10)
- Google Merchant Feed adapter
- Facebook Shop Feed adapter
- Instagram Shop Feed adapter (extends Facebook)
- SocialFeedRefreshJob for scheduled updates

#### Payment Integration (Dalga 10)
- PayTR Direct adapter (server-to-server)
- PayTR iFrame adapter (redirect flow)
- IPaymentProvider interface

#### Blazor Server SaaS (Dalga 12)
- 17 Razor pages: Home, Dashboard, Login, Stock, Orders, Leads, Deals, Contacts,
  Employees, LeaveRequests, Expenses, BankAccounts, Documents, Marketplaces,
  Reports, Settings, ProfitLoss
- PWA-ready, responsive layout
- Shared Application/Domain layer with WPF desktop

#### Avalonia Cross-Platform PoC (Dalga 12)
- 17 Views: Dashboard, Leads, Kanban, ProfitLoss, Products, Stock, Orders,
  Settings, Contacts, Employees, LeaveRequests, Documents, Reports,
  Marketplaces, Expenses, BankAccounts, MainWindow
- Runs on Windows, macOS, Linux

#### ASP.NET Core Web API (Dalga 12)
- 18 endpoint groups: Health, Auth, Products, Stock, Orders, Categories,
  Invoices, Quotations, SyncStatus, Dashboard, SupplierFeeds,
  DropshippingPool, Dropshipping, Finance, CRM, Accounting,
  Notifications, Shipping
- API key authentication + JWT token auth (HMAC-SHA256)
- Per-API-key rate limiting (fixed window)
- OpenAPI/Swagger documentation

#### Multi-Currency (Dalga 10)
- TCMB exchange rate integration (EUR/USD/GBP)

#### i18n Infrastructure (Dalga 12)
- Localization resource files: Strings.tr.resx, Strings.en.resx
- Turkish + English support

#### Tasks & Calendar (Dalga 8)
- Project, Milestone, WorkTask, TimeEntry, ProjectMember entities
- CalendarEvent, CalendarEventAttendee entities
- ProjectsView, KanbanBoardView, CalendarView (WPF)

#### Notifications (Dalga 10)
- NotificationDashboardView (WPF)
- Notification API endpoints

#### MESA Bridge (Dalga 8)
- Real activation support for AI integration
- Event monitoring (MesaEventMonitor, MesaEventPublisher, MesaConsumer)
- Mock service for development

#### Quotation System (Dalga 8)
- Quotation, QuotationLine entities
- Create, Accept, Reject, ConvertToInvoice workflow
- QuotationView (WPF)

#### Bitrix24 Integration (Dalga 8)
- Bitrix24Adapter with OAuth2 authentication
- Bitrix24Deal, Bitrix24Contact, Bitrix24DealProductRow entities
- PushOrderToBitrix24, SyncBitrix24Contacts commands

### Architecture
- Clean Architecture + DDD + CQRS/MediatR pattern
- 12 solution projects (Domain, Application, Infrastructure, Desktop, Core,
  WebApi, Blazor, Avalonia, Tests.Unit, Tests.Integration, Tests.Architecture,
  MesTechStok.Tests)
- 93+ domain entities across 8 bounded contexts
  (Stock, CRM, HR, Finance, Accounting, Dropshipping, Tasks, Calendar)
- 77+ CQRS Commands with handlers
- 67+ CQRS Queries with handlers
- 144+ total CQRS endpoints
- 83+ WPF Views (XAML)
- Multi-tenant EF Core query filters (ITenantEntity, ITenantProvider)
- Domain events: 15+ event types (StockChanged, OrderPlaced, InvoiceCreated,
  LowStockDetected, PriceChanged, ReturnCreated, BuyboxLost, etc.)
- Value Objects: Money, SKU, Barcode, StockLevel, UnitOfMeasure, LocationCode, Address
- Domain Services: StockCalculation, Pricing, BarcodeValidation, PlatformReturnPolicy
- 6 Docker services: PostgreSQL 17 (pgvector), Redis 7, RabbitMQ 3,
  MySQL 8, Seq, MinIO

### Security
- BCrypt password hashing (BCrypt.Net-Next 4.0.3)
- JWT token authentication (HMAC-SHA256)
- API key authentication with X-API-Key header
- Zero hardcoded production credentials in source code
- User-secrets for all sensitive configuration
- Docker credentials via environment variables with required markers
- FeedCredentialProtector for encrypted feed credentials
- Token rotation service for API key lifecycle management
- Multi-tenant data isolation via EF Core global query filters

### Infrastructure
- Testcontainers for PostgreSQL, Redis, RabbitMQ integration tests
- WireMock-based adapter contract tests
- CI/CD: 9-step pipeline (secret-scan, boundary-check, build, test, quality-gate)
- Directory.Build.props with .NET analyzers (NetAnalyzers, Meziantou)
- Health check endpoints (/health)
- Polly resilience policies for HTTP clients
- Circuit breaker pattern with CircuitStateLog

### Test Coverage
- 3361+ total tests
- Unit tests: Domain entities, value objects, handlers, services
- Integration tests: Testcontainers (PG/Redis/RMQ), adapter contracts,
  cargo providers, invoice providers, settlement parsers
- Architecture tests: Clean Architecture dependency rules, naming conventions,
  CRM layer isolation
- E2E tests: Full orchestration flow, fatura flow, sandbox flow,
  Blazor page navigation
- Performance benchmarks: Bulk sync, system performance, startup time
- Regression tests: 5-platform regression, past wave interrogation,
  build regression, domain configuration

---

## [0.8.0] - 2026-03-01 (Dalga 8-D Sprint)

### Added
- Dropshipping module activation (first customer-facing release)
- 8 active platform adapters (TY, OC, CS, HB, PZ, N11, Amazon, B24)
- 6/6 export formatters
- 4/4 feed parsers
- 3 cargo providers (Yurtici, Aras, Surat)
- 9 e-fatura providers (IInvoiceProviderFactory)
- 33+ CQRS commands, 28+ CQRS queries
- 3241 tests, 60+ domain entities

### Metrics
- Build errors: 0
- Auditor score: A- (8.6/10)

---

## [0.1.0] - 2025-12-01 (Dalga 1 Baseline)

### Added
- Clean Architecture foundation
- WPF desktop app with MaterialDesign
- SQLite + EF Core persistence
- Trendyol + OpenCart adapters
- 38 domain entities, 15 initial entities
- 253 tests (up from 8)
- CI/CD 9-step pipeline
- Docker: PostgreSQL, Redis, RabbitMQ, MySQL, Seq
- 56/56 HTML pages via ComponentLoader

### Fixed
- 120+ build errors resolved to 0
- 192 domain+app warnings resolved to 0
- 25+ hardcoded credentials removed

---

[1.0.0-rc1]: https://github.com/MesTechSync/MesTech_Stok/compare/v0.8.0...v1.0.0-rc1
[0.8.0]: https://github.com/MesTechSync/MesTech_Stok/compare/v0.1.0...v0.8.0
[0.1.0]: https://github.com/MesTechSync/MesTech_Stok/releases/tag/v0.1.0
