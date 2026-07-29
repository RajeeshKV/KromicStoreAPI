using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeLegacyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add legacy color columns to Themes table if missing
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    -- Add PrimaryColor column if missing (legacy field for backward compatibility)
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='PrimaryColor'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""PrimaryColor"" character varying(7);
                    END IF;
                    
                    -- Add SecondaryColor column if missing (legacy field for backward compatibility)
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='SecondaryColor'
                    ) THEN
                        ALTER TABLE ""Themes"" ADD COLUMN ""SecondaryColor"" character varying(7);
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='PrimaryColor'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""PrimaryColor"";
                    END IF;
                    
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='Themes' AND column_name='SecondaryColor'
                    ) THEN
                        ALTER TABLE ""Themes"" DROP COLUMN ""SecondaryColor"";
                    END IF;
                END $$;");
        }
    }
}
