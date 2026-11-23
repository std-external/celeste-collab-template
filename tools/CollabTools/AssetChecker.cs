using System.Text;
using System.Text.RegularExpressions;

namespace CollabTools;

/// <summary>素材路径与命名检查，规则见 docs/02-素材命名规范.md。</summary>
public static partial class AssetChecker
{
    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal)
    {
        "decal", "anim", "bg", "fg", "tile", "obj", "npc", "icon", "skull",
        "heart", "sticker", "map", "ui", "end", "bgm", "sfx", "amb", "lua",
        "readme", "license",
    };

    private static readonly HashSet<string> AudioRoles = new(StringComparer.Ordinal) { "bgm", "sfx", "amb" };

    private static readonly HashSet<string> GameplayCats = new(StringComparer.Ordinal)
    {
        "decals", "bgs", "tilesets", "objects", "characters", "danger",
        "collectables", "animatedTiles", "scenery",
    };

    private static readonly HashSet<string> SkipTop = new(StringComparer.Ordinal)
    {
        ".git", "contrib", "docs", "tools", "bin", "Code", "Ahorn", "Loenn",
        "Tutorials", "Dialog", "Source", "Effects", "lib-stripped", "obj",
    };

    private static readonly string[] OtherAtlasRoots =
    {
        "Gui", "Stickers", "Journal", "Checkpoints", "Mountain", "Endscreens",
        "Portraits", "ColorGrading",
    };

    [GeneratedRegex(@"^[A-Za-z0-9_.\-]+$")]
    private static partial Regex AsciiName();

    private static readonly char[] WindowsBad = { '<', '>', ':', '"', '|', '?', '*' };

    public static int Run(string[] args)
    {
        string root = ".";
        string? collab = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--collab":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("--collab 后面要跟合集 id");
                        return 2;
                    }
                    collab = args[++i];
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine("用法: check [目录] [--collab 合集id]");
                    return 0;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        Console.Error.WriteLine($"不认识的参数: {args[i]}");
                        return 2;
                    }
                    root = args[i];
                    break;
            }
        }

        string rootFull = Path.GetFullPath(root);
        if (!Directory.Exists(rootFull))
        {
            Console.Error.WriteLine($"目录不存在: {rootFull}");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(collab))
        {
            string idFile = Path.Combine(rootFull, "CollabUtils2CollabID.txt");
            if (!File.Exists(idFile))
            {
                Console.Error.WriteLine("找不到 CollabUtils2CollabID.txt，用 --collab 指定合集 id");
                return 2;
            }
            collab = File.ReadAllText(idFile, Encoding.UTF8).Trim();
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (string path in Directory.EnumerateFiles(rootFull, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(rootFull, path).Replace('\\', '/');
            string[] parts = rel.Split('/');

            if (parts.Any(SkipTop.Contains)) continue;
            if (parts.Length < 2) continue;
            if (parts[0] is not ("Maps" or "Graphics" or "Audio" or "Assets")) continue;
            if (Path.GetFileName(path) is ".gitkeep" or ".DS_Store" or "Thumbs.db") continue;

            CheckName(rel, errors);

            switch (parts[0])
            {
                case "Maps" when path.EndsWith(".bin", StringComparison.OrdinalIgnoreCase):
                    CheckMapBin(rel, path, collab, errors, warnings);
                    break;
                case "Graphics" when parts.Length >= 4 && parts[1] == "Atlases" && parts[2] == "Gameplay":
                    CheckGameplayAsset(rel, path, collab, errors, warnings);
                    break;
                case "Graphics" when parts.Length >= 3 && parts[1] == "Atlases" && OtherAtlasRoots.Contains(parts[2]):
                    CheckOtherAtlasAsset(rel, collab, errors);
                    break;
                case "Audio":
                    CheckAudio(rel, path, collab, errors, warnings);
                    break;
                case "Assets" when path.EndsWith(".lua", StringComparison.OrdinalIgnoreCase):
                    CheckLua(rel, collab, warnings);
                    break;
            }
        }

        CheckCaseCollisions(rootFull, errors);

        foreach (string message in errors) Console.WriteLine($"错误: {message}");
        foreach (string message in warnings) Console.WriteLine($"警告: {message}");
        Console.WriteLine($"\n{errors.Count} 个错误 / {warnings.Count} 个警告");

        return errors.Count == 0 ? 0 : 1;
    }

    private static void CheckName(string rel, List<string> errors)
    {
        foreach (string part in rel.Split('/'))
        {
            if (!AsciiName().IsMatch(part))
            {
                errors.Add($"{rel}: 路径里有非 ASCII / 非法字符: \"{part}\"");
                return;
            }
            if (part.StartsWith(' ') || part.EndsWith(' '))
            {
                errors.Add($"{rel}: 目录或文件名首尾有空格: \"{part}\"");
                return;
            }
            if (part.IndexOfAny(WindowsBad) >= 0)
            {
                errors.Add($"{rel}: 名字里有 Windows 不认的字符: \"{part}\"");
                return;
            }
        }
    }

    private static void CheckGameplayAsset(string rel, string full, string collab, List<string> errors, List<string> warnings)
    {
        string[] parts = rel.Split('/');
        string category = parts[3];
        string[] tail = parts[4..];

        if (!GameplayCats.Contains(category))
        {
            warnings.Add($"{rel}: 图集分类 \"{category}\" 不在常用列表里（decals/bgs/tilesets/objects/characters/danger/collectables/animatedTiles/scenery）");
        }

        if (tail.Length == 0) return;

        if (tail[0] != collab)
        {
            errors.Add($"{rel}: 素材不在合集命名空间 {collab}/ 下，会跟别的 mod 撞路径");
            return;
        }

        if (tail.Length >= 3 && tail[1] == "_Shared")
        {
            CheckSharedAsset(rel, full, tail[^1], collab, errors, warnings);
            return;
        }

        if (tail.Length < 4)
        {
            warnings.Add($"{rel}: 少了作者/地图两层（应为 {collab}/<作者>/<图>/...）");
            return;
        }

        string author = tail[1];
        string map = tail[2];
        string fileName = tail[^1];
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string[] segments = stem.Split('_');

        if (segments.Length < 4)
        {
            errors.Add($"{rel}: 文件名不是 <作者>_<图>_<作用>_<名字> 四段式");
            return;
        }

        if (segments[0] != author)
        {
            warnings.Add($"{rel}: 文件名里的作者 {segments[0]} 和目录 {author} 不一致");
        }
        if (segments[1] != map)
        {
            warnings.Add($"{rel}: 文件名里的图名 {segments[1]} 和目录 {map} 不一致");
        }

        CheckRoleAndTexture(rel, full, segments[2], stem, errors, warnings);
    }

    private static void CheckSharedAsset(string rel, string full, string fileName, string collab, List<string> errors, List<string> warnings)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string[] segments = stem.Split('_');
        if (segments.Length < 4 || segments[0] != collab || segments[1] != "shared")
        {
            errors.Add($"{rel}: 共用素材应为 {collab}_shared_<作用>_<名字>");
            return;
        }
        CheckRoleAndTexture(rel, full, segments[2], stem, errors, warnings);
    }

    private static void CheckRoleAndTexture(string rel, string full, string role, string stem, List<string> errors, List<string> warnings)
    {
        if (!Roles.Contains(role))
        {
            errors.Add($"{rel}: 作用词 \"{role}\" 不在词表里（见 docs/02-素材命名规范.md）");
        }

        if (full.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && TryReadPngSize(full, out int width, out int height))
        {
            if ((width % 2 != 0 || height % 2 != 0) && role is "decal" or "anim" or "bg" or "fg" or "tile" or "obj")
            {
                warnings.Add($"{rel}: 贴图尺寸 {width}x{height} 有奇数边，decal 容易出渲染缝隙");
            }
        }

        if (stem.Length > 0 && char.IsAsciiDigit(stem[^1]) && role is not ("anim" or "end"))
        {
            warnings.Add($"{rel}: 文件名以数字结尾，游戏会当成动画帧；单张图请改名");
        }
    }

    private static void CheckOtherAtlasAsset(string rel, string collab, List<string> errors)
    {
        string[] parts = rel.Split('/');
        if (!parts.Contains(collab))
        {
            errors.Add($"{rel}: 素材不在合集命名空间 {collab}/ 下");
        }
    }

    private static void CheckMapBin(string rel, string full, string collab, List<string> errors, List<string> warnings)
    {
        string[] parts = rel.Split('/');
        if (parts.Length < 4)
        {
            errors.Add($"{rel}: bin 层级不对，应为 Maps/{collab}/<大厅>/<图>.bin");
            return;
        }
        if (parts[1] != collab)
        {
            errors.Add($"{rel}: bin 不在合集目录 Maps/{collab}/ 下");
        }

        string lobby = parts[2];
        string name = parts[^1];

        if (name == "ZZ-HeartSide.bin") return;

        if (lobby == "0-Lobbies")
        {
            if (!LobbyBinName().IsMatch(name))
            {
                warnings.Add($"{rel}: 大厅 bin 建议写成 <序号>-<名字>.bin 或 0-Prologue.bin");
            }
            return;
        }

        if (lobby == "0-Gyms")
        {
            if (!GymBinName().IsMatch(name))
            {
                warnings.Add($"{rel}: 健身房 bin 要和对应大厅同名，例如 1-Main.bin");
            }
            return;
        }

        string stem = Path.GetFileNameWithoutExtension(name);
        if (!stem.Contains('_'))
        {
            errors.Add($"{rel}: 小图 bin 应为 <作者>_<图>.bin");
            return;
        }

        if (!File.Exists(Path.ChangeExtension(full, ".meta.yaml")))
        {
            warnings.Add($"{rel}: 缺同名 .meta.yaml");
        }
    }

    private static void CheckAudio(string rel, string full, string collab, List<string> errors, List<string> warnings)
    {
        string name = Path.GetFileName(rel);

        if (name.EndsWith(".guids.txt", StringComparison.Ordinal))
        {
            string baseName = name[..^".guids.txt".Length];
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(full)!, baseName + ".bank")))
            {
                errors.Add($"{rel}: 找不到同名 .bank");
            }
            return;
        }

        if (!name.EndsWith(".bank", StringComparison.Ordinal)) return;

        string stem = name[..^".bank".Length];
        string[] segments = stem.Split('_');
        if (segments.Length < 4)
        {
            errors.Add($"{rel}: bank 名应为 <合集>_<作者>_<图>_<bgm|sfx>.bank");
            return;
        }
        if (segments[0] != collab)
        {
            errors.Add($"{rel}: bank 没带合集前缀 {collab}");
        }
        if (!AudioRoles.Contains(segments[^1]))
        {
            warnings.Add($"{rel}: bank 最后一段建议是 bgm/sfx/amb");
        }
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(full)!, stem + ".guids.txt")))
        {
            errors.Add($"{rel}: 缺同名 .guids.txt，缺了整包都不会加载");
        }
    }

    private static void CheckLua(string rel, string collab, List<string> warnings)
    {
        string[] parts = rel.Split('/');
        if (parts.Length < 5)
        {
            warnings.Add($"{rel}: 脚本建议放 Assets/{collab}/<作者>/<图>/ 下");
            return;
        }

        string stem = Path.GetFileNameWithoutExtension(rel);
        string[] segments = stem.Split('_');
        if (segments.Length < 3 || segments[^2] != "lua")
        {
            warnings.Add($"{rel}: 脚本名建议 <作者>_<图>_lua_<名字>.lua");
        }
    }

    /// <summary>同一个目录里只差大小写的文件名，在 Windows 上会被合并，在 Linux 上是两个文件。</summary>
    private static void CheckCaseCollisions(string root, List<string> errors)
    {
        foreach (string dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (dir.Split(Path.DirectorySeparatorChar).Any(SkipTop.Contains)) continue;

            Dictionary<string, string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                string name = Path.GetFileName(file);
                if (seen.TryGetValue(name, out string? other) && other != name)
                {
                    errors.Add($"{Path.GetRelativePath(root, dir)}: \"{other}\" 和 \"{name}\" 只差大小写，换平台会撞");
                }
                else
                {
                    seen[name] = name;
                }
            }
        }
    }

    private static bool TryReadPngSize(string path, out int width, out int height)
    {
        width = height = 0;
        try
        {
            using FileStream stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[24];
            if (stream.Read(header) < 24) return false;
            ReadOnlySpan<byte> signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (!header[..8].SequenceEqual(signature)) return false;
            if (header[12] != (byte)'I' || header[13] != (byte)'H' || header[14] != (byte)'D' || header[15] != (byte)'R') return false;
            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^(0-Prologue|\d+-[A-Za-z0-9_]+)\.bin$")]
    private static partial Regex LobbyBinName();

    [GeneratedRegex(@"^\d+-[A-Za-z0-9_]+\.bin$")]
    private static partial Regex GymBinName();
}
