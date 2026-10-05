# 小小湾

根据角色参考图制作的蓝发红瞳小伙伴，提供 **Codex Pet 版** 和 **独立 Windows 桌宠版**。

![小小湾待机动画](docs/idle.gif)
![小小湾视线环视](docs/look-clockwise.gif)

## 下载

前往 [Releases](https://github.com/zhazhongshizi/xiaoxiaowan-pet/releases/latest)：

| 文件 | 用途 |
| --- | --- |
| `xiaoxiaowan-codex-pet.zip` | Codex 宠物，含透明动画和本地预览 |
| `xiaoxiaowan-desktop.zip` | Windows 独立桌宠，解压后运行 `XiaoXiaoWan.exe` |
| `SHA256SUMS.txt` | 下载文件校验值 |

## Codex Pet 版

将压缩包中的 `bluebell` 文件夹放入 `%USERPROFILE%\.codex\pets\`，目录结构应为：

```text
%USERPROFILE%\.codex\pets\bluebell\pet.json
%USERPROFILE%\.codex\pets\bluebell\spritesheet.webp
```

在 Codex 的宠物选择器中选择 **小小湾**。内部 ID 保留为 `bluebell`；这不影响显示名称。包中不自动更换当前宠物。

本地预览：打开 `bluebell/preview.html`。九种动作、16 方向视线、透明 WebP，使用 v2 精灵格式。

## 普通桌宠版

Windows 10 / 11，使用系统 .NET Framework 4.x；无需 Codex、Python、Node 或联网。

- 左键拖动，单击挥手，双击跳跃。
- 右键菜单：动作、四档大小、置顶、视线跟随、散步、暂停、隐藏、退出。
- 托盘图标：左键显示或隐藏，右键打开菜单。
- 再次启动会显示已运行的桌宠，不重复创建。
- 配置保存在 `%LOCALAPPDATA%\XiaoXiaoWan\settings.json`。

透明区域不遮挡桌面操作。不添加开机启动，不修改 Codex 宠物。程序未签名，Windows 可能提示未知发布者。

## 源码与验证

- [独立桌宠源码和构建说明](desktop-pet/README.md)
- [Codex 包说明](codex-pet/bluebell/README.md)
- [验证记录](VALIDATION.md)

角色素材根据用户提供的参考图制作。公开上传不代表授予原角色或参考图的知识产权；使用、再分发前请确认相应权利。本仓库未声明额外开源许可。
