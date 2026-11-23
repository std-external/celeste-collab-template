#!/usr/bin/env python3
"""把各作者的对话片段合并进合集的 Dialog/*.txt。

用法:
    python3 tools/merge_dialog.py --into "Dialog/English.txt" contrib/*/*/dialog.en.txt

规则:
  * 以 <id>= 开头的行算一条新条目，后面的行都归它（缩进的续行）。
  * 目标里已有的键：内容一致就跳过，不一致就报错退出，不覆盖。
  * 片段里键重复也报错。
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

KEY_LINE = None  # 延迟编译


def parse(path: Path) -> list[tuple[str | None, list[str]]]:
    import re

    global KEY_LINE
    if KEY_LINE is None:
        KEY_LINE = re.compile(r"^([A-Za-z0-9_]+)=")
    blocks: list[tuple[str | None, list[str]]] = []
    key: str | None = None
    buf: list[str] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        m = KEY_LINE.match(line)
        if m:
            if key is not None or buf:
                blocks.append((key, buf))
            key, buf = m.group(1), [line]
        else:
            buf.append(line)
    if key is not None or buf:
        blocks.append((key, buf))
    return blocks


def value_of(block: list[str]) -> str:
    return "\n".join(line.strip() for line in block).strip()


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--into", required=True, help="目标对话文件")
    ap.add_argument("sources", nargs="+", help="各作者的片段")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    target = Path(args.into)
    if not target.exists():
        print(f"目标不存在: {target}", file=sys.stderr)
        return 2

    blocks = parse(target)
    existing = {k: value_of(b) for k, b in blocks if k}

    added = 0
    for src in args.sources:
        path = Path(src)
        if not path.exists():
            print(f"跳过不存在的片段: {path}", file=sys.stderr)
            continue
        for key, block in parse(path):
            if key is None:
                continue
            value = value_of(block)
            if key in existing:
                if existing[key] != value:
                    print(
                        f"冲突: {key} 在 {path} 和目标里内容不同，先人工确认",
                        file=sys.stderr,
                    )
                    return 1
                print(f"已存在，跳过: {key}")
                continue
            blocks.append((key, block))
            existing[key] = value
            added += 1
            print(f"合入: {key}  <- {path}")

    if args.dry_run:
        print(f"dry-run：会新增 {added} 条")
        return 0

    out: list[str] = []
    for key, block in blocks:
        out.extend(block)
    target.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"写入 {target}，新增 {added} 条")
    return 0


if __name__ == "__main__":
    sys.exit(main())
