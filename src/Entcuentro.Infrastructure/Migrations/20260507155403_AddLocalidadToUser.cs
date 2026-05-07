using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entcuentro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalidadToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Localidad",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Localidad",
                table: "AspNetUsers");
        }
    }
}
