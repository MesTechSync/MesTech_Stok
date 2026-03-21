CLAUDE.md'yi oku. Docs/MEGA/ klasöründeki 3 dosyayı oku (MEGA_EMIRNAME, MEGA_DELTA_RAPORU, DETAY_ATLASI).

Sen DEV 5'sin — Test & Kalite & Doğrulama sorumlusu.
Sadece şu dosyalara dokunabilirsin:
- tests/**
- Docs/MEGA/ (doğrulama raporları)

Başka DEV'in alanına DOKUNMA.

ÖNCELİK SIRASI İLE GÖREVLERİN:

P1-09: TODO/FIXME temizliği (107 → <10)
- ÖNCE: grep -rn 'TODO\|FIXME\|HACK\|XXX' src/ --include='*.cs' --include='*.axaml' --include='*.razor' | grep -v bin | wc -l
- Her TODO için karar ver:
  A) Tamamla (test yaz, eksik kodu yaz) → tests/ içinde
  B) Gerçekten gelecek için ise → "// FUTURE:" olarak yeniden etiketle
  C) Gereksizse → sil
- tests/ içindeki TODO'lar için gerçek test implementasyonu yaz
- 20'lik batch
- Commit: fix(quality): resolve TODO/FIXME batch N [MEGA-P1-09]

TEST BORCU TEMİZLİĞİ:
- ÖNCE: grep -rn '\[Skip\]\|\[Ignore\]' tests/ --include='*.cs' | grep -v bin | wc -l
- Skip olan testleri gerçek teste dönüştür veya kaldır
- ÖNCE: grep -rn 'NotImplementedException' tests/ --include='*.cs' | grep -v bin | wc -l
- NotImplemented testleri tamamla
- Commit: fix(tests): convert skip/stub tests to real tests [MEGA-P1-TEST]

DOĞRULAMA RAPORU (en önemli görevin):
- Diğer 4 DEV çalışırken ve bitirince, Mega Emirname'deki tüm Faz 1 keşif komutlarını çalıştır
- Sonuçları Docs/MEGA/MEGA_DOGRULAMA_RAPORU_R1.md dosyasına yaz
- Format:

```markdown
# MEGA DOĞRULAMA RAPORU — Round 1
# Tarih: [bugünün tarihi]

## B01: TEMA
| Metrik | Hedef | R1 Öncesi | R1 Sonrası | Delta |
|--------|-------|-----------|------------|-------|
| #2855AC dosya | 0 | 179 | ??? | ??? |
...

## B02: SHELL & GÜVENLİK
...

## GENEL METRİKLER
| Test sayısı | ??? |
| Build error | ??? |
| Coverage | ??? |
```

- Bu rapor 2. çalıştırmaya karar vermek için kullanılacak
- Delta > 0 olan bileşenler → 2. çalıştırmada tekrar hedeflenir
- Delta = 0 olanlar → atlanır

Commit: docs(quality): MEGA validation report round 1 [MEGA-DOGRULAMA-R1]

COVERAGE RAPORU:
- dotnet test --collect:"XPlat Code Coverage" çalıştır
- reportgenerator ile HTML rapor oluştur
- Docs/MEGA/COVERAGE_R1/ klasörüne kopyala
- Commit: docs(quality): code coverage report round 1 [MEGA-COVERAGE-R1]

Her görev bitince bana ÖNCE/SONRA sayılarını göster.
