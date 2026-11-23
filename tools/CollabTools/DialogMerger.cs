using System.Text;
using System.Text.RegularExpressions;

namespace CollabTools;

/// <summary>
/// 把各作者的对话片段并进合集对话文件。
/// 规则：以 id= 开头的行算一条新条目，后面的行归它；目标里已有的键内容一致就跳过，不一致就报错停下。
/// </summary>
public static partial class DialogMerger
{
    [GeneratedRegex(@"^([A-Za-z0-9_]+)=")]
    private static partial Regex KeyLine();

    private readonly record struct Block(string? Key, List<string> Lines)
    {
        public string Value => string.Join("\n", Lines.Select(line => line.Trim())).Trim();
    }

    public static int Run(string[] args)
    {
        string? into = null;
        bool dryRun = false;
        List<string> sources = new();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--into":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("--into 后面要跟目标文件");
                        return 2;
                    }
                    into = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine("用法: merge --into Dialog/English.txt 片段1 片段2 ... [--dry-run]");
                    return 0;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        Console.Error.WriteLine($"不认识的参数: {args[i]}");
                        return 2;
                    }
                    sources.Add(args[i]);
                    break;
            }
        }

        if (into is null || sources.Count == 0)
        {
            Console.Error.WriteLine("用法: merge --into Dialog/English.txt 片段1 片段2 ... [--dry-run]");
            return 2;
        }

        if (!File.Exists(into))
        {
            Console.Error.WriteLine($"目标不存在: {into}");
            return 2;
        }

        List<Block> blocks = Parse(into);
        Dictionary<string, string> existing = new(StringComparer.Ordinal);
        foreach (Block block in blocks)
        {
            if (block.Key is not null) existing[block.Key] = block.Value;
        }

        int added = 0;
        foreach (string source in sources)
        {
            if (!File.Exists(source))
            {
                Console.Error.WriteLine($"跳过不存在的片段: {source}");
                continue;
            }

            foreach (Block block in Parse(source))
            {
                if (block.Key is null) continue;

                if (existing.TryGetValue(block.Key, out string? value))
                {
                    if (value != block.Value)
                    {
                        Console.Error.WriteLine($"冲突: {block.Key} 在 {source} 和目标里内容不同，先人工确认");
                        return 1;
                    }
                    Console.WriteLine($"已存在，跳过: {block.Key}");
                    continue;
                }

                blocks.Add(block);
                existing[block.Key] = block.Value;
                added++;
                Console.WriteLine($"合入: {block.Key}  <- {source}");
            }
        }

        if (dryRun)
        {
            Console.WriteLine($"dry-run：会新增 {added} 条");
            return 0;
        }

        List<string> output = new();
        foreach (Block block in blocks) output.AddRange(block.Lines);
        File.WriteAllText(into, string.Join("\n", output) + "\n", new UTF8Encoding(false));
        Console.WriteLine($"写入 {into}，新增 {added} 条");
        return 0;
    }

    private static List<Block> Parse(string path)
    {
        List<Block> blocks = new();
        string? key = null;
        List<string> buffer = new();

        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            Match match = KeyLine().Match(line);
            if (match.Success)
            {
                if (key is not null || buffer.Count > 0) blocks.Add(new Block(key, buffer));
                key = match.Groups[1].Value;
                buffer = new List<string> { line };
            }
            else
            {
                buffer.Add(line);
            }
        }

        if (key is not null || buffer.Count > 0) blocks.Add(new Block(key, buffer));
        return blocks;
    }
}
