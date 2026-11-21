using Bookly.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Books;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);

        return app;
    }

    static async Task<Ok<List<BookResponse>>> GetAll(BooklyDbContext db, CancellationToken ct)
    {
        var books = await db.Books.AsNoTracking().OrderBy(b => b.Title).ToListAsync(ct);
        return TypedResults.Ok(books.Select(BookResponse.From).ToList());
    }

    static async Task<Results<Ok<BookResponse>, NotFound>> GetById(int id, BooklyDbContext db, CancellationToken ct)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return book is null ? TypedResults.NotFound() : TypedResults.Ok(BookResponse.From(book));
    }

    static async Task<Results<Created<BookResponse>, ProblemHttpResult>> Create(
        BookRequest request, BooklyDbContext db, CancellationToken ct)
    {
        if (request.Isbn13 is not null && await db.Books.AnyAsync(b => b.Isbn13 == request.Isbn13, ct))
            return IsbnConflict(request.Isbn13);

        var now = DateTime.UtcNow;
        var book = new Book { CreatedAt = now, UpdatedAt = now };
        request.ApplyTo(book);

        db.Books.Add(book);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/books/{book.Id}", BookResponse.From(book));
    }

    static async Task<Results<Ok<BookResponse>, NotFound, ProblemHttpResult>> Update(
        int id, BookRequest request, BooklyDbContext db, CancellationToken ct)
    {
        var book = await db.Books.FindAsync([id], ct);
        if (book is null)
            return TypedResults.NotFound();

        if (request.Isbn13 is not null &&
            await db.Books.AnyAsync(b => b.Id != id && b.Isbn13 == request.Isbn13, ct))
            return IsbnConflict(request.Isbn13);

        request.ApplyTo(book);
        book.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(BookResponse.From(book));
    }

    static async Task<Results<NoContent, NotFound>> Delete(int id, BooklyDbContext db, CancellationToken ct)
    {
        var deleted = await db.Books.Where(b => b.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    static ProblemHttpResult IsbnConflict(string isbn) =>
        TypedResults.Problem(
            title: "Książka o tym ISBN już istnieje",
            detail: $"ISBN {isbn} jest już w katalogu.",
            statusCode: StatusCodes.Status409Conflict);
}