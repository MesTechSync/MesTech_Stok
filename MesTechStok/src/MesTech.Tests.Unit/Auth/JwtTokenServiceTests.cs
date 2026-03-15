using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MesTech.Tests.Unit.Auth;

/// <summary>
/// JwtTokenService unit tests — Dalga 9 PoC.
/// DEV 4 gercek JwtTokenService olusturana kadar inline stub ile test.
///
/// JwtTokenService expected location:
///   MesTech.Infrastructure/Auth/JwtTokenService.cs
///
/// Expected interface:
///   string GenerateToken(Guid userId, Guid tenantId, string userName)
///   (bool isValid, Guid? userId, Guid? tenantId) ValidateToken(string token)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "JwtAuth")]
public class JwtTokenServiceTests
{
    private static readonly Guid TestUserId =
        Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid TestTenantId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IConfiguration CreateTestConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-minimum-32-characters-long!!",
                ["Jwt:Issuer"] = "mestech-test",
                ["Jwt:Audience"] = "mestech-clients",
                ["Jwt:ExpiryMinutes"] = "60"
            }).Build();

    private static PocJwtTokenService CreateSut() => new(CreateTestConfig());

    // ══════════════════════════════════════════════════════════════
    //  Test 1: GenerateToken returns non-empty JWT string
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// GenerateToken bos olmayan, "eyJ" ile baslayan gecerli JWT donmeli.
    /// JWT format: header.payload.signature (3 parca, base64url).
    /// </summary>
    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyJwtString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var token = sut.GenerateToken(TestUserId, TestTenantId, "test-user");

        // Assert
        token.Should().NotBeNullOrWhiteSpace("JWT token bos olmamali");
        token.Should().StartWith("eyJ", "JWT header base64url ile 'eyJ' baslar");
        token.Split('.').Should().HaveCount(3,
            "JWT format: header.payload.signature — 3 parca olmali");
    }

    // ══════════════════════════════════════════════════════════════
    //  Test 2: ValidateToken with valid token returns correct claims
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gecerli token dogrulama — userId ve tenantId claim'leri dogru donmeli.
    /// </summary>
    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrueAndCorrectClaims()
    {
        // Arrange
        var sut = CreateSut();
        var token = sut.GenerateToken(TestUserId, TestTenantId, "test-user");

        // Act
        var (isValid, userId, tenantId) = sut.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue("dogru secret ile uretilmis token gecerli olmali");
        userId.Should().Be(TestUserId, "userId claim'i orijinal deger olmali");
        tenantId.Should().Be(TestTenantId, "tenantId claim'i orijinal deger olmali");
    }

    // ══════════════════════════════════════════════════════════════
    //  Test 3: ValidateToken with invalid token returns false
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gecersiz/bozuk token ile dogrulama — false donmeli, userId/tenantId null.
    /// </summary>
    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.payload";

        // Act
        var (isValid, userId, tenantId) = sut.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse("gecersiz token dogrulama false donmeli");
        userId.Should().BeNull("gecersiz token icin userId null olmali");
        tenantId.Should().BeNull("gecersiz token icin tenantId null olmali");
    }

    // ══════════════════════════════════════════════════════════════
    //  Test 4: ValidateToken with tampered token returns false
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tampered token (imza degistirilmis) — false donmeli.
    /// Signature kisminin son karakterini degistirip tamper simulasyonu.
    /// </summary>
    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnFalse()
    {
        // Arrange
        var sut = CreateSut();
        var validToken = sut.GenerateToken(TestUserId, TestTenantId, "test-user");

        // Tamper: signature kisminin son karakterini degistir
        var parts = validToken.Split('.');
        var signature = parts[2];
        var lastChar = signature[^1];
        var tamperedChar = lastChar == 'A' ? 'B' : 'A';
        var tamperedSignature = signature[..^1] + tamperedChar;
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        // Act
        var (isValid, userId, tenantId) = sut.ValidateToken(tamperedToken);

        // Assert
        isValid.Should().BeFalse("tamper edilmis token dogrulama false donmeli");
        userId.Should().BeNull("tampered token icin userId null olmali");
        tenantId.Should().BeNull("tampered token icin tenantId null olmali");
    }

    // ══════════════════════════════════════════════════════════════
    //  PoC stub — DEV 4 gercek JwtTokenService olusturunca
    //  bu stub kaldirilip gercek sinif referans edilecek.
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Temporary PoC JWT token service stub.
    /// Mirrors the expected JwtTokenService behavior from DEV 4.
    /// Will be replaced when MesTech.Infrastructure/Auth/JwtTokenService.cs is created.
    /// </summary>
    private sealed class PocJwtTokenService
    {
        private readonly IConfiguration _config;

        public PocJwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Guid userId, Guid tenantId, string userName)
        {
            var secret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret config eksik");
            var issuer = _config["Jwt:Issuer"] ?? "mestech";
            var audience = _config["Jwt:Audience"] ?? "mestech-clients";
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("tenantId", tenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (bool isValid, Guid? userId, Guid? tenantId) ValidateToken(string token)
        {
            var secret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret config eksik");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "mestech",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "mestech-clients",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validationParameters, out _);

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var tenantIdClaim = principal.FindFirst("tenantId")?.Value;

                var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : (Guid?)null;
                var tenantId = Guid.TryParse(tenantIdClaim, out var tid) ? tid : (Guid?)null;

                return (true, userId, tenantId);
            }
            catch
            {
                return (false, null, null);
            }
        }
    }
}
