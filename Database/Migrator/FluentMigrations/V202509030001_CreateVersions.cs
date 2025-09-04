using FluentMigrator;

namespace Migrator.FluentMigrations;

// Initial FluentMigrator migration mirroring the EF Core model for the "Versions" table.
[Migration(202509030001, "Create Versions table with unique index and required columns")]
public sealed class V202509030001_CreateVersions : Migration
{
    public override void Up()
    {
        // Table: Versions
        Create.Table("Versions")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("ProjectId").AsInt64().NotNullable()
            .WithColumn("IdentifierName").AsString(200).NotNullable()
            .WithColumn("ReleaseNumber").AsString(100).NotNullable()
            .WithColumn("Meta").AsString(200).Nullable()
            // EF maps LastUpdated as text (ISO 8601 string); use PostgreSQL text type explicitly
            .WithColumn("LastUpdated").AsCustom("text").NotNullable()
            .WithColumn("ConcurrencyToken").AsGuid().NotNullable();

        // Unique index on (ProjectId, IdentifierName)
        Create.Index("IX_Versions_ProjectId_IdentifierName")
            .OnTable("Versions")
            .OnColumn("ProjectId").Ascending()
            .OnColumn("IdentifierName").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        // Drop index then table for clean rollback
        Delete.Index("IX_Versions_ProjectId_IdentifierName").OnTable("Versions");
        Delete.Table("Versions");
    }
}