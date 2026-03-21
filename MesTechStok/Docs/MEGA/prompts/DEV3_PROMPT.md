CLAUDE.md'yi oku. Docs/MEGA/ klasöründeki 3 dosyayı oku (MEGA_EMIRNAME, MEGA_DELTA_RAPORU, DETAY_ATLASI).

Sen DEV 3'sün — Entegrasyon & MESA OS sorumlusu.
Sadece şu dosyalara dokunabilirsin:
- src/MesTech.Infrastructure/Integration/**
- src/MesTech.Infrastructure/Messaging/**
- src/MesTech.Infrastructure/Jobs/**

Başka DEV'in alanına DOKUNMA.

ÖNCELİK SIRASI İLE GÖREVLERİN:

P1-06: 18 Consumer log-only → MediatR dispatch
- ÖNCE: grep -rn 'IMediator\|_mediator\|Send(\|Publish(' src/ --include='*Consumer*.cs' | grep -v bin | grep -v Test | wc -l
- ÖNCE: grep -rn '_logger\.Log\|LogInformation\|LogWarning' src/ --include='*Consumer*.cs' | grep -v bin | grep -v Test | wc -l
- DETAY_ATLASI'ndan consumer listesi:
  1. AccountingApprovalConsumer → _mediator.Send(new ProcessAccountingApprovalCommand(...))
  2. AccountingRejectionConsumer → _mediator.Send(new ProcessAccountingRejectionCommand(...))
  3. AiAdvisoryRecommendationConsumer → _mediator.Send(new ProcessAiAdvisoryCommand(...))
  4. AiDocumentExtractedConsumer → _mediator.Send(new ProcessDocumentExtractionCommand(...))
  5. AiEInvoiceDraftGeneratedConsumer → _mediator.Send(new ProcessEInvoiceDraftCommand(...))
  6. AiErpReconciliationDoneConsumer → _mediator.Send(new ProcessErpReconciliationCommand(...))
  7. AiReconciliationSuggestedConsumer → _mediator.Send(new ProcessReconciliationSuggestionCommand(...))
  8. BotEFaturaRequestedConsumer → _mediator.Send(new ProcessBotEFaturaRequestCommand(...))
  9. DocumentClassifiedConsumer → _mediator.Send(new ProcessDocumentClassificationCommand(...))
  10. NotificationSentConsumer → _mediator.Send(new ProcessNotificationSentCommand(...))
  11. MesaDlqConsumer → log + alert (DLQ özel — MediatR opsiyonel)
  12. MesaAiContentConsumer → _mediator.Send(new ProcessAiContentCommand(...))
  13. MesaAiPriceConsumer → _mediator.Send(new ProcessAiPriceCommand(...))
  14. MesaBotStatusConsumer → _mediator.Send(new ProcessBotStatusCommand(...))
  15. MesaAiPriceOptimizedConsumer → _mediator.Send(new ProcessPriceOptimizationCommand(...))
  16. MesaAiStockPredictedConsumer → _mediator.Send(new ProcessStockPredictionCommand(...))
  17. MesaBotInvoiceRequestConsumer → _mediator.Send(new ProcessBotInvoiceRequestCommand(...))
  18. MesaBotReturnRequestConsumer → _mediator.Send(new ProcessBotReturnRequestCommand(...))

Her consumer için:
  1. İlgili Command Application katmanında var mı kontrol et (DEV 1 alanında oluşturulmuşsa kullan)
  2. Yoksa ve Application'a dokunman gerekiyorsa → sadece consumer'da IMediator inject et, Send çağrısını yaz, Command yoksa ILogger ile birlikte bir TODO bırak
  3. Consumer'daki _logger.LogInformation satırlarını KALDIRMA — üstüne MediatR dispatch EKLE
  4. dotnet build → 0 error

6'lık batch halinde:
- Batch 1: consumer 1-6 (kolay olanlar)
- Batch 2: consumer 7-12
- Batch 3: consumer 13-18

Commit: feat(mesa): migrate consumer batch N to MediatR dispatch [MEGA-P1-06]

SONRA: grep -rn '_mediator' src/ --include='*Consumer*.cs' | grep -v bin | grep -v Test | wc -l → ≥18 olmalı

Her görev bitince bana ÖNCE/SONRA sayılarını göster.
