#!/usr/bin/env python3
"""合集目录与素材命名检查。

用法:
    python3 tools/check_assets.py .
    python3 tools/check_assets.py . --collab MyCollab

退出码 1 表示有错误。警告和提示不影响退出码。
"""
from __future__ import annotations

import argparse
import re
import struct
import sys
from pathlib import Path

# 作用词表，和 docs/02-素材命名规范.md 保持一致
ROLES = {
    "decal", "anim", "bg", "fg", "tile", "obj", "npc", "icon", "skull",
    "heart", "sticker", "map", "ui", "end", "bgm", "sfx", "amb", "lua",
    "readme", "license",
}
AUDIO_ROLES = {"bgm", "sfx", "amb"}
GAMEPLAY_CATS = {
    "decals", "bgs", "tilesets", "objects", "characters", "danger",
    "collectables", "animatedTiles", "scenery",
}
SKIP_TOP = {
    ".git", "contrib", "docs", "tools", "bin", "Code", "Loeb", "Ahorn",
    "Loenn", "Tutorials", "Dialog",
}
WINDOWS_BAD = re.compile(r'[<>:"|?*]')
ASCII_NAME = re.compile(r"^[A-Za-z0-9_.\-]+$")

errors: list[str] = []
warnings: list[str] = []
notes: list[str] = []


def err(msg: str) -> None:
    errors.append(msg)


def warn(msg: str) -> None:
    warnings.append(msg)


def png_size(path: Path) -> tuple[int, int] | None:
    try:
        with path.open("rb") as fh:
            head = fh.read(24)
    except OSError:
        return None
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n" or head[12:16] != b"IHDR":
        return None
    return struct.unpack(">II", head[16:24])


def check_name(rel: Path) -> None:
    for part in rel.parts:
        if not ASCII_NAME.match(part):
            err(f"{rel}: 路径里有非 ASCII / 非法字符: {part!r}")
            break
        if part.startswith(" ") or part.endswith(" "):
            err(f"{rel}: 目录或文件名首尾有空格: {part!r}")
            break
        if WINDOWS_BAD.search(part):
            err(f"{rel}: 名字里有 Windows 不认的字符: {part!r}")
            break


def check_gameplay_asset(rel: Path, collab: str) -> None:
    parts = rel.parts
    # Graphics/Atlases/Gameplay/<cat>/<Collab>/<Author>/<Map>/<file>
    tail = parts[4:]
    if not tail:
        return
    if tail[0] != collab:
        err(f"{rel}: 素材不在合集命名空间 {collab}/ 下，会跟别的 mod 撞路径")
        return
    if len(tail) < 4:
        if tail[1:] == ("_Shared",) or (len(tail) == 2 and tail[1] == "_Shared"):
            return
        warn(f"{rel}: 少了作者/地图两层（应为 {collab}/<作者>/<图>/...）")
        return
    author, mapname, filename = tail[1], tail[2], tail[3]
    if author == "_Shared":
        return
    stem = Path(filename).stem
    segs = stem.split("_")
    if len(segs) < 4:
        err(f"{rel}: 文件名不是 <作者>_<图>_<作用>_<名字> 四段式")
        return
    if segs[0] != author:
        warn(f"{rel}: 文件名里的作者 {segs[0]!r} 和目录 {author!r} 不一致")
    if segs[1] != mapname:
        warn(f"{rel}: 文件名里的图名 {segs[1]!r} 和目录 {mapname!r} 不一致")
    role = segs[2]
    if role not in ROLES:
        err(f"{rel}: 作用词 {role!r} 不在词表里（见 docs/02-素材命名规范.md）")
    if rel.suffix.lower() == ".png":
        size = png_size(rel)
        if size:
            w, h = size
            if w % 2 or h % 2:
                if role in {"decal", "anim", "bg", "fg", "tile", "obj"}:
                    warn(f"{rel}: 贴图尺寸 {w}x{h} 有奇数边，decal 容易出渲染缝隙")
    if stem[-1].isdigit() and role not in {"anim", "end"}:
        warn(f"{rel}: 文件名以数字结尾，游戏会当成动画帧；单张图请改名")


