using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electrons.Net8.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Awards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Award = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    YearStart = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    YearEnd = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Finish = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Current = table.Column<bool>(type: "bit", nullable: false),
                    Bats = table.Column<int>(type: "int", nullable: false),
                    Throws = table.Column<int>(type: "int", nullable: false),
                    POS1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    POS2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    POS3 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NickName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HomeTown = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Divorces = table.Column<int>(type: "int", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<short>(type: "smallint", nullable: false),
                    Uniform = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Awards");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "History");

            migrationBuilder.DropTable(
                name: "Player");
        }
    }
}
