using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlliedSM.Migrations
{
    /// <inheritdoc />
    public partial class StudentAdmissionFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransportFee",
                table: "Students",
                newName: "AdmissionFee");

            migrationBuilder.AddColumn<decimal>(
                name: "AdmissionFee",
                table: "FeeChallans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdmissionFee",
                table: "FeeChallans");

            migrationBuilder.RenameColumn(
                name: "AdmissionFee",
                table: "Students",
                newName: "TransportFee");
        }
    }
}