def check_map_bin(rel: Path, full: Path, collab: str) -> None:
    parts = rel.parts
    # Maps/<Collab>/<Lobby>/<bin>.bin
    if len(parts) < 4:
        err(f"{rel}: bin 层级不对，应为 Maps/{collab}/<大厅>/<图>.bin")
        return
    lobby, name = parts[2], Path(parts[3]).name
    if name == "ZZ-HeartSide.bin":
        return
    if lobby == "0-Lobbies":
        if not re.match(r"^(0-Prologue|\d+-[A-Za-z0-9_]+)\.bin$", name):
            warn(f"{rel}: 大厅 bin 建议写成 <序号>-<名字>.bin")
        return
    if lobby == "0-Gyms":
        if not re.match(r"^\d+-[A-Za-z0-9_]+\.bin$", name):
            warn(f"{rel}: 健身房 bin 要和对应大厅同名，例如 1-Main.bin")
        return
    stem = Path(name).stem
    if "_" not in stem:
        err(f"{rel}: 小图 bin 应为 <作者>_<图>.bin")
        return
    meta = full.with_suffix(".meta.yaml")
    if not meta.exists():
        warn(f"{rel}: 缺同名 .meta.yaml")


def check_audio(rel: Path, full: Path, collab: str) -> None:
    stem = rel.name
    if stem.endswith(".guids.txt"):
        base = stem[: -len(".guids.txt")]
        if not (full.parent / f"{base}.bank").exists():
            err(f"{rel}: 找不到同名 .bank")
        return
    if not stem.endswith(".bank"):
        return
    segs = stem[: -len(".bank")].split("_")
    if len(segs) < 4:
        err(f"{rel}: bank 名应为 <合集>_<作者>_<图>_<bgm|sfx>.bank")
        return
    if segs[0] != collab:
        err(f"{rel}: bank 没带合集前缀 {collab}")
    if segs[-1] not in AUDIO_ROLES:
        warn(f"{rel}: bank 最后一段建议是 bgm/sfx/amb")
    if not (full.parent / f"{stem[: -len('.bank')]}.guids.txt").exists():
        err(f"{rel}: 缺同名 .guids.txt，缺了整包都不会加载")


def check_lua(rel: Path, collab: str) -> None:
    parts = rel.parts
    if len(parts) < 5:
        warn(f"{rel}: 脚本建议放 Assets/{collab}/<作者>/<图>/ 下")
        return
    stem = Path(rel.name).stem
    segs = stem.split("_")
    if len(segs) < 3 or segs[2] != "lua":
        warn(f"{rel}: 脚本名建议 <作者>_<图>_lua_<名字>.lua")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("root", nargs="?", default=".")
    ap.add_argument("--collab", help="合集 id，默认读 CollabUtils2CollabID.txt")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    collab = args.collab
    if not collab:
        idfile = root / "CollabUtils2CollabID.txt"
        if idfile.exists():
            collab = idfile.read_text(encoding="utf-8").strip()
        else:
            print("找不到 CollabUtils2CollabID.txt，用 --collab 指定合集 id", file=sys.stderr)
            return 2

    for path in sorted(root.rglob("*")):
        if any(part in SKIP_TOP for part in path.relative_to(root).parts):
            continue
        rel = path.relative_to(root)
        if path.is_dir():
            continue
        if path.name in {".gitkeep", ".DS_Store", "Thumbs.db"}:
            continue
        if rel.parts[0] not in {"Maps", "Graphics", "Audio", "Assets"}:
            continue
        check_name(rel)
        if rel.parts[0] == "Maps" and path.suffix == ".bin":
            check_map_bin(rel, path, collab)
        elif rel.parts[:3] == ("Graphics", "Atlases", "Gameplay"):
            check_gameplay_asset(rel, collab)
        elif rel.parts[0] == "Audio":
            check_audio(rel, path, collab)
        elif rel.parts[0] == "Assets" and path.suffix == ".lua":
            check_lua(rel, collab)
        elif rel.parts[0] == "Graphics" and rel.parts[1] == "Atlases":
            if collab not in rel.parts[2:3] and collab not in rel.parts:
                err(f"{rel}: 素材不在合集命名空间 {collab}/ 下")

    for msg in errors:
        print("错误:", msg)
    for msg in warnings:
        print("警告:", msg)
    for msg in notes:
        print("提示:", msg)
    print(f"\n{len(errors)} 个错误 / {len(warnings)} 个警告")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
