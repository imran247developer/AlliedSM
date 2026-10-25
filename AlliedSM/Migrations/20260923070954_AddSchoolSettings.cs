using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlliedSM.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolSettings",
                columns: table => new
                {
                    SchoolSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JazzCashNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountsWhatsApp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrincipalMobile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DayEndReportTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSettings", x => x.SchoolSettingId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolSettings");
        }
    }
}
