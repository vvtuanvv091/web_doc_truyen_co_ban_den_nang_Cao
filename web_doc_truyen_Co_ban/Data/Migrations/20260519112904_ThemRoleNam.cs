using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_doc_truyen_Co_ban.Migrations
{
    /// <inheritdoc />
    public partial class ThemRoleNam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleTarget",
                table: "ThongBaos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleTarget",
                table: "ThongBaos");
        }
    }
}
