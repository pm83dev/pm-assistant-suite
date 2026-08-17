using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Indirizzo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Progetti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descrizione = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progetti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Progetti_Clienti_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clienti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Note",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DataCreazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Contenuto = table.Column<string>(type: "TEXT", nullable: false),
                    Titolo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProgettoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Note_Progetti_ProgettoId",
                        column: x => x.ProgettoId,
                        principalTable: "Progetti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OreLavorate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ore = table.Column<decimal>(type: "TEXT", nullable: false),
                    Descrizione = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProgettoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OreLavorate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OreLavorate_Progetti_ProgettoId",
                        column: x => x.ProgettoId,
                        principalTable: "Progetti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Note_ProgettoId",
                table: "Note",
                column: "ProgettoId");

            migrationBuilder.CreateIndex(
                name: "IX_OreLavorate_ProgettoId",
                table: "OreLavorate",
                column: "ProgettoId");

            migrationBuilder.CreateIndex(
                name: "IX_Progetti_ClienteId",
                table: "Progetti",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Note");

            migrationBuilder.DropTable(
                name: "OreLavorate");

            migrationBuilder.DropTable(
                name: "Progetti");

            migrationBuilder.DropTable(
                name: "Clienti");
        }
    }
}
