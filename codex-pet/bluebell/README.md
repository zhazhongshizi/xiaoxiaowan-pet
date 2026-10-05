# 小小湾 Codex Pet

将本文件夹放到 Windows 的 `%USERPROFILE%\.codex\pets\bluebell\`，至少保留 `pet.json` 和 `spritesheet.webp`。在 Codex 宠物选择器中选择“小小湾”。

稳定内部 ID 为 `bluebell`，显示名称为“小小湾”。安装不会自动改动当前宠物。

## 动画

- v2 格式，透明 WebP，1536 x 2288。
- 8 列 x 11 行，单帧 192 x 208。
- 九种状态：idle、running-right、running-left、waving、jumping、failed、waiting、running（专注工作）、review。
- 16 个顺时针视线方向，从向上开始。

打开 `preview.html` 可离线预览动作、播放速度、逐帧、背景和指针视线。GIF 位于 `qa/previews/`，静态帧位于 `qa/stills/`。

图集结构、透明度、动画配准和浏览器预览已检查。实际 Codex 桌面端的宠物选择与播放仍需用户验收。参见仓库根目录的 `VALIDATION.md`。
