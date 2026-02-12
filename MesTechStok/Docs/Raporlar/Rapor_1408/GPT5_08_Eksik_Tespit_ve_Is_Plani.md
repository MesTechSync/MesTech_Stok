## Eksik/Sorun Tespiti (Gerçek Koda Dayalı)
- **DeltaSyncService**: İskelet; OpenCart zamanı-tabanlı değişim sorguları ve yerel karşılaştırmalar TODO.
- **SyncRetryService**: Ürün/Sipariş/Stok özel retry işleyicileri TODO; genel çerçeve hazır.
- **AuthService**: Login demo akışında BCrypt ile tutarlılık notu; parola doğrulama yolunda iyileştirme.
- **OpenCartHttpService (Desktop)**: Polly politikaları belirtimi ve hata kategorileri genişletilebilir.
- **Index/Schema Yardımcıları**: Uzun vadede resmi EF migrations ile hizalama önerilir.

## Önceliklendirilmiş İş Planı
1. Delta Senkron Tamamlanması (Yüksek)
   - OpenCart değişim tespiti, yerel timestamp karşılaştırmaları, idempotency anahtarı kullanımı.
2. Retry İşleyicileri (Yüksek)
   - `SyncRetryService` için ürün/sipariş/stok özel geri dönüş fonksiyonları.
3. Güvenlik Tutarlılığı (Orta)
   - Tüm parola operasyonlarında BCrypt tekilleştirme, token rotation değerlendirmesi.
4. Telemetri Dashboard (Orta)
   - API başarısızlık oranı, devre durumu, yavaş endpointler ve senkron hataları.
5. UI/UX İyileştirmeleri (Orta)
   - Karanlık tema, performans ölçümü, liste sanallaştırma.

## Başarı Kriterleri
- Delta senkron modülleri tüm CRUD değişimlerini yakalar; kuyrukta çözümlenmemiş öğe oranı <%1.
- Devre açık kalma süresi < 2dk ortalama; başarısızlık oranı < %5.
- EF şema resmi migration’lar ile hizalı; canlı ek kolon/indeks yardımcıları yalnızca acil durumlarda kullanılır.
