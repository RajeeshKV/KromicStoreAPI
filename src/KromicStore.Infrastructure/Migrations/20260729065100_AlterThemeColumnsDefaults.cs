using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterThemeColumnsDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure IsActive column has a default value
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Check if IsActive column exists and update null values to true
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='IsActive'
                    ) THEN
                        -- Set any null IsActive values to true
                        UPDATE ""Themes"" SET ""IsActive"" = true WHERE ""IsActive"" IS NULL;
                        
                        -- Alter column to have a default if it doesn't already
                        ALTER TABLE ""Themes"" ALTER COLUMN ""IsActive"" SET DEFAULT true;
                    END IF;
                    
                    -- Ensure DefinitionJson has a default empty object
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='DefinitionJson'
                    ) THEN
                        UPDATE ""Themes"" SET ""DefinitionJson"" = '{}' WHERE ""DefinitionJson"" IS NULL OR ""DefinitionJson"" = '';
                        ALTER TABLE ""Themes"" ALTER COLUMN ""DefinitionJson"" SET DEFAULT '{}';
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Remove defaults (downgrade)
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='IsActive'
                    ) THEN
                        ALTER TABLE ""Themes"" ALTER COLUMN ""IsActive"" DROP DEFAULT;
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='DefinitionJson'
                    ) THEN
                        ALTER TABLE ""Themes"" ALTER COLUMN ""DefinitionJson"" DROP DEFAULT;
                    END IF;
                END $$;");
        }
    }
}
