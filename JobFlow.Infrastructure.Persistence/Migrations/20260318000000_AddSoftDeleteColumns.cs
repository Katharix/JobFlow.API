using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using JobFlow.Infrastructure.Persistence;

#nullable disable

namespace JobFlow.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(JobFlowDbContext))]
[Migration("20260318000000_AddSoftDeleteColumns")]
public partial class AddSoftDeleteColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @schema sysname;
            DECLARE @table sysname;
            DECLARE @sql nvarchar(max);

            DECLARE cur CURSOR FAST_FORWARD FOR
            SELECT DISTINCT c.TABLE_SCHEMA, c.TABLE_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.COLUMN_NAME = 'CreatedAt'
              AND c.TABLE_NAME <> '__EFMigrationsHistory';

            OPEN cur;
            FETCH NEXT FROM cur INTO @schema, @table;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @sql = N'';

                IF NOT EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema
                      AND TABLE_NAME = @table
                      AND COLUMN_NAME = 'IsActive'
                )
                BEGIN
                    SET @sql += N'ALTER TABLE [' + @schema + N'].[' + @table + N'] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_' + @table + N'_IsActive] DEFAULT(1);';
                END

                IF NOT EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema
                      AND TABLE_NAME = @table
                      AND COLUMN_NAME = 'DeactivatedAtUtc'
                )
                BEGIN
                    SET @sql += N'ALTER TABLE [' + @schema + N'].[' + @table + N'] ADD [DeactivatedAtUtc] datetime2 NULL;';
                END

                IF LEN(@sql) > 0
                BEGIN
                    EXEC sp_executesql @sql;
                END

                FETCH NEXT FROM cur INTO @schema, @table;
            END

            CLOSE cur;
            DEALLOCATE cur;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @schema sysname;
            DECLARE @table sysname;
            DECLARE @constraintName sysname;
            DECLARE @sql nvarchar(max);

            DECLARE cur CURSOR FAST_FORWARD FOR
            SELECT DISTINCT c.TABLE_SCHEMA, c.TABLE_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.COLUMN_NAME = 'CreatedAt'
              AND c.TABLE_NAME <> '__EFMigrationsHistory';

            OPEN cur;
            FETCH NEXT FROM cur INTO @schema, @table;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @sql = N'';

                SELECT @constraintName = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns col
                    ON col.default_object_id = dc.object_id
                INNER JOIN sys.tables t
                    ON t.object_id = col.object_id
                INNER JOIN sys.schemas s
                    ON s.schema_id = t.schema_id
                WHERE s.name = @schema
                  AND t.name = @table
                  AND col.name = 'IsActive';

                IF @constraintName IS NOT NULL
                BEGIN
                    SET @sql += N'ALTER TABLE [' + @schema + N'].[' + @table + N'] DROP CONSTRAINT [' + @constraintName + N'];';
                END

                IF EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema
                      AND TABLE_NAME = @table
                      AND COLUMN_NAME = 'IsActive'
                )
                BEGIN
                    SET @sql += N'ALTER TABLE [' + @schema + N'].[' + @table + N'] DROP COLUMN [IsActive];';
                END

                IF EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema
                      AND TABLE_NAME = @table
                      AND COLUMN_NAME = 'DeactivatedAtUtc'
                )
                BEGIN
                    SET @sql += N'ALTER TABLE [' + @schema + N'].[' + @table + N'] DROP COLUMN [DeactivatedAtUtc];';
                END

                IF LEN(@sql) > 0
                BEGIN
                    EXEC sp_executesql @sql;
                END

                SET @constraintName = NULL;
                FETCH NEXT FROM cur INTO @schema, @table;
            END

            CLOSE cur;
            DEALLOCATE cur;
            """);
    }
}
