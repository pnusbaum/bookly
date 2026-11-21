using Bookly.Api.Books;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Data;

public class BooklyDbContext(DbContextOptions<BooklyDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.Property(b => b.Title).HasMaxLength(500);
            book.Property(b => b.Subtitle).HasMaxLength(500);
            book.Property(b => b.Authors).HasMaxLength(500);
            book.Property(b => b.Isbn13).HasMaxLength(13);
            book.HasIndex(b => b.Isbn13).IsUnique();
            book.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            book.Property(b => b.Location).HasMaxLength(100);
            book.Property(b => b.Borrower).HasMaxLength(200);
        });
    }
}