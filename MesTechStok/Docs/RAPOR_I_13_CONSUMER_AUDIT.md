# İ-13 S-01: MESA OS Consumer Audit Raporu

**Tarih:** 2026-03-20
**Hazırlayan:** DEV 1 (Backend)
**Emirname:** İ-13 MESA OS Sağlamlaştırma

## Özet

19 consumer tespit edildi. İ-13 S-02 sonrası tüm consumer'lar gerçek domain operasyonu yapıyor.

## Consumer Envanteri

### Muhasebe Consumer'ları (10 adet)

| # | Consumer | Event | Domain İşlemi | MediatR | DB Yazma | Exception |
|---|----------|-------|---------------|---------|----------|-----------|
| 1 | AccountingApprovalConsumer | BotAccountingApprovedEvent | PersonalExpense + JournalEntry oluştur | ❌ | ✅ | try/catch + throw |
| 2 | AccountingRejectionConsumer | BotAccountingRejectedEvent | Document metadata güncelle | ❌ | ✅ | try/catch + throw |
| 3 | AiDocumentExtractedConsumer | AiDocumentExtractedEvent | Confidence>=0.90: PersonalExpense oluştur | ❌ | ✅ | try/catch + throw |
| 4 | DocumentClassifiedConsumer | AiDocumentClassifiedEvent | Document classification güncelle | ❌ | ✅ | try/catch + throw |
| 5 | AiReconciliationSuggestedConsumer | AiReconciliationSuggestedEvent | ReconciliationMatch (NeedsReview) | ❌ | ✅ | try/catch + throw |
| 6 | AiAdvisoryRecommendationConsumer | AiAdvisoryRecommendationEvent | NotificationLog oluştur | ❌ | ✅ | try/catch + throw |
| 7 | NotificationSentConsumer | BotNotificationSentEvent | NotificationLog oluştur | ❌ | ✅ | try/catch + throw |
| 8 | AiEInvoiceDraftGeneratedConsumer | AiEInvoiceDraftGeneratedIntegrationEvent | NotificationLog (muhasebe bilgi) | ❌ | ✅ | try/catch + throw |
| 9 | AiErpReconciliationDoneConsumer | AiErpReconciliationDoneIntegrationEvent | ReconciliationMatch per mismatch | ❌ | ✅ | try/catch + throw |
| 10 | BotEFaturaRequestedConsumer | BotEFaturaRequestedIntegrationEvent | NotificationLog (muhasebe bilgi) | ❌ | ✅ | try/catch + throw |

### MESA OS Consumer'ları (7 adet — İ-13 S-02 ile derinleştirildi)

| # | Consumer | Event | Domain İşlemi (S-02 sonrası) | DB Yazma |
|---|----------|-------|-------------------------------|----------|
| 11 | MesaAiContentConsumer | MesaAiContentGeneratedEvent | Product.Description güncelle | ✅ |
| 12 | MesaAiPriceConsumer | MesaAiPriceRecommendedEvent | PriceRecommendation kaydet | ✅ |
| 13 | MesaBotStatusConsumer | MesaBotNotificationSentEvent | NotificationLog kaydet | ✅ |
| 14 | MesaAiPriceOptimizedConsumer | MesaAiPriceOptimizedEvent | PriceRecommendation + %20 alert | ✅ |
| 15 | MesaAiStockPredictedConsumer | MesaAiStockPredictedEvent | StockPrediction kaydet | ✅ |
| 16 | MesaBotInvoiceRequestConsumer | MesaBotInvoiceRequestedEvent | Order→Invoice lookup | ✅ (read) |
| 17 | MesaBotReturnRequestConsumer | MesaBotReturnRequestedEvent | ReturnRequest oluştur | ✅ |

### Altyapı Consumer'ları (2 adet)

| # | Consumer | Event | Domain İşlemi |
|---|----------|-------|---------------|
| 18 | MesaMeetingScheduledConsumer | MesaMeetingScheduledEvent | MediatR: CreateCalendarEventCommand |
| 19 | MesaDlqConsumer | Fault | Error metric + log (sink) |

## Idempotency Durumu

- **IdempotencyFilter<T>**: Tüm consumer'lara otomatik uygulanıyor (MassTransit pipe filter)
- **IProcessedMessageStore**: InMemoryProcessedMessageStore (7 gün TTL, saatlik cleanup)
- **Gelecek:** Redis-backed store multi-instance senaryoları için

## DLQ İzleme

- **DlqMonitorService**: Hangfire job (5dk periyod), RabbitMQ Management API
- **DlqReprocessService**: Manuel admin endpoint, max 3 reprocess
- **DlqEndpoints**: POST /api/internal/dlq/reprocess/{queue}, GET /api/internal/dlq/status

## Health Check

- **MesaCompositeHealthCheck**: 4 bileşen (RabbitMQ, MESA API, Circuit Breaker, Staleness)
- **Endpoint**: /api/health/mesa

## Metrikler

- 6 Prometheus metrik: mesa_ai_request_total, mesa_ai_request_duration_seconds, mesa_bot_send_total, mesa_consumer_processed_total, mesa_circuit_breaker_state, mesa_dlq_depth

## Sonuç

- **Log-only consumer kalmadı**: 0 (tümü domain operasyonu yapıyor)
- **Toplam consumer**: 19
- **İ-13 ile eklenen repo**: 3 (IPriceRecommendationRepository, IStockPredictionRepository, IReturnRequestRepository)
- **İ-13 ile eklenen test**: 23 (5 CircuitBreaker + 4 Idempotency + 14 ConsumerDepth)
- **Build**: 0 error
