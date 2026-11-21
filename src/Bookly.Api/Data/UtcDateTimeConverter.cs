using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookly.Api.Data;

// Zapis: zawsze UTC. Odczyt: oznacz wartość jako UTC, żeby JSON miał sufiks "Z".
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));