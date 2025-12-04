using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Rendering.Themes;

/// <summary>
/// Manages card themes and color configurations.
/// </summary>
public static class ThemeManager
{
    private static readonly Dictionary<string, CardColors> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = new CardColors
        {
            TitleColor = "2f80ed",
            TextColor = "434d58",
            IconColor = "4c71f2",
            BgColor = "fffefe",
            BorderColor = "e4e2e2"
        },
        ["default_repocard"] = new CardColors
        {
            TitleColor = "2f80ed",
            TextColor = "434d58",
            IconColor = "586069",
            BgColor = "fffefe",
            BorderColor = "e4e2e2"
        },
        ["dark"] = new CardColors
        {
            TitleColor = "fff",
            TextColor = "9f9f9f",
            IconColor = "79ff97",
            BgColor = "151515",
            BorderColor = "e4e2e2"
        },
        ["radical"] = new CardColors
        {
            TitleColor = "fe428e",
            TextColor = "a9fef7",
            IconColor = "f8d847",
            BgColor = "141321",
            BorderColor = "e4e2e2"
        },
        ["merko"] = new CardColors
        {
            TitleColor = "abd200",
            TextColor = "68b587",
            IconColor = "b7d364",
            BgColor = "0a0f0b",
            BorderColor = "e4e2e2"
        },
        ["gruvbox"] = new CardColors
        {
            TitleColor = "fabd2f",
            TextColor = "8ec07c",
            IconColor = "fe8019",
            BgColor = "282828",
            BorderColor = "e4e2e2"
        },
        ["gruvbox_light"] = new CardColors
        {
            TitleColor = "b57614",
            TextColor = "427b58",
            IconColor = "af3a03",
            BgColor = "fbf1c7",
            BorderColor = "e4e2e2"
        },
        ["tokyonight"] = new CardColors
        {
            TitleColor = "70a5fd",
            TextColor = "38bdae",
            IconColor = "bf91f3",
            BgColor = "1a1b27",
            BorderColor = "e4e2e2"
        },
        ["onedark"] = new CardColors
        {
            TitleColor = "e4bf7a",
            TextColor = "df6d74",
            IconColor = "8eb573",
            BgColor = "282c34",
            BorderColor = "e4e2e2"
        },
        ["cobalt"] = new CardColors
        {
            TitleColor = "e683d9",
            TextColor = "75eeb2",
            IconColor = "0480ef",
            BgColor = "193549",
            BorderColor = "e4e2e2"
        },
        ["synthwave"] = new CardColors
        {
            TitleColor = "e2e9ec",
            TextColor = "e5289e",
            IconColor = "ef8539",
            BgColor = "2b213a",
            BorderColor = "e4e2e2"
        },
        ["highcontrast"] = new CardColors
        {
            TitleColor = "e7f216",
            TextColor = "fff",
            IconColor = "00ffff",
            BgColor = "000",
            BorderColor = "e4e2e2"
        },
        ["dracula"] = new CardColors
        {
            TitleColor = "ff6e96",
            TextColor = "f8f8f2",
            IconColor = "bd93f9",
            BgColor = "282a36",
            BorderColor = "e4e2e2"
        },
        ["prussian"] = new CardColors
        {
            TitleColor = "bddfff",
            TextColor = "6e93b5",
            IconColor = "38a0ff",
            BgColor = "172f45",
            BorderColor = "e4e2e2"
        },
        ["monokai"] = new CardColors
        {
            TitleColor = "eb1f6a",
            TextColor = "f1f1eb",
            IconColor = "e28905",
            BgColor = "272822",
            BorderColor = "e4e2e2"
        },
        ["vue"] = new CardColors
        {
            TitleColor = "41b883",
            TextColor = "273849",
            IconColor = "41b883",
            BgColor = "fffefe",
            BorderColor = "e4e2e2"
        },
        ["vue-dark"] = new CardColors
        {
            TitleColor = "41b883",
            TextColor = "fffefe",
            IconColor = "41b883",
            BgColor = "273849",
            BorderColor = "e4e2e2"
        },
        ["shades-of-purple"] = new CardColors
        {
            TitleColor = "fad000",
            TextColor = "a599e9",
            IconColor = "b362ff",
            BgColor = "2d2b55",
            BorderColor = "e4e2e2"
        },
        ["nightowl"] = new CardColors
        {
            TitleColor = "c792ea",
            TextColor = "7fdbca",
            IconColor = "ffeb95",
            BgColor = "011627",
            BorderColor = "e4e2e2"
        },
        ["buefy"] = new CardColors
        {
            TitleColor = "7957d5",
            TextColor = "363636",
            IconColor = "ff3860",
            BgColor = "ffffff",
            BorderColor = "e4e2e2"
        },
        ["blue-green"] = new CardColors
        {
            TitleColor = "2f97c1",
            TextColor = "0cf574",
            IconColor = "f5b700",
            BgColor = "040f0f",
            BorderColor = "e4e2e2"
        },
        ["algolia"] = new CardColors
        {
            TitleColor = "00aeff",
            TextColor = "ffffff",
            IconColor = "2dde98",
            BgColor = "050f2c",
            BorderColor = "e4e2e2"
        },
        ["great-gatsby"] = new CardColors
        {
            TitleColor = "ffa726",
            TextColor = "ffd95b",
            IconColor = "ffb74d",
            BgColor = "000000",
            BorderColor = "e4e2e2"
        },
        ["darcula"] = new CardColors
        {
            TitleColor = "ba5f17",
            TextColor = "bebebe",
            IconColor = "84628f",
            BgColor = "242424",
            BorderColor = "e4e2e2"
        },
        ["bear"] = new CardColors
        {
            TitleColor = "e03c8a",
            TextColor = "bcb28d",
            IconColor = "00aabb",
            BgColor = "1f2023",
            BorderColor = "e4e2e2"
        },
        ["solarized-dark"] = new CardColors
        {
            TitleColor = "268bd2",
            TextColor = "859900",
            IconColor = "b58900",
            BgColor = "002b36",
            BorderColor = "e4e2e2"
        },
        ["solarized-light"] = new CardColors
        {
            TitleColor = "268bd2",
            TextColor = "859900",
            IconColor = "b58900",
            BgColor = "fdf6e3",
            BorderColor = "e4e2e2"
        },
        ["chartreuse-dark"] = new CardColors
        {
            TitleColor = "7fff00",
            TextColor = "fff",
            IconColor = "00aeff",
            BgColor = "000",
            BorderColor = "e4e2e2"
        },
        ["nord"] = new CardColors
        {
            TitleColor = "81a1c1",
            TextColor = "d8dee9",
            IconColor = "88c0d0",
            BgColor = "2e3440",
            BorderColor = "e4e2e2"
        },
        ["gotham"] = new CardColors
        {
            TitleColor = "2aa889",
            TextColor = "99d1ce",
            IconColor = "599cab",
            BgColor = "0c1014",
            BorderColor = "e4e2e2"
        },
        ["material-palenight"] = new CardColors
        {
            TitleColor = "c792ea",
            TextColor = "a6accd",
            IconColor = "89ddff",
            BgColor = "292d3e",
            BorderColor = "e4e2e2"
        },
        ["graywhite"] = new CardColors
        {
            TitleColor = "24292e",
            TextColor = "24292e",
            IconColor = "24292e",
            BgColor = "ffffff",
            BorderColor = "e4e2e2"
        },
        ["vision-friendly-dark"] = new CardColors
        {
            TitleColor = "ffb000",
            TextColor = "ffffff",
            IconColor = "785ef0",
            BgColor = "000000",
            BorderColor = "e4e2e2"
        },
        ["ayu-mirage"] = new CardColors
        {
            TitleColor = "f4cd7c",
            TextColor = "c7c8c2",
            IconColor = "73d0ff",
            BgColor = "1f2430",
            BorderColor = "e4e2e2"
        },
        ["midnight-purple"] = new CardColors
        {
            TitleColor = "9745f5",
            TextColor = "ffffff",
            IconColor = "9f4bff",
            BgColor = "000000",
            BorderColor = "e4e2e2"
        },
        ["calm"] = new CardColors
        {
            TitleColor = "e07a5f",
            TextColor = "ebcfb2",
            IconColor = "edae49",
            BgColor = "373f51",
            BorderColor = "e4e2e2"
        },
        ["flag-india"] = new CardColors
        {
            TitleColor = "ff8f1c",
            TextColor = "509E2F",
            IconColor = "250E62",
            BgColor = "ffffff",
            BorderColor = "e4e2e2"
        },
        ["omni"] = new CardColors
        {
            TitleColor = "ff79c6",
            TextColor = "e1e1e6",
            IconColor = "e7de79",
            BgColor = "191622",
            BorderColor = "e4e2e2"
        },
        ["react"] = new CardColors
        {
            TitleColor = "61dafb",
            TextColor = "ffffff",
            IconColor = "61dafb",
            BgColor = "20232a",
            BorderColor = "e4e2e2"
        },
        ["jolly"] = new CardColors
        {
            TitleColor = "ff64da",
            TextColor = "a695c7",
            IconColor = "a695c7",
            BgColor = "291B3E",
            BorderColor = "e4e2e2"
        },
        ["maroongold"] = new CardColors
        {
            TitleColor = "f7ef8a",
            TextColor = "e0aa3e",
            IconColor = "f7ef8a",
            BgColor = "260000",
            BorderColor = "e4e2e2"
        },
        ["yeblu"] = new CardColors
        {
            TitleColor = "ffff00",
            TextColor = "ffffff",
            IconColor = "ffff00",
            BgColor = "002046",
            BorderColor = "e4e2e2"
        },
        ["blueberry"] = new CardColors
        {
            TitleColor = "82aaff",
            TextColor = "27e8a7",
            IconColor = "89ddff",
            BgColor = "242938",
            BorderColor = "e4e2e2"
        },
        ["slateorange"] = new CardColors
        {
            TitleColor = "faa627",
            TextColor = "ffffff",
            IconColor = "faa627",
            BgColor = "36393f",
            BorderColor = "e4e2e2"
        },
        ["kacho_ga"] = new CardColors
        {
            TitleColor = "bf4a3f",
            TextColor = "d9c8a9",
            IconColor = "a64833",
            BgColor = "402b23",
            BorderColor = "e4e2e2"
        },
        ["outrun"] = new CardColors
        {
            TitleColor = "ffcc00",
            TextColor = "8b8b8b",
            IconColor = "ff1aff",
            BgColor = "141439",
            BorderColor = "e4e2e2"
        },
        ["ocean_dark"] = new CardColors
        {
            TitleColor = "8957B2",
            TextColor = "92D534",
            IconColor = "FFFFFF",
            BgColor = "151A28",
            BorderColor = "e4e2e2"
        },
        ["city_lights"] = new CardColors
        {
            TitleColor = "5D8CB3",
            TextColor = "718CA1",
            IconColor = "E5C07B",
            BgColor = "1D252C",
            BorderColor = "e4e2e2"
        },
        ["github_dark"] = new CardColors
        {
            TitleColor = "58A6FF",
            TextColor = "C3D1D9",
            IconColor = "1F6FEB",
            BgColor = "0D1117",
            BorderColor = "e4e2e2"
        },
        ["github_dark_dimmed"] = new CardColors
        {
            TitleColor = "539BF5",
            TextColor = "ADBAC7",
            IconColor = "539BF5",
            BgColor = "22272E",
            BorderColor = "373E47"
        },
        ["discord_old_blurple"] = new CardColors
        {
            TitleColor = "7289DA",
            TextColor = "FFFFFF",
            IconColor = "7289DA",
            BgColor = "2C2F33",
            BorderColor = "e4e2e2"
        },
        ["aura_dark"] = new CardColors
        {
            TitleColor = "ff7372",
            TextColor = "EDECEE",
            IconColor = "6cffd0",
            BgColor = "252334",
            BorderColor = "e4e2e2"
        },
        ["panda"] = new CardColors
        {
            TitleColor = "19f9d899",
            TextColor = "FF75B5",
            IconColor = "19f9d899",
            BgColor = "31353a",
            BorderColor = "e4e2e2"
        },
        ["noctis_minimus"] = new CardColors
        {
            TitleColor = "d3b692",
            TextColor = "c5cdd3",
            IconColor = "72b7c0",
            BgColor = "1b2932",
            BorderColor = "e4e2e2"
        },
        ["cobalt2"] = new CardColors
        {
            TitleColor = "ffc600",
            TextColor = "ffffff",
            IconColor = "0088ff",
            BgColor = "193549",
            BorderColor = "e4e2e2"
        },
        ["swift"] = new CardColors
        {
            TitleColor = "000000",
            TextColor = "000000",
            IconColor = "F05138",
            BgColor = "f7f7f7",
            BorderColor = "e4e2e2"
        },
        ["aura"] = new CardColors
        {
            TitleColor = "a277ff",
            TextColor = "61ffca",
            IconColor = "ffca85",
            BgColor = "15141b",
            BorderColor = "e4e2e2"
        },
        ["apprentice"] = new CardColors
        {
            TitleColor = "ffffff",
            TextColor = "bcbcbc",
            IconColor = "ffffaf",
            BgColor = "262626",
            BorderColor = "e4e2e2"
        },
        ["moltack"] = new CardColors
        {
            TitleColor = "86092c",
            TextColor = "574038",
            IconColor = "86092c",
            BgColor = "f5e1c0",
            BorderColor = "e4e2e2"
        },
        ["codeSTACKr"] = new CardColors
        {
            TitleColor = "ff652f",
            TextColor = "ffffff",
            IconColor = "ffe400",
            BgColor = "09131b",
            BorderColor = "e4e2e2"
        },
        ["rose_pine"] = new CardColors
        {
            TitleColor = "9ccfd8",
            TextColor = "e0def4",
            IconColor = "ebbcba",
            BgColor = "191724",
            BorderColor = "e4e2e2"
        },
        ["catppuccin_latte"] = new CardColors
        {
            TitleColor = "1e66f5",
            TextColor = "4c4f69",
            IconColor = "fe640b",
            BgColor = "eff1f5",
            BorderColor = "e4e2e2"
        },
        ["catppuccin_mocha"] = new CardColors
        {
            TitleColor = "89b4fa",
            TextColor = "cdd6f4",
            IconColor = "fab387",
            BgColor = "1e1e2e",
            BorderColor = "e4e2e2"
        },
        ["date_night"] = new CardColors
        {
            TitleColor = "DA7885",
            TextColor = "E1B2B2",
            IconColor = "BB8470",
            BgColor = "170F0C",
            BorderColor = "e4e2e2"
        },
        ["one_dark_pro"] = new CardColors
        {
            TitleColor = "61AFEF",
            TextColor = "E5C06E",
            IconColor = "C778DD",
            BgColor = "23272E",
            BorderColor = "e4e2e2"
        },
        ["rose"] = new CardColors
        {
            TitleColor = "8d192b",
            TextColor = "d9a8ad",
            IconColor = "c5797d",
            BgColor = "111",
            BorderColor = "e4e2e2"
        },
        ["holi"] = new CardColors
        {
            TitleColor = "5FABEE",
            TextColor = "D6E7FF",
            IconColor = "65DBA4",
            BgColor = "030A16",
            BorderColor = "e4e2e2"
        },
        ["neon"] = new CardColors
        {
            TitleColor = "00EAD3",
            TextColor = "FFFFFF",
            IconColor = "FF449F",
            BgColor = "000000",
            BorderColor = "e4e2e2"
        },
        ["blue_navy"] = new CardColors
        {
            TitleColor = "00b4d8",
            TextColor = "90e0ef",
            IconColor = "0077b6",
            BgColor = "03045e",
            BorderColor = "e4e2e2"
        },
        ["calm_pink"] = new CardColors
        {
            TitleColor = "FFB4B4",
            TextColor = "FFD6D6",
            IconColor = "DE8282",
            BgColor = "534747",
            BorderColor = "e4e2e2"
        },
        ["ambient_gradient"] = new CardColors
        {
            TitleColor = "FFD6E0",
            TextColor = "C4FCEF",
            IconColor = "94FBAB",
            BgColor = "4158D0,C850C0,FFCC70",
            BorderColor = "e4e2e2"
        }
    };

    /// <summary>
    /// Gets colors for a theme with optional overrides.
    /// </summary>
    public static CardColors GetColors(
        string? theme = null,
        string? titleColor = null,
        string? textColor = null,
        string? iconColor = null,
        string? bgColor = null,
        string? borderColor = null,
        string? ringColor = null)
    {
        var baseTheme = Themes.GetValueOrDefault(theme ?? "default", Themes["default"]);

        return new CardColors
        {
            TitleColor = NormalizeColor(titleColor) ?? baseTheme.TitleColor,
            TextColor = NormalizeColor(textColor) ?? baseTheme.TextColor,
            IconColor = NormalizeColor(iconColor) ?? baseTheme.IconColor,
            BgColor = NormalizeColor(bgColor) ?? baseTheme.BgColor,
            BorderColor = NormalizeColor(borderColor) ?? baseTheme.BorderColor,
            RingColor = NormalizeColor(ringColor) ?? baseTheme.RingColor
        };
    }

    /// <summary>
    /// Checks if a theme exists.
    /// </summary>
    public static bool ThemeExists(string theme) => Themes.ContainsKey(theme);

    /// <summary>
    /// Gets all available theme names.
    /// </summary>
    public static IEnumerable<string> GetThemeNames() => Themes.Keys;

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        // Remove # prefix if present
        color = color.TrimStart('#');

        // Validate hex color format
        if (IsValidHexColor(color))
            return color;

        return null;
    }

    private static bool IsValidHexColor(string color)
    {
        return color.Length is 3 or 6 or 8 &&
               color.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}
