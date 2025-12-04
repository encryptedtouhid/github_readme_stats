using System.Buffers;
using System.Text;
using System.Web;

namespace GitHubStats.Rendering.Common;

/// <summary>
/// High-performance SVG builder using pooled StringBuilder.
/// Optimized for high-throughput scenarios.
/// </summary>
public sealed class SvgBuilder : IDisposable
{
    private static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;
    private readonly StringBuilder _sb;
    private bool _disposed;

    public SvgBuilder(int initialCapacity = 4096)
    {
        _sb = new StringBuilder(initialCapacity);
    }

    public SvgBuilder Append(string value)
    {
        _sb.Append(value);
        return this;
    }

    public SvgBuilder Append(char value)
    {
        _sb.Append(value);
        return this;
    }

    public SvgBuilder Append(int value)
    {
        _sb.Append(value);
        return this;
    }

    public SvgBuilder Append(double value)
    {
        _sb.Append(value.ToString("0.##"));
        return this;
    }

    public SvgBuilder AppendLine(string? value = null)
    {
        if (value != null)
            _sb.Append(value);
        _sb.AppendLine();
        return this;
    }

    public SvgBuilder AppendEncoded(string? value)
    {
        if (value != null)
            _sb.Append(HttpUtility.HtmlEncode(value));
        return this;
    }

    /// <summary>
    /// Starts an SVG element with common attributes.
    /// </summary>
    public SvgBuilder StartSvg(int width, int height, string? viewBox = null)
    {
        _sb.Append($@"<svg width=""{width}"" height=""{height}"" viewBox=""");
        _sb.Append(viewBox ?? $"0 0 {width} {height}");
        _sb.Append(@""" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" role=""img"" aria-labelledby=""descId"">");
        return this;
    }

    public SvgBuilder EndSvg()
    {
        _sb.Append("</svg>");
        return this;
    }

    public SvgBuilder StartGroup(string? transform = null, string? className = null, string? testId = null)
    {
        _sb.Append("<g");
        if (transform != null)
            _sb.Append($@" transform=""{transform}""");
        if (className != null)
            _sb.Append($@" class=""{className}""");
        if (testId != null)
            _sb.Append($@" data-testid=""{testId}""");
        _sb.Append('>');
        return this;
    }

    public SvgBuilder EndGroup()
    {
        _sb.Append("</g>");
        return this;
    }

    public SvgBuilder Rect(double x, double y, double width, double height,
        string? fill = null, string? stroke = null, double? rx = null, double? ry = null,
        string? testId = null, double? strokeOpacity = null)
    {
        _sb.Append($@"<rect x=""{x}"" y=""{y}"" width=""{width}"" height=""{height}""");
        if (rx.HasValue)
            _sb.Append($@" rx=""{rx.Value}""");
        if (ry.HasValue)
            _sb.Append($@" ry=""{ry.Value}""");
        if (fill != null)
            _sb.Append($@" fill=""{fill}""");
        if (stroke != null)
            _sb.Append($@" stroke=""{stroke}""");
        if (strokeOpacity.HasValue)
            _sb.Append($@" stroke-opacity=""{strokeOpacity.Value}""");
        if (testId != null)
            _sb.Append($@" data-testid=""{testId}""");
        _sb.Append("/>");
        return this;
    }

    public SvgBuilder Circle(double cx, double cy, double r, string? fill = null,
        string? stroke = null, double? strokeWidth = null)
    {
        _sb.Append($@"<circle cx=""{cx}"" cy=""{cy}"" r=""{r}""");
        if (fill != null)
            _sb.Append($@" fill=""{fill}""");
        if (stroke != null)
            _sb.Append($@" stroke=""{stroke}""");
        if (strokeWidth.HasValue)
            _sb.Append($@" stroke-width=""{strokeWidth.Value}""");
        _sb.Append("/>");
        return this;
    }

    public SvgBuilder Text(string content, double x, double y,
        string? className = null, string? fill = null, string? testId = null)
    {
        _sb.Append($@"<text x=""{x}"" y=""{y}""");
        if (className != null)
            _sb.Append($@" class=""{className}""");
        if (fill != null)
            _sb.Append($@" fill=""{fill}""");
        if (testId != null)
            _sb.Append($@" data-testid=""{testId}""");
        _sb.Append('>');
        _sb.Append(HttpUtility.HtmlEncode(content));
        _sb.Append("</text>");
        return this;
    }

    public SvgBuilder Style(string css)
    {
        _sb.Append("<style>").Append(css).Append("</style>");
        return this;
    }

    public SvgBuilder Title(string title, string id = "titleId")
    {
        _sb.Append($@"<title id=""{id}"">");
        _sb.Append(HttpUtility.HtmlEncode(title));
        _sb.Append("</title>");
        return this;
    }

    public SvgBuilder Desc(string desc, string id = "descId")
    {
        _sb.Append($@"<desc id=""{id}"">");
        _sb.Append(HttpUtility.HtmlEncode(desc));
        _sb.Append("</desc>");
        return this;
    }

    public SvgBuilder Raw(string content)
    {
        _sb.Append(content);
        return this;
    }

    public override string ToString() => _sb.ToString();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sb.Clear();
    }
}
