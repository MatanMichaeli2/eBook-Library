using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebApplication2.Models;
using WebApplication2.Helpers;

namespace WebApplication2.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BorrowedBook> BorrowedBooks { get; set; }
        public DbSet<PurchasedBook> PurchasedBooks { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<WebSiteFeedback> WebSiteFeedbacks { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public static void SeedData(LibraryContext context)
        {
            try
            {
                // If additional seeding logic is needed for other entities, it can go here.
                Console.WriteLine("Books are now loaded directly from the database. Skipping JSON file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during seeding: {ex.Message}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
 
            modelBuilder.Entity<Book>()
                .Property(b => b.BookId)
                .ValueGeneratedNever();

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Book)
                .WithMany()
                .HasForeignKey(t => t.BookId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Book)
                .WithMany()
                .HasForeignKey(f => f.BookId);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId);

            modelBuilder.Entity<Feedback>()
            .Property(f => f.FeedbackId)
            .ValueGeneratedOnAdd();


            modelBuilder.Entity<WebSiteFeedback>()
                .Property(f => f.SubmittedOn)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<BorrowedBook>()
             .HasOne(b => b.Book)
             .WithMany()
             .HasForeignKey(b => b.BookId)
             .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<WaitingList>()
                .HasOne(w => w.Book)
                .WithMany()
                .HasForeignKey(w => w.BookId);

            modelBuilder.Entity<WaitingList>()
                .HasOne(w => w.User)
                .WithMany() 
                .HasForeignKey(w => w.UserId);

        }
    }
}
