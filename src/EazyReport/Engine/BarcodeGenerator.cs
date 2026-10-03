using System.Text;
using QRCoder;

namespace EazyReport.Engine;

public static class BarcodeGenerator
{
    private static readonly Dictionary<char, string> Code128Patterns = new()
    {
        [' '] = "212222", ['!'] = "222122", ['"'] = "222221", ['#'] = "121223", ['$'] = "121322",
        ['%'] = "131222", ['&'] = "122213", ['\''] = "122312", ['('] = "132212", [')'] = "221213",
        ['*'] = "221312", ['+'] = "231212", [','] = "112232", ['-'] = "122132", ['.'] = "122231",
        ['/'] = "113222", ['0'] = "123122", ['1'] = "123221", ['2'] = "223211", ['3'] = "221132",
        ['4'] = "221231", ['5'] = "213212", ['6'] = "223112", ['7'] = "312131", ['8'] = "311222",
        ['9'] = "321122", [':'] = "321221", [';'] = "312212", ['<'] = "322112", ['='] = "322211",
        ['>'] = "212123", ['?'] = "212321", ['@'] = "232121", ['A'] = "111323", ['B'] = "131123",
        ['C'] = "131321", ['D'] = "112313", ['E'] = "132113", ['F'] = "132311", ['G'] = "211313",
        ['H'] = "231113", ['I'] = "231311", ['J'] = "112133", ['K'] = "112331", ['L'] = "132131",
        ['M'] = "113123", ['N'] = "113321", ['O'] = "133121", ['P'] = "313121", ['Q'] = "211331",
        ['R'] = "231131", ['S'] = "213113", ['T'] = "213311", ['U'] = "213131", ['V'] = "311123",
        ['W'] = "311321", ['X'] = "331121", ['Y'] = "312113", ['Z'] = "312311", ['['] = "332111",
        ['\\'] = "314111", [']'] = "221411", ['^'] = "431111", ['_'] = "111224", ['`'] = "111422",
        ['a'] = "121124", ['b'] = "121421", ['c'] = "141122", ['d'] = "141221", ['e'] = "112214",
        ['f'] = "112412", ['g'] = "122114", ['h'] = "122411", ['i'] = "142112", ['j'] = "142211",
        ['k'] = "241211", ['l'] = "221114", ['m'] = "413111", ['n'] = "241112", ['o'] = "134111",
        ['p'] = "111242", ['q'] = "121142", ['r'] = "121241", ['s'] = "114212", ['t'] = "124112",
        ['u'] = "124211", ['v'] = "411212", ['w'] = "421112", ['x'] = "421211", ['y'] = "212141",
        ['z'] = "214121", ['{'] = "412121", ['|'] = "111143", ['}'] = "111341", ['~'] = "131141",
    };

    private const string StartB = "211214";
    private const string StopPattern = "2331112";

    public static bool IsTwoDimensional(string? symbology)
    {
        string s = (symbology ?? "").Trim().ToLowerInvariant();
        return s is "qrcode" or "microqrcode" or "swissqrcode" or "datamatrix" or "pdf417" or "aztec";
    }

    public static string EncodeCode128B(string text)
    {
        var sb = new StringBuilder();
        sb.Append(StartB);
        int checkSum = 104; // Start Code B
        int pos = 1;

        foreach (char ch in text)
        {
            if (Code128Patterns.TryGetValue(ch, out var pat))
            {
                int charVal = (int)ch - 32;
                checkSum += charVal * pos;
                sb.Append(pat);
                pos++;
            }
        }

        int checkVal = checkSum % 103;
        char checkCh = (checkVal + 32 >= 32 && checkVal + 32 <= 126) ? (char)(checkVal + 32) : ' ';
        sb.Append(Code128Patterns.GetValueOrDefault(checkCh, "212222"));
        sb.Append(StopPattern);

        return sb.ToString();
    }

    public static bool[,]? GetQrMatrix(string text)
    {
        try
        {
            using var qrGen = new QRCodeGenerator();
            using var qrData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            var moduleMatrix = qrData.ModuleMatrix;
            int rows = moduleMatrix.Count;
            int cols = rows > 0 ? moduleMatrix[0].Count : 0;
            if (rows == 0 || cols == 0) return null;

            var res = new bool[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    res[r, c] = moduleMatrix[r][c];
                }
            }
            return res;
        }
        catch
        {
            return null;
        }
    }

    public static string GenerateSvg(string symbology, string val, string barColor = "#000000", string bgColor = "#ffffff", bool showText = false)
    {
        if (string.IsNullOrEmpty(val)) val = "12345678";
        if (string.IsNullOrEmpty(barColor)) barColor = "#000000";

        if (IsTwoDimensional(symbology))
        {
            var matrix = GetQrMatrix(val);
            if (matrix == null)
            {
                // Fallback 21x21 mock
                matrix = new bool[21, 21];
                for (int r = 0; r < 21; r++)
                    for (int c = 0; c < 21; c++)
                        matrix[r, c] = (r + c) % 2 == 0;
            }

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            var sb = new StringBuilder();
            sb.Append($"<svg viewBox=\"0 0 {cols} {rows}\" preserveAspectRatio=\"xMidYMid meet\" style=\"display:block;width:100%;height:100%\" xmlns=\"http://www.w3.org/2000/svg\">");

            if (!string.IsNullOrEmpty(bgColor) && !string.Equals(bgColor, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($"<rect width=\"{cols}\" height=\"{rows}\" fill=\"{bgColor}\"/>");
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (matrix[r, c])
                    {
                        sb.Append($"<rect x=\"{c}\" y=\"{r}\" width=\"1\" height=\"1\" fill=\"{barColor}\"/>");
                    }
                }
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
        else
        {
            // 1D Barcode (Code 128)
            string pattern = EncodeCode128B(val);
            int totalModules = 0;
            foreach (char ch in pattern) totalModules += (ch - '0');
            if (totalModules <= 0) totalModules = 1;

            double svgHeight = showText ? 60.0 : 50.0;
            double barH = showText ? 42.0 : 50.0;

            var sb = new StringBuilder();
            sb.Append($"<svg viewBox=\"0 0 {totalModules} {svgHeight}\" preserveAspectRatio=\"none\" style=\"display:block;width:100%;height:100%\" xmlns=\"http://www.w3.org/2000/svg\">");

            if (!string.IsNullOrEmpty(bgColor) && !string.Equals(bgColor, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($"<rect width=\"{totalModules}\" height=\"{svgHeight}\" fill=\"{bgColor}\"/>");
            }

            double currX = 0.0;
            bool isBar = true;

            foreach (char digit in pattern)
            {
                int modWidth = digit - '0';
                if (isBar)
                {
                    sb.Append($"<rect x=\"{currX}\" y=\"0\" width=\"{modWidth}\" height=\"{barH}\" fill=\"{barColor}\"/>");
                }
                currX += modWidth;
                isBar = !isBar;
            }

            if (showText)
            {
                sb.Append($"<text x=\"{totalModules / 2.0}\" y=\"{svgHeight - 2}\" text-anchor=\"middle\" font-family=\"Courier, monospace\" font-size=\"9\" fill=\"{barColor}\">{System.Net.WebUtility.HtmlEncode(val)}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
