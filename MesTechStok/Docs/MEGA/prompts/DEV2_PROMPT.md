CLAUDE.md'yi oku. Docs/MEGA/ klasöründeki 3 dosyayı oku (MEGA_EMIRNAME, MEGA_DELTA_RAPORU, DETAY_ATLASI).

Sen DEV 2'sin — Frontend & UI sorumlusu.
Sadece şu dosyalara dokunabilirsin:
- src/MesTech.Avalonia/Views/**
- src/MesTech.Avalonia/Themes/**
- src/MesTech.Avalonia/Dialogs/**
- src/MesTech.Avalonia/ViewModels/**
- src/MesTech.Avalonia/Styles/**
- src/MesTechStok.Desktop/Views/** (sadece renk düzeltme)
- frontend/html/** (innerHTML sanitize)

Başka DEV'in alanına DOKUNMA.

ÖNCELİK SIRASI İLE GÖREVLERİN:

P0-03: #2855AC → DynamicResource (179 dosya)
- ÖNCE: grep -rl '#2855AC\|#2855ac' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | wc -l
- 20'lik batch halinde çalış
- Her dosyada #2855AC → {DynamicResource MesPrimaryColor} (veya uygun token adı)
- MesTechDesignTokens.axaml'da token tanımlı mı kontrol et, yoksa ekle
- Her batch sonrası: dotnet build → 0 error
- SONRA: aynı grep → 20 azalmış olmalı
- Commit: fix(theme): migrate batch N #2855AC→DynamicResource [MEGA-P0-03]

P0-04: innerHTML → DOMPurify sanitize (334 açık)
- ÖNCE: grep -rn 'innerHTML' frontend/ --include='*.html' --include='*.js' | wc -l
- ÖNCE: grep -rn 'DOMPurify\|sanitize' frontend/ --include='*.html' --include='*.js' | wc -l
- Her HTML dosyasına DOMPurify ekle (CDN: <script src="https://cdnjs.cloudflare.com/ajax/libs/dompurify/3.0.6/purify.min.js">)
- innerHTML = value → innerHTML = DOMPurify.sanitize(value)
- 30'luk batch halinde
- Commit: fix(security): sanitize innerHTML batch N with DOMPurify [MEGA-P0-04]

P1-08: Avalonia 24 placeholder → gerçek içerik
- ÖNCE: grep -rl 'placeholder\|Placeholder\|TODO\|Coming Soon\|Yapım Aşamasında' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
- Her placeholder view'da:
  1. İlgili ViewModel'i bul (aynı isimde *ViewModel.cs)
  2. ViewModel MediatR query kullanıyorsa → gerçek data binding yap
  3. ViewModel yoksa → en azından boş state + layout + açıklama ekle
- 6'lık batch halinde
- Commit: fix(avalonia): replace placeholder with real content batch N [MEGA-P1-08]

P1-10: Dashboard 2 stub ViewModel → gerçek MediatR
- AccountingDashboardAvaloniaViewModel: "Will be replaced" → _mediator.Send(...)
- Commit: fix(dashboard): replace demo VM with MediatR dispatch [MEGA-P1-10]

P2-14: Finance Avalonia view 8 yeni (2→10)
- Mevcut: BudgetAvaloniaView + ProfitLossAvaloniaView
- Ekle: CashFlowView, CommissionView, ReconciliationView, TaxView, KdvView, FixedExpenseView, RevenueView, FinanceDashboardView
- Her view: basit layout + ViewModel + MediatR query bind
- Commit: feat(avalonia): add 8 finance views [MEGA-P2-14]

P2-16: Buybox Avalonia view
- Domain/Service zaten var (6 dosya) — sadece UI eksik
- BuyboxAvaloniaView.axaml + BuyboxAvaloniaViewModel.cs
- Commit: feat(avalonia): add Buybox monitoring view [MEGA-P2-16]

P2-17: CommandPalette
- Ctrl+K veya Ctrl+P ile açılan arama kutusu
- Commit: feat(avalonia): add CommandPalette quick search [MEGA-P2-17]

P2-18: Inter font embed
- src/MesTech.Avalonia/Assets/Fonts/Inter-Regular.ttf
- App.axaml'da FontFamily referansı
- Commit: feat(avalonia): embed Inter font [MEGA-P2-18]

Her görev bitince bana ÖNCE/SONRA sayılarını göster.
