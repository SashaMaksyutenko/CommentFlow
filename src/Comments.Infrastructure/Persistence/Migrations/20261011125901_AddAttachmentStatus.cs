using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Attachments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                // Edited by hand: files uploaded before this migration are already processed
                defaultValue: "Ready");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Attachments");
        }
    }
}
