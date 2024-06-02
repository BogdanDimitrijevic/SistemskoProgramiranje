

public static class Extensions
{
    public static string? ProjectBase()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var baseDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;


        return baseDir;
    }
}