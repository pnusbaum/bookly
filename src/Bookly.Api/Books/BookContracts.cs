using System.ComponentModel.DataAnnotations;

namespace Bookly.Api.Books;

public record BookRequest(
    [RegularExpression(@"^\d{13}$", ErrorMessage = "ISBN musi składać się z 13 cyfr.")] string? Isbn13,
    [Required, StringLength(500)] string Title,
    [StringLength(500)] string? Subtitle,
    [Required, StringLength(500)] string Authors,
    [StringLength(200)] string? Publisher,
    [Range(1, 2100)] int? PublishedYear,
    [Range(1, 20000)] int? PageCount,
    [StringLength(10)] string? Language,
    [Url, StringLength(2000)] string? CoverUrl,
    ReadingStatus Status,
    [StringLength(100)] string? Location,
    [StringLength(200)] string? Borrower,
    DateTime? BorrowedAt,
    [Range(1, 5)] int? Rating,
    [StringLength(4000)] string? Notes)
{
    public void ApplyTo(Book book)
    {
        book.Isbn13 = Isbn13;
        book.Title = Title;
        book.Subtitle = Subtitle;
        book.Authors = Authors;
        book.Publisher = Publisher;
        book.PublishedYear = PublishedYear;
        book.PageCount = PageCount;
        book.Language = Language;
        book.CoverUrl = CoverUrl;
        book.Status = Status;
        book.Location = Location;
        book.Borrower = Borrower;
        book.BorrowedAt = BorrowedAt?.ToUniversalTime();
        book.Rating = Rating;
        book.Notes = Notes;
    }
}

public record BookResponse(
    int Id, string? Isbn13, string Title, string? Subtitle, string Authors,
    string? Publisher, int? PublishedYear, int? PageCount, string? Language,
    string? CoverUrl, ReadingStatus Status, string? Location, string? Borrower,
    DateTime? BorrowedAt, int? Rating, string? Notes,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    public static BookResponse From(Book b) => new(
        b.Id, b.Isbn13, b.Title, b.Subtitle, b.Authors,
        b.Publisher, b.PublishedYear, b.PageCount, b.Language,
        b.CoverUrl, b.Status, b.Location, b.Borrower,
        b.BorrowedAt, b.Rating, b.Notes,
        b.CreatedAt, b.UpdatedAt);
}