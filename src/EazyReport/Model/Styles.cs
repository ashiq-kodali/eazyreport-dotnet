namespace EazyReport.Model;

public class TextStyle
{
    public string FontFamily { get; set; } = "Inter";
    public double FontSize { get; set; } = 10.0;
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    public bool Underline { get; set; } = false;
    public string Color { get; set; } = "#000000";
    public string Align { get; set; } = "left"; // left, center, right, justify
    public string VerticalAlign { get; set; } = "top"; // top, middle, bottom
    public double LineHeight { get; set; } = 1.2;
}

public class BoxStyle
{
    public string Background { get; set; } = "";
    public double BorderWidth { get; set; } = 0.0;
    public string BorderColor { get; set; } = "#000000";
    public string BorderStyle { get; set; } = "solid"; // solid, dashed, dotted
    public double BorderRadius { get; set; } = 0.0;
    public double Padding { get; set; } = 0.0;
    public double? PaddingTop { get; set; }
    public double? PaddingRight { get; set; }
    public double? PaddingBottom { get; set; }
    public double? PaddingLeft { get; set; }

    public double GetPaddingTop() => PaddingTop ?? Padding;
    public double GetPaddingRight() => PaddingRight ?? Padding;
    public double GetPaddingBottom() => PaddingBottom ?? Padding;
    public double GetPaddingLeft() => PaddingLeft ?? Padding;
}

public class ValueFormat
{
    public string Type { get; set; } = "text"; // text, currency, number, date, percent
    public string Pattern { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public int Decimals { get; set; } = 2;
    public bool Thousands { get; set; } = true;
}

public class PageMargins
{
    public double Top { get; set; } = 10.0;
    public double Right { get; set; } = 10.0;
    public double Bottom { get; set; } = 10.0;
    public double Left { get; set; } = 10.0;
}

public class Watermark
{
    public bool Enabled { get; set; } = false;
    public string Text { get; set; } = "DRAFT";
    public string Color { get; set; } = "#94a3b8";
    public double FontSize { get; set; } = 90.0;
    public double Angle { get; set; } = -45.0;
    public double Opacity { get; set; } = 0.18;
    public string Image { get; set; } = "";
    public bool OnTop { get; set; } = false;
}
