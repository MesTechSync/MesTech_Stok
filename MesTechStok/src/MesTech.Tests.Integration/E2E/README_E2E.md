# E2E Test Suite — MesTech

## Overview

This folder contains end-to-end scenario tests that validate cross-component
workflows in MesTech. Unlike unit or integration tests that focus on a single
service, E2E tests wire multiple real services together (with mock adapters for
external APIs) and verify the entire chain.

## Test Files

| File | Phase | Description |
|------|-------|-------------|
| `OrchestrationE2ETests.cs` | Dalga 3 | Cross-component orchestration (cargo selector, adapter factory, auto-shipment) |
| `FullFlowE2ETests.cs` | Dalga 4 | Order-to-invoice-to-cargo chain, returns lifecycle, claims, customer accounts |
| `FaturaE2ETests.cs` | Dalga 5 | Invoice flow with WireMock (TrendyolEFaturamAdapter) |
| `SandboxE2ETests.cs` | Dalga 6 | Real sandbox tests (requires API credentials, skipped by default) |
| `AhmetBeyScenarioTests.cs` | Dalga 12 | Full 14-step user journey scaffold (see below) |
| `Blazor/BlazorE2ETests.cs` | Dalga 12 | Playwright E2E stubs for Blazor SaaS (6 tests, requires :5200) |
| `Blazor/BlazorTestConfiguration.cs` | Dalga 12 | Playwright config: routes, viewports, CI skip helper |

## AhmetBey Scenario (Dalga 12)

The "Ahmet Bey" test models the complete journey of a cosmetics shop owner
using MesTech from first registration through daily operations.

### 14 Steps

| # | Method | What it validates |
|---|--------|-------------------|
| 01 | `Step01_CreateTenant` | Tenant creation and default settings |
| 02 | `Step02_ConnectTrendyolStore` | Store connection with API key, TestConnectionAsync |
| 03 | `Step03_Sync50Products` | Product sync (PullProductsAsync, 50 items) |
| 04 | `Step04_Receive5Orders` | Order pull, OrderLine creation, stock deduction |
| 05 | `Step05_CalculateCommission` | Platform commission rates per category |
| 06 | `Step06_CreateShipment` | AutoShipmentService with YurticiKargo |
| 07 | `Step07_CreateInvoice` | MockInvoiceProvider e-fatura + PDF |
| 08 | `Step08_ParseSettlement` | Settlement report CSV parsing |
| 09 | `Step09_CreateJournalEntry` | Double-entry accounting (debit = credit) |
| 10 | `Step10_ViewProfitReport` | Profit/loss report generation |
| 11 | `Step11_ImportBankStatement` | OFX bank statement import |
| 12 | `Step12_RunReconciliation` | Bank vs book reconciliation |
| 13 | `Step13_ApproveExpenseFromBot` | Bot-initiated expense approval workflow |
| 14 | `Step14_GenerateDailyBriefing` | Aggregated daily advisory briefing |

### Current Status

All 14 tests are **scaffold only** — they compile and pass with placeholder
assertions (`Assert.True(true, "TODO: ...")`). Each method contains detailed
TODO comments specifying the MediatR commands/queries that should be called
once the corresponding domain and application layers are implemented.

### Running

```bash
dotnet test --filter "Category=E2E" --logger "console;verbosity=normal"
```

To run only the AhmetBey scenario:

```bash
dotnet test --filter "FullyQualifiedName~AhmetBeyScenarioTests"
```

### Dependencies

- **xUnit** — test framework
- **FluentAssertions** — assertion library (used in other E2E tests)
- **Moq** — mock framework for adapters
- **Bogus** — fake data generation (for product/order fixtures)
- **Testcontainers** — PostgreSQL/Redis/RabbitMQ containers (when DB tests are activated)
- **WireMock.Net** — HTTP mock server (for adapter HTTP tests)
