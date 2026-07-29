using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingThemeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to Themes table in a safe way (PostgreSQL idempotent)
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Add CreatedByUserId column if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='CreatedByUserId'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""CreatedByUserId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                    
                    -- Add LastModifiedByUserId column if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='LastModifiedByUserId'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""LastModifiedByUserId"" uuid;
                    END IF;
                    
                    -- Add SourceThemeId column if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='SourceThemeId'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""SourceThemeId"" uuid;
                    END IF;
                    
                    -- Add OwnerTenantId column if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='OwnerTenantId'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""OwnerTenantId"" uuid;
                    END IF;
                    
                    -- Add IsPublic column if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='IsPublic'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""IsPublic"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Drop columns if they exist (downgrade not supported, only removes added columns)
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='CreatedByUserId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""CreatedByUserId"";
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='LastModifiedByUserId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""LastModifiedByUserId"";
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='SourceThemeId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""SourceThemeId"";
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='OwnerTenantId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""OwnerTenantId"";
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='IsPublic'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""IsPublic"";
                    END IF;
                END $$;");
        }
    }
}
