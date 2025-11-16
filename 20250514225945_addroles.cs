using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JWTRefreshTokenInDotNet6.Migrations
{
    /// <inheritdoc />
    public partial class addroles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"INSERT INTO [dbo].[AspNetRoles] VALUES ('{Guid.NewGuid()}', 'Admin', 'ADMIN', '{Guid.NewGuid()}')"
            );
            migrationBuilder.Sql(
                $"INSERT INTO [dbo].[AspNetRoles] VALUES ('{Guid.NewGuid()}', 'Vendor', 'VENDOR', '{Guid.NewGuid()}')"
            );
            migrationBuilder.Sql(
                $"INSERT INTO [dbo].[AspNetRoles] VALUES ('{Guid.NewGuid()}', 'Customer', 'CUSTOMER', '{Guid.NewGuid()}')"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM [dbo].[AspNetRoles] WHERE Name = 'Admin'");
            migrationBuilder.Sql($"DELETE FROM [dbo].[AspNetRoles] WHERE Name = 'Vendor'");
            migrationBuilder.Sql($"DELETE FROM [dbo].[AspNetRoles] WHERE Name = 'Customer'");
        }
    }
}
