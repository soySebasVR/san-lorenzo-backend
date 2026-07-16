-- Drops all foreign keys then all tables in the current database.
-- DEV ONLY: wipes everything so schema.sql can recreate the new structure.
SET NOCOUNT ON;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.'
            + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
FROM sys.foreign_keys;
EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql += 'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';'
FROM sys.tables;
EXEC sp_executesql @sql;
