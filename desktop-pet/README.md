# 小小湾 Windows 桌宠

从仓库 Releases 下载 `xiaoxiaowan-desktop.zip`，解压并双击 `XiaoXiaoWan.exe`。

左键拖动，单击挥手，双击跳跃；右键打开动作、大小、置顶、视线跟随、散步、暂停、隐藏和退出菜单。托盘图标左键显示或隐藏，右键打开同一菜单。

程序离线运行，无安装器、不添加开机启动、不更改 Codex 宠物。偏好保存在 `%LOCALAPPDATA%\XiaoXiaoWan\settings.json`。再次启动会显示已有桌宠。未作数字签名。

## 构建

Windows 10 / 11，使用系统 .NET Framework 4.x 的 C# 编译器和 WinForms，无 NuGet 依赖。图集与图标已包含在源码目录，无需重新生成素材。

在仓库根目录运行 PowerShell：

```powershell
& .\desktop-pet\src\build.ps1
```

输出为 `dist/desktop/XiaoXiaoWan.exe`。可指定输出目录：

```powershell
& .\desktop-pet\src\build.ps1 -Output 'C:\MyBuild\XiaoXiaoWan'
```

如需从 Codex 图集重新转换嵌入素材，可安装 Pillow 后运行 `desktop-pet/src/prepare_assets.py`。普通运行和编译不需要 Python。

## 自测

```powershell
Start-Process .\dist\desktop\XiaoXiaoWan.exe -ArgumentList '--self-test', "$PWD\dist\qa" -Wait
Start-Process .\dist\desktop\XiaoXiaoWan.exe -ArgumentList '--verify', "$PWD\dist\qa" -Wait
```

`--verify` 会短暂打开桌宠和白色测试背景，检查本机原生窗口交互，恢复鼠标并关闭测试实例，不读写用户偏好。使用不含空格的测试输出路径；如有空格，须将传入参数中的路径也加引号。

已生成的检查记录位于 `qa/`，验收边界见仓库根目录 `VALIDATION.md`。
