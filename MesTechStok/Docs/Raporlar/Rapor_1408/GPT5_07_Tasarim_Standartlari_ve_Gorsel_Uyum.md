## Renk, Tipografi ve Grid
- Tema: Light (varsayılan), tipografi ve modern kontrol seti (`Styles/ModernControls.xaml`).
- Grid/padding/margin standartları Views altında tutarlı kullanılıyor.

## Bileşenler ve UX
- Bildirim/Toast sistemi (non-blocking), yükleme spineri, KPI ve sayfalama bileşenleri.
- Erişilebilirlik: Klavye navigasyonu ve kontrast için `AccessibilityHelper` mevcuttur.

## Performans
- Dashboard grafik/animasyonlarında hafif animasyonlar; gereksiz repaint azaltılmalı.
- Listelerde sayfalama (`PaginationComponent`) ve arama/filtre önerilir.

## Görsel Tutarlılık Önerileri
- Renk paleti ve boşluk ölçekleri tek merkezden yönetilmeli (token yaklaşımı).
- Karanlık tema alternatifi (Dark) konfigüre edilebilir.

## Barkod Ekranları
- ROI, threshold ve decode parametreleri konfigürasyon tabanlı; varsayılanlar ürün perakende senaryosuna optimize.
- Canlı önizleme (PreviewEnabled) ile kullanıcı geri bildirimi hızlı verilir.

## Test ve Onay Listesi
- Kontrast (WCAG AA), odak halkası, klavye ile tüm akışlar, animasyon azaltma, responsive pencere boyutlarında kırılma testi.
