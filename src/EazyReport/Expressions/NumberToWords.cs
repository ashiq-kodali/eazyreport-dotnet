namespace EazyReport.Expressions;

public static class NumberToWords
{
    private static readonly string[] Units = {
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens = {
        "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string Convert(double number, string currency = "")
    {
        if (double.IsNaN(number) || double.IsInfinity(number)) return "Zero";

        long whole = (long)Math.Floor(Math.Abs(number));
        int cents = (int)Math.Round((Math.Abs(number) - whole) * 100);

        string words = whole == 0 ? "Zero" : NumberToWordsHelper(whole);

        if (number < 0) words = "Minus " + words;

        if (string.IsNullOrWhiteSpace(currency))
        {
            if (cents > 0)
            {
                words += $" and {cents:D2}/100";
            }
            return words;
        }

        string curUpper = currency.ToUpperInvariant();
        string curName = curUpper switch
        {
            "USD" => "Dollars",
            "EUR" => "Euros",
            "GBP" => "Pounds",
            "INR" => "Rupees",
            "CAD" => "Dollars",
            "AUD" => "Dollars",
            _ => curUpper
        };

        string centName = curUpper switch
        {
            "INR" => "Paise",
            "GBP" => "Pence",
            "EUR" => "Cents",
            _ => "Cents"
        };

        if (cents > 0)
        {
            words += $" {curName} and {NumberToWordsHelper(cents)} {centName}";
        }
        else
        {
            words += $" {curName} Only";
        }

        return words;
    }

    private static string NumberToWordsHelper(long number)
    {
        if (number == 0) return "";

        if (number < 20) return Units[number];

        if (number < 100)
        {
            int rem = (int)(number % 10);
            return Tens[number / 10] + (rem > 0 ? " " + Units[rem] : "");
        }

        if (number < 1000)
        {
            long rem = number % 100;
            return Units[number / 100] + " Hundred" + (rem > 0 ? " " + NumberToWordsHelper(rem) : "");
        }

        if (number < 1000000)
        {
            long rem = number % 1000;
            return NumberToWordsHelper(number / 1000) + " Thousand" + (rem > 0 ? " " + NumberToWordsHelper(rem) : "");
        }

        if (number < 1000000000)
        {
            long rem = number % 1000000;
            return NumberToWordsHelper(number / 1000000) + " Million" + (rem > 0 ? " " + NumberToWordsHelper(rem) : "");
        }

        long billRem = number % 1000000000;
        return NumberToWordsHelper(number / 1000000000) + " Billion" + (billRem > 0 ? " " + NumberToWordsHelper(billRem) : "");
    }
}
