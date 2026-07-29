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
            // Safe PostgreSQL migration - drops TenantId column and related objects if they exist
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
                    
                    -- Create new FK if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE table_name='Themes' AND constraint_name='FK_Themes_Tenants_OwnerTenantId'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD CONSTRAINT ""FK_Themes_Tenants_OwnerTenantId""
                        FOREIGN KEY (""OwnerTenantId"") REFERENCES ""Tenants""(""Id"") ON DELETE CASCADE;
                    END IF;
                END $$;");

            migrationBuilder.CreateIndex(
                name: "IX_Themes_OwnerTenantId_IsActive",
                table: "Themes",
                columns: new[] { "OwnerTenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Themes_OwnerTenantId_IsPublic",
                table: "Themes",
                columns: new[] { "OwnerTenantId", "IsPublic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Themes_OwnerTenantId_IsActive",
                table: "Themes");

            migrationBuilder.DropIndex(
                name: "IX_Themes_OwnerTenantId_IsPublic",
                table: "Themes");

            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    ALTER TABLE ""Themes"" DROP CONSTRAINT IF EXISTS ""FK_Themes_Tenants_OwnerTenantId"";
                END $$;");
        }
    }
}
