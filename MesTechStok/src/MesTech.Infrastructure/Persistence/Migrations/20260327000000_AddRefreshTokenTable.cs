using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesTech.Infrastructure.Persistence.Migrations;

/// <summary>
/// Manual SQL migration — EF snapshot drift prevents auto-generation (G083).
/// Creates refresh_tokens table for JWT refresh token rotation (OWASP ASVS V3.3).
/// </summary>
public partial class AddRefreshTokenTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS refresh_tokens (
                "Id"                    uuid NOT NULL DEFAULT gen_random_uuid(),
                "TenantId"              uuid NOT NULL,
                "UserId"                uuid NOT NULL,
                "TokenHash"             character varying(128) NOT NULL,
                "ExpiresAt"             timestamp with time zone NOT NULL,
                "IsRevoked"             boolean NOT NULL DEFAULT false,
                "RevokedAt"             timestamp with time zone,
                "RevokedReason"         character varying(256),
                "ReplacedByTokenHash"   character varying(128),
                "IpAddress"             character varying(45),
                "UserAgent"             character varying(512),
                "CreatedAt"             timestamp with time zone NOT NULL DEFAULT now(),
                "UpdatedAt"             timestamp with time zone,
                CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_refresh_tokens_token_hash
                ON refresh_tokens ("TokenHash");

            CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_active
                ON refresh_tokens ("UserId", "IsRevoked");

            CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at
                ON refresh_tokens ("ExpiresAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS refresh_tokens;
            """);
    }
}
