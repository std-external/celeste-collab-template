# 合集地图模板

一个能直接改名用的合集骨架，按「合集 / 作者 / 地图」三层放东西，所有素材名字都带前缀。

## 五分钟上手

1. 复制整个 `CollabTemplate/`，改成你的合集目录名。
2. 全项目替换 `ExampleCollab` → 你的合集 id（只能用字母和数字）。
3. `everest.yaml` 里改 `Name`、版本、Everest 版本。
4. `CollabUtils2CollabID.txt` 内容改成同一个 id。
5. 把 `AuthorA` / `ExampleMap` 换成真作者和真图，删掉示例文件，只留 `.gitkeep` 占位。
6. 按 `docs/01-模板与目录规范.md` 往对应格子里放东西。

## 规矩速记

- 结构不能自己发明：`0-Lobbies`、`0-Gyms`、`ZZ-HeartSide` 是 CollabUtils2 认死的。
- 素材全落在 `<合集>/<作者>/<图>/` 下，别丢到 `bgs/`、`decals/` 根目录。
- 文件名 `<作者>_<图>_<作用>_<名字>`，只用 ASCII 和下划线。
- 音频 bank、对话 id、session flag、sprite id 必须带全前缀，这些是全局唯一的。
- 一个作者一张图一个目录，作者交第二张图就新建目录，不要复用。

## 文档

| 文件 | 内容 |
|---|---|
| `docs/00-调研-现有Collab怎么组织.md` | SJ2021 / Acorn / Surprise / 中文社区的对比和教训 |
| `docs/01-模板与目录规范.md` | 目录树，每格放什么，一张图可以有什么 |
| `docs/02-素材命名规范.md` | 四段式命名、作用词表、全局唯一清单、反例 |
| `docs/03-提交与合并检查清单.md` | 交图、合并、打包、验收 |
| `docs/templates/` | meta、贴纸、心换皮、作者交图说明的样例 |

## 工具

```
python3 tools/check_assets.py .                      # 查命名和路径
python3 tools/merge_dialog.py --into Dialog/English.txt contrib/*/*/dialog.en.txt
```

检查脚本会报三类：错误（必须改）、警告（建议改）、提示（可以不管）。
