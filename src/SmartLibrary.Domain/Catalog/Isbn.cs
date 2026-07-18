namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// ISBN normalization and validation. All lookups and storage use the 13-digit form;
/// 10-digit input is validated and converted (978 prefix + recomputed check digit).
/// </summary>
public static class Isbn
{
    /// <summary>
    /// Strips hyphens/spaces, validates, and returns the canonical ISBN-13.
    /// Returns null when the input is not a valid ISBN-10 or ISBN-13.
    /// </summary>
    public static string? TryNormalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = new string([.. value.Where(c => char.IsAsciiDigit(c) || c is 'X' or 'x')]).ToUpperInvariant();

        return cleaned.Length switch
        {
            10 when IsValidIsbn10(cleaned) => ConvertIsbn10To13(cleaned),
            13 when IsValidIsbn13(cleaned) => cleaned,
            _ => null,
        };
    }

    public static bool IsValid(string? value) => TryNormalize(value) is not null;

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digit;
            if (isbn[i] == 'X')
            {
                if (i != 9)
                {
                    return false;
                }

                digit = 10;
            }
            else if (char.IsAsciiDigit(isbn[i]))
            {
                digit = isbn[i] - '0';
            }
            else
            {
                return false;
            }

            sum += (10 - i) * digit;
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Any(c => !char.IsAsciiDigit(c)))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }

    private static string ConvertIsbn10To13(string isbn10)
    {
        var core = "978" + isbn10[..9];
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += (core[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        var check = (10 - (sum % 10)) % 10;
        return core + (char)('0' + check);
    }
}
