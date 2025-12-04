using GitHubStats.Domain.Interfaces;
using System.Web;

namespace GitHubStats.Rendering.Common;

/// <summary>
/// Base SVG card rendering class.
/// </summary>
public class Card
{
    public int Width { get; set; } = 495;
    public int Height { get; set; } = 195;
    public double BorderRadius { get; set; } = 4.5;
    public CardColors Colors { get; set; } = new();
    public string Title { get; set; } = "";
    public string? TitlePrefixIcon { get; set; }
    public bool HideBorder { get; set; }
    public bool HideTitle { get; set; }
    public bool DisableAnimations { get; set; }
    public string? CustomCss { get; set; }
    public string A11yTitle { get; set; } = "";
    public string A11yDesc { get; set; } = "";

    protected int PaddingX => 25;
    protected int PaddingY => 35;

    public string Render(string body)
    {
        using var svg = new SvgBuilder(8192);

        var effectiveHeight = HideTitle ? Height - 30 : Height;

        svg.Append($@"<svg width=""{Width}"" height=""{effectiveHeight}"" viewBox=""0 0 {Width} {effectiveHeight}"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" role=""img"" aria-labelledby=""descId"">");
        svg.Title(A11yTitle);
        svg.Desc(A11yDesc);

        // Style
        svg.Append("<style>");
        svg.Append(GetHeaderStyle());
        if (!string.IsNullOrEmpty(CustomCss))
            svg.Append(CustomCss);
        if (!DisableAnimations)
            svg.Append(GetAnimationStyles());
        if (DisableAnimations)
            svg.Append("* { animation-duration: 0s !important; animation-delay: 0s !important; }");
        svg.Append("</style>");

        // Gradient if needed
        svg.Append(RenderGradient());

        // Background
        var bgFill = IsGradientBackground() ? "url(#gradient)" : $"#{Colors.BgColor}";
        svg.Rect(0.5, 0.5, Width - 1, effectiveHeight - 1,
            fill: bgFill,
            stroke: $"#{Colors.BorderColor}",
            rx: BorderRadius,
            ry: BorderRadius,
            testId: "card-bg",
            strokeOpacity: HideBorder ? 0 : 1);

        // Title
        if (!HideTitle)
        {
            svg.Append(RenderTitle());
        }

        // Body
        var bodyTransform = HideTitle ? PaddingX : PaddingY + 20;
        svg.StartGroup(transform: $"translate(0, {bodyTransform})", testId: "main-card-body");
        svg.Append(body);
        svg.EndGroup();

        svg.EndSvg();
        return svg.ToString();
    }

    protected virtual string RenderTitle()
    {
        using var svg = new SvgBuilder(512);
        svg.StartGroup(transform: $"translate({PaddingX}, {PaddingY})", testId: "card-title");

        if (!string.IsNullOrEmpty(TitlePrefixIcon))
        {
            svg.Append($@"<svg class=""icon"" x=""0"" y=""-13"" viewBox=""0 0 16 16"" version=""1.1"" width=""16"" height=""16"">");
            svg.Append(TitlePrefixIcon);
            svg.Append("</svg>");
            svg.Append($@"<text x=""25"" y=""0"" class=""header"" data-testid=""header"">{HttpUtility.HtmlEncode(Title)}</text>");
        }
        else
        {
            svg.Append($@"<text x=""0"" y=""0"" class=""header"" data-testid=""header"">{HttpUtility.HtmlEncode(Title)}</text>");
        }

        svg.EndGroup();
        return svg.ToString();
    }

    protected string GetHeaderStyle()
    {
        return $@"
.header {{
    font: 600 18px 'Segoe UI', Ubuntu, Sans-Serif;
    fill: #{Colors.TitleColor};
    animation: fadeInAnimation 0.8s ease-in-out forwards;
}}
@supports(-moz-appearance: auto) {{
    .header {{ font-size: 15.5px; }}
}}
";
    }

    protected virtual string GetAnimationStyles()
    {
        return @"
@keyframes scaleInAnimation {
    from { transform: translate(-5px, 5px) scale(0); }
    to { transform: translate(-5px, 5px) scale(1); }
}
@keyframes fadeInAnimation {
    from { opacity: 0; }
    to { opacity: 1; }
}
";
    }

    protected string RenderGradient()
    {
        if (!IsGradientBackground())
            return "";

        var colors = Colors.BgColor.Split(',');
        if (colors.Length < 2)
            return "";

        var angle = 0;
        if (int.TryParse(colors[0], out var parsedAngle))
        {
            angle = parsedAngle;
            colors = colors.Skip(1).ToArray();
        }

        using var svg = new SvgBuilder(256);
        svg.Append($@"<defs><linearGradient id=""gradient"" gradientTransform=""rotate({angle})"" gradientUnits=""userSpaceOnUse"">");

        for (var i = 0; i < colors.Length; i++)
        {
            var offset = colors.Length == 1 ? 0 : (i * 100) / (colors.Length - 1);
            svg.Append($@"<stop offset=""{offset}%"" stop-color=""#{colors[i].Trim()}""/>");
        }

        svg.Append("</linearGradient></defs>");
        return svg.ToString();
    }

    private bool IsGradientBackground()
    {
        return Colors.BgColor.Contains(',');
    }
}
