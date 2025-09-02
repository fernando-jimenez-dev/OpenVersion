using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Versions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    IdentifierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReleaseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Meta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastUpdated = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Versions_ProjectId_IdentifierName",
                table: "Versions",
                columns: new[] { "ProjectId", "IdentifierName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Versions");
        }
    }
}
