# Unit Test Kuralları (PlanningApp.Test)

## Kapsam ve Referanslar
- Referanslar: SADECE PlanningApp.Domain, PlanningApp.Application.
- Framework: SADECE xUnit. Mocking: Moq. Assertion: FluentAssertions.
- Kapsam: Domain invariant'ları + Application Handler'ları, tüm
  repository/dış servis bağımlılıkları MOCK'lanarak test edilir.
- Gerçek veritabanı, gerçek dosya sistemi, gerçek ağ çağrısı YAPILMAZ.

## Klasör Yapısı
- Test dosyaları, test edilen entity/handler'ın Domain/Application
  katmanındaki feature klasörüyle eşleşecek şekilde organize edilir
  (örn. `Tasks/PlanningTaskTests.cs`, `Weeks/PlanningWeekTests.cs`),
  tek bir düz klasörde toplanmaz.

## İsimlendirme
- Test metod isimleri de (diğer tüm metodlar gibi) İngilizce olmalıdır —
  sadece string literal içindeki hata mesajları ve yorum satırları Türkçe
  kalır.
- Format: `MethodName_Scenario_ExpectedResult`
  (örn. `Complete_OnPooledTask_ThrowsDomainException`).

## Yapı
- Her test metodu Arrange-Act-Assert (AAA) bloklarına ayrılır ve bu üç
  blok yorum satırıyla (`// Arrange`, `// Act`, `// Assert`) işaretlenir.
- Aynı senaryonun birden fazla benzer girdiyle test edildiği durumlarda
  (örn. boş/boşluklu/null başlık) ayrı `[Fact]`'ler yerine `[Theory]` +
  `[InlineData]` kullanılır.
- Her test sınıfı, tekrarlayan kurulum kodunu private static helper
  metodlarda toplar (örn. `CreateSampleTask()`, `CreateCompletedTask()`).
  Bu helper'lar test sınıfının en altında, "Test Helpers" başlığı altında
  yer alır.
- `DomainException` fırlatan senaryolarda, sadece exception tipini değil
  mesajını da doğrula (`.Should().Throw<DomainException>().WithMessage(...)`)
  — bu, yanlış invariant'ın yanlışlıkla doğru görünmesini engeller.

## Kapsam Beklentisi
- Her yeni Command/Query Handler'ı için en az 1 happy-path + 1
  validation-failure senaryosu.
- Her entity invariant'ı için en az 1 "kural ihlali reddedilir" senaryosu
  (bkz. entity_rules.md → Test Beklentisi).
- Yeni bir entity/handler'ın testleri, elimizde zaten yazılmış bir
  "referans implementasyon" testi varsa (örn. PlanningTaskTests.cs),
  Claude Code'a bu referansla birlikte yazdırılır — sıfırdan elle
  yazılmaz, tekrar eden bir iştir.