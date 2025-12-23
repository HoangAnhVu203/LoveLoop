using System.Text;

public static class MoneyFormatter
{
    static readonly string[] UNITS = {
        "",     
        "k",    
        "M",    
        "B",    
        "T"     
    };

    public static string Format(long value)
    {
        if (value < 100_000)
            return value.ToString("N0"); // 99,999

        double v = value;
        int unitIndex = 0;

        while (v >= 1000 && unitIndex < UNITS.Length - 1)
        {
            v /= 1000;
            unitIndex++;
        }

        // Nếu vượt T → dùng aa, ab, ac...
        if (v >= 1000)
        {
            v /= 1000;
            unitIndex++;
        }

        string unit = GetUnit(unitIndex);
        return $"{v:0.0}".Replace('.', ',') + unit;
    }

    static string GetUnit(int index)
    {
        if (index < UNITS.Length)
            return UNITS[index];

        // aa, ab, ac...
        index -= UNITS.Length;

        int first = index / 26;
        int second = index % 26;

        char a = (char)('a' + first);
        char b = (char)('a' + second);

        return $"{a}{b}";
    }
}
