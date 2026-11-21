namespace Bookly.Api.Books;

public enum ReadingStatus
{
    ToRead,
    Reading,
    Read,
}

public class Book
{
    public int Id { get; set; }
    public string? Isbn13 { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string Authors { get; set; } = "";
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
    public string? CoverUrl { get; set; }
    public ReadingStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Borrower { get; set; }
    public DateTime? BorrowedAt { get; set; }
    public int? Rating { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}