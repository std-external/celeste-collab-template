using System.Text;

namespace CollabTools;

/// <summary>
/// 合集模板的命令行工具：查素材命名（check）和并对话（merge）。
/// 用法见 tools/README.md。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return args.Length == 0 ? 2 : 0;
        }

        return args[0] switch
        {
            "check" => AssetChecker.Run(args[1..]),
            "merge" => DialogMerger.Run(args[1..]),
            _ => Unknown(args[0]),
        };
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"未知子命令: {command}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            collab-tools — 合集模板自带的两个小工具

              dotnet run --project tools/CollabTools -- check [目录] [--collab 合集id]
                  查素材路径和命名。默认从 CollabUtils2CollabID.txt 读合集 id。
                  有错误时退出码 1，警告不影响退出码。

              dotnet run --project tools/CollabTools -- merge --into Dialog/English.txt 片段1 片段2 ...
                  把各作者的对话片段并进合集对话文件。键重复且内容不同会报错停下。

              dotnet run --project tools/CollabTools -- merge --into ... --dry-run
                  只报会加什么，不写文件。
            """);
    }
}
