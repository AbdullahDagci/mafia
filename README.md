# Sus Payı

Barafranca/Omerta tarzı, metin tabanlı, sezonluk mobil mafya oyunu. Sezonlar 12 hafta sürer; Türkiye'de 8 şehirde geçer.

| Katman | Teknoloji |
| --- | --- |
| Sunucu | ASP.NET Core (.NET 10), minimal API |
| Veritabanı | PostgreSQL 17, EF Core + Npgsql |
| İstemci | Flutter (henüz başlanmadı) |

## Klasör yapısı

```
backend/
  src/SusPayi.Domain          Saf oyun kuralları (paket bağımlılığı yok)
  src/SusPayi.Infrastructure  EF Core, veritabanı, rastgelelik
  src/SusPayi.Api             HTTP uçları, kimlik doğrulama, hız sınırı
  tests/SusPayi.Domain.Tests  Kural testleri (xUnit v3)
```

## Yerelde çalıştırma

Gerekenler: .NET 10 SDK, Docker.

```bash
docker compose up -d postgres

cd backend
dotnet tool install --global dotnet-ef --version "10.*"

# İlk migration (bir kez; çıkan Migrations klasörünü commit'leyin)
dotnet ef migrations add InitialCreate \
  --project src/SusPayi.Infrastructure --startup-project src/SusPayi.Api

dotnet run --project src/SusPayi.Api
```

Development ortamında uygulama açılışta migration'ları uygular ve çalışan sezon yoksa "İstanbul Kuruluşu" sezonunu (84 gün, son 14 gün final evresi) oluşturur. API dokümanı: `http://localhost:5080/openapi/v1.json`.

Testler:

```bash
cd backend && dotnet test --solution SusPayi.slnx
```

## Uç noktalar (`/api/v1`)

| Yöntem | Yol | Açıklama |
| --- | --- | --- |
| POST | `/auth/register` | Hesap aç (e-posta, görünen ad, parola) |
| POST | `/auth/login` | Giriş; access + refresh token döner |
| POST | `/auth/refresh` | Token yenile |
| GET | `/seasons/current` | Çalışan sezon |
| POST | `/seasons/current/characters` | Bu sezon için karakter aç |
| GET | `/me/character` | Karakterim |
| GET | `/crimes` | Suç listesi ve güncel başarı şansım |
| POST | `/crimes/{crimeId}/attempts` | Suç işle |
| GET | `/jail?cursor=&limit=` | Hapistekiler (sayfalı) |
| POST | `/jail/{inmateId}/bust` | Hapisten kurtar |
| POST | `/jail/bribe` | Rüşvetle çık |
| GET | `/health/live`, `/health/ready` | Sağlık kontrolü (kök yolda) |

Kural ihlalleri (bekleme süresi, hapis, yetersiz para) `application/problem+json` döner; `code` alanı makine tarafından okunur, varsa `retryAt` tekrar denenebilecek zamanı verir.

## Üretim notları

- Bağlantı dizesi ortam değişkeniyle verilir: `ConnectionStrings__Game`. Depoda gizli bilgi tutulmaz.
- Migration'lar uygulama açılışında değil, dağıtım adımında uygulanır:
  `dotnet ef migrations script --idempotent -o migrate.sql` ile üretilen betik çalıştırılır.
- Birden fazla sunucu örneğinde token'ların geçerli kalması için Data Protection anahtarları ortak bir yerde tutulmalıdır (`DataProtection__KeysPath` ile paylaşılan bir disk ya da veritabanı/Redis).
- Ters vekil (reverse proxy) arkasında hız sınırının istemci IP'sini doğru görmesi için forwarded headers ayarlanmalıdır.

## Durum

İlk dilim: hesap, sezon, karakter, suç, hapis (kurtarma ve rüşvet). Sıradaki modüller: araba hırsızlığı, kaçakçılık, şehirler arası yolculuk, banka, hastane, kumarhane, aile, harita ve baskınlar, PvP, onur listesi.
