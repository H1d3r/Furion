namespace MyApp.Helpers;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        return value?.Length > maxLength ? value[..maxLength] + "..." : value;
    }
}

public class DescModel
{
    public string Description { get; set; }
}