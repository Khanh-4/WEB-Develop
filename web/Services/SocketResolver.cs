using System.Text.RegularExpressions;

namespace TechSpecs.Services;

/// <summary>
/// Best-effort CPU/motherboard socket resolver.
///
/// The scraped DB frequently stores "Unknown" for a board's socket, which let
/// incompatible parts slip through the Builder's compatibility filter (e.g. an
/// AM4 board shown for an LGA1851 CPU — bug #14). When the declared socket is
/// missing we infer it from the chipset (motherboards) or the model number
/// (CPUs) embedded in the product name. If we still can't tell, we return the
/// original value so the filter stays permissive rather than hiding everything.
/// </summary>
public static class SocketResolver
{
    public static bool IsKnown(string? s) =>
        !string.IsNullOrWhiteSpace(s) && s != "Unknown" && s != "Universal";

    // Motherboard chipset → socket. Tokens are matched as substrings of the
    // upper-cased product name; AM5/AM4 are checked before Intel so an "X870"
    // board never falls through to an LGA bucket.
    private static readonly (string Socket, string[] Chipsets)[] BoardMap =
    {
        ("AM5",     new[] { "A620", "B650", "B840", "B850", "X670", "X870" }),
        ("AM4",     new[] { "A320", "A520", "B350", "B450", "B550", "X370", "X470", "X570" }),
        ("LGA1851", new[] { "B860", "H810", "Q870", "W880", "Z890" }),
        ("LGA1700", new[] { "H610", "B660", "H670", "B760", "H770", "Z690", "Z790" }),
        ("LGA1200", new[] { "H410", "B460", "H470", "Z490", "H510", "B560", "H570", "Z590" }),
    };

    public static string ResolveBoardSocket(string? declared, string? name)
    {
        if (IsKnown(declared)) return declared!;
        var n = (name ?? string.Empty).ToUpperInvariant();
        foreach (var (socket, chipsets) in BoardMap)
            if (chipsets.Any(n.Contains))
                return socket;
        return declared ?? "Unknown";
    }

    public static string ResolveCpuSocket(string? declared, string? name)
    {
        if (IsKnown(declared)) return declared!;
        var n = (name ?? string.Empty).ToUpperInvariant();

        // Intel Core Ultra (Series 2) → LGA1851
        if (n.Contains("CORE ULTRA") || n.Contains("ULTRA 5") || n.Contains("ULTRA 7") || n.Contains("ULTRA 9"))
            return "LGA1851";

        // AMD Ryzen — first digit of the 4-digit model selects the platform
        var ryzen = Regex.Match(n, @"RYZEN\s+\d\s+(\d)\d{3}");
        if (ryzen.Success)
            return ryzen.Groups[1].Value is "7" or "8" or "9" ? "AM5" : "AM4";

        // Intel Core i3/i5/i7/i9 — generation prefix selects the socket
        var intel = Regex.Match(n, @"I[3579][\s-]*(\d{2})\d{2,3}");
        if (intel.Success && int.TryParse(intel.Groups[1].Value, out var gen))
        {
            if (gen is 12 or 13 or 14) return "LGA1700";
            if (gen is 10 or 11) return "LGA1200";
        }
        return declared ?? "Unknown";
    }
}
