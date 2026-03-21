CLAUDE.md'yi oku. Docs/MEGA/ klasöründeki 3 dosyayı oku (MEGA_EMIRNAME, MEGA_DELTA_RAPORU, DETAY_ATLASI).

Sen DEV 4'sün — DevOps & Güvenlik & Blazor sorumlusu.
Sadece şu dosyalara dokunabilirsin:
- docker/**
- .github/workflows/**
- Scripts/**
- src/MesTech.Blazor/**
- .env, .env.example, .env.template, .gitignore
- Docs/ (rollback, production readiness dokümanları)

Başka DEV'in alanına DOKUNMA.

ÖNCELİK SIRASI İLE GÖREVLERİN:

P0-02: Credential temizlik
- .env dosyasını git'ten çıkar:
  git rm --cached .env 2>/dev/null
  echo ".env" >> .gitignore (yoksa ekle)
- ÖNCE: grep -rn 'api[_-]key\|apiKey\|secret\|password' src/ --include='*.cs' --include='*.json' | grep -v bin | grep -v Test | grep -v placeholder | grep -v template | grep -v Configuration | wc -l
- Gerçek credential varsa → env var veya user-secrets'a taşı
- SONRA: aynı grep → 0
- Commit: fix(security): remove .env from git + credential cleanup [MEGA-P0-02]

P1-07: Blazor 616 STUB → gerçek API bağlantı
- ÖNCE: grep -rn 'TODO\|STUB\|NotImplemented\|placeholder\|Demo\|sample' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' | grep -v bin | wc -l
- Her .razor dosyasında:
  1. TODO/STUB satırını bul
  2. MesTechApiClient inject et (@inject MesTechApiClient ApiClient)
  3. İlgili endpoint'i çağır (OnInitializedAsync'de)
  4. Sonucu UI'a bind et
- 20'lik batch halinde
- Her batch sonrası: dotnet build → 0 error
- Commit: fix(blazor): replace STUB with real API batch N [MEGA-P1-07]

P1-11: Rollback prosedürü + Smoke test
- Docs/MEGA/ROLLBACK_PROSEDURU.md oluştur:
  1. Docker rollback komutu (docker compose down + pull eski tag + up)
  2. DB migration rollback (dotnet ef database update [önceki-migration])
  3. Git revert komutu
  4. Kontrol listesi (health check URL'leri)
- Scripts/smoke-test.sh oluştur:
  curl health endpoint, DB bağlantı, Redis ping, RabbitMQ status
- Commit: docs(production): add rollback procedure + smoke test script [MEGA-P1-11]

P2-19: Prometheus alert rules YAML
- docker/prometheus/alert_rules.yml oluştur
- Rules: high_error_rate, low_disk_space, service_down, high_latency
- Commit: feat(monitoring): add Prometheus alert rules [MEGA-P2-19]

P2-20: EditForm validation genişlet (7→15)
- ÖNCE: grep -rn 'EditForm\|DataAnnotationsValidator' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l
- Mevcut 7 form'a ek olarak 8 yeni form'a EditForm+validation ekle
- Commit: feat(blazor): add EditForm validation to 8 forms [MEGA-P2-20]

Her görev bitince bana ÖNCE/SONRA sayılarını göster.
