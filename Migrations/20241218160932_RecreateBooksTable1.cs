using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class RecreateBooksTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    BookId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.None),
                    Title = table.Column<string>(nullable: false),
                    Author = table.Column<string>(nullable: false),
                    Publisher = table.Column<string>(nullable: false),
                    YearPublished = table.Column<int>(nullable: false),
                    Genre = table.Column<string>(nullable: false),
                    PriceBuy = table.Column<decimal>(nullable: false),
                    PriceBorrow = table.Column<decimal>(nullable: false),
                    IsBorrowable = table.Column<bool>(nullable: false),
                    IsDiscounted = table.Column<bool>(nullable: false),
                    DiscountEndDate = table.Column<DateTime>(nullable: true),
                    DiscountedPrice = table.Column<decimal>(nullable: true),
                    CopiesAvailable = table.Column<int>(nullable: false),
                    AgeLimit = table.Column<int>(nullable: false),
                    CoverImage = table.Column<string>(nullable: false),
                    Popularity = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.BookId);
                });
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }

    }
}
