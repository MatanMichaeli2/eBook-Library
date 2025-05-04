using Microsoft.EntityFrameworkCore.Migrations;

public partial class ReorderUsersTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop existing primary key constraint (you'll need to find its name)
        migrationBuilder.DropPrimaryKey(
            name: "PK_Users",
            table: "Users");

        // Create new clustered index
        migrationBuilder.Sql(@"
            CREATE CLUSTERED INDEX IX_Users_Role_ID ON Users
            (
                CASE WHEN Role = 'Admin' THEN 0 ELSE 1 END ASC,
                ID ASC
            );
        ");

        // Re-create primary key as non-clustered
        migrationBuilder.AddPrimaryKey(
            name: "PK_Users",
            table: "Users",
            column: "ID")
            .Annotation("SqlServer:Clustered", false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert changes if needed
        migrationBuilder.DropPrimaryKey(
            name: "PK_Users",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_Role_ID",
            table: "Users");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Users",
            table: "Users",
            column: "ID")
            .Annotation("SqlServer:Clustered", true);
    }
}