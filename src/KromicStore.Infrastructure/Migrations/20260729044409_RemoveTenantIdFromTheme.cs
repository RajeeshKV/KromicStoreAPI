using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantIdFromTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe PostgreSQL migration - only drops TenantId if it exists
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Drop old foreign key if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE table_name='Themes' AND constraint_name='FK_Themes_Tenants_TenantId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP CONSTRAINT ""FK_Themes_Tenants_TenantId"";
                    END IF;
                    
                    -- Drop old indexes if they exist
                    DROP INDEX IF EXISTS ""IX_Themes_TenantId"";
                    DROP INDEX IF EXISTS ""IX_Themes_TenantId_IsActive"";
                    DROP INDEX IF EXISTS ""IX_Themes_TenantId_IsPublic"";
                    
                    -- Drop TenantId column if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='TenantId'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""TenantId"";
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Downgrade not supported for this destructive migration
        }
    }
}
