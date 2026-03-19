# Performance Benchmark Plan — 2026-03-19

## Overview

EMR-18 performance benchmark scaffold for MesTech Stock Management System.
Tests use InMemory EF Core for baseline measurements; production targets assume PostgreSQL.

## 7 Scenarios

| # | Scenario | InMemory Target | PostgreSQL Target | Measurement |
|---|----------|----------------|-------------------|-------------|
| 1 | 1000 product sync (AddRange + SaveChanges) | <100ms | <500ms | Stopwatch |
| 2 | 500 order fetch (paged, Include) | <50ms | <200ms | Stopwatch |
| 3 | 100 concurrent API requests | P99 <1000ms | P99 <1000ms | Task timing |
| 4 | 10 parallel invoice generation | — | <2000ms | Task timing |
| 5 | Dashboard aggregate (10K orders) | <100ms | <500ms | Stopwatch |
| 6 | 500 stock update burst | <1000ms | <5000ms | Stopwatch |
| 7 | Memory stability (30 min) | ~50MB | <200MB stable | WorkingSet64 |

## Implemented (EMR-18 L-01)

- **StockPerformanceTests.cs**: Scenarios 1, 2, 5, 6
- **ApiPerformanceTests.cs**: Scenario 3 (health + auth endpoints)

## Not Yet Implemented

- **Scenario 4**: 10 parallel invoice generation (requires EInvoice domain completion)
- **Scenario 7**: Memory stability (requires long-running test harness, not suitable for CI)

## Prerequisites

- .NET 9.0 SDK
- xUnit test runner
- For PostgreSQL targets: Docker with `mestech-postgres` container
- For Testcontainers-based runs: Docker Desktop running

## Test Infrastructure

- **InMemory EF Core**: Used for baseline benchmarks (fast, no Docker dependency)
- **WebApplicationFactory**: Used for API performance tests (MesTechWebApplicationFactory)
- **Tenant isolation**: Each test creates its own tenant ID for data isolation
- **No Bogus dependency**: Data generation uses simple loops (Bogus available in src/MesTech.Tests.Integration)

## Running Tests

```bash
# Run all performance tests
dotnet test tests/MesTech.Integration.Tests/ --filter "Category=Performance" -v normal

# Run specific scenario
dotnet test tests/MesTech.Integration.Tests/ --filter "FullyQualifiedName~Benchmark_1000_Product" -v normal

# With detailed output
dotnet test tests/MesTech.Integration.Tests/ --filter "Category=Performance" -v normal --logger "console;verbosity=detailed"
```

## Interpreting Results

- InMemory results are **lower bounds** — real PostgreSQL latency will be higher
- First-run JIT warmup may cause outliers; consider running twice
- P99 = 99th percentile latency (1 out of 100 requests can be slower)
- CI gate recommendation: fail if any scenario exceeds 2x the PostgreSQL target

## Future Enhancements

1. Add Testcontainers-based PostgreSQL benchmarks for realistic measurements
2. Add BenchmarkDotNet for micro-benchmarks (serialization, mapping)
3. Add memory profiling scenario (Scenario 7) as a manual/nightly job
4. Add Scenario 4 (invoice) when EInvoice module stabilizes
5. Integrate with CI as a non-blocking quality gate (warn, don't fail)
