using Microsoft.EntityFrameworkCore.Migrations;

namespace WebApp.Migrations
{
    public partial class docker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileHistory",
                columns: table => new
                {
                    HistoryId = table.Column<string>(nullable: false),
                    ContextId = table.Column<string>(nullable: false),
                    ProductVersion = table.Column<string>(nullable: false),
                    WorkloadName = table.Column<string>(nullable: false),
                    Argument = table.Column<string>(nullable: false),
                    Requests = table.Column<int>(nullable: false),
                    Errors = table.Column<int>(nullable: false),
                    Duration = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileHistory", x => x.HistoryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileHistory");
        }
    }
}
