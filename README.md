# 决构编辑器 · MDPro3

部署到已有 MDPro3 本体的局面编辑扩展，用于 Combo 推演和卡片效果测试。YGOPro 版本见 [duel-structure-editor-ygopro](https://github.com/BAQjun/duel-structure-editor-ygopro)。

- 自定义双方开局，生成和编辑卡片、怪兽形态、状态与限制。
- 编辑超量素材、指示物、基本分、抽卡和卡组顺序。
- 双方视角切换、暂停、回溯、逐点前进、全局存档与分支恢复。
- 复用 MDPro3 原生界面和效果记录，编辑记录随回溯及存档恢复。
- Windows 安装器自动识别兼容的 IL2CPP / Mono 本体并选择组件。

## 适配范围

目标为 MDPro3 v1.4.8 / Unity 6000.0.24f1 / Windows x64，安装器会检查本体兼容性。原有 IL2CPP 版本已做过本体运行验证。Mono 版是按用户提供的程序集制作的适配验证版，尚未完成完整 Mono 本体的实际游戏启动和界面验证。

Mono 组件已通过 83 项 64 位核心断言、211 个游戏成员访问检查、存档记录压缩读写及安装、更新、卸载检查。具体范围见 `docs/MDPro3-Mono-适配验证.txt`。

## 源码结构

- `extension`：IL2CPP 扩展、界面与原生交互。
- `extension-mono`：Mono 兼容层及最小成员访问适配器。
- `upstream/Assets/Scripts/MDPro3/DuelEditor`：编辑器自有的共享核心封装，不包含完整 MDPro3 本体源码。
- `native`：修改后的 YGOPro 核心与编辑状态接口。
- `installer`：Windows 安装器；`tools`、`tests`：构建、适配与验证工具。

## 构建

将仓库放在 D:。现有工具使用 Unity 6000.3.21f1 随附的编译工具、Python 3.14 和 LLVM-MinGW，路径需按本机调整。

IL2CPP 构建运行 `tools/build_extension.ps1`，需要自己的 MDPro3 互操作程序集及 BepInEx IL2CPP 组件。Mono 构建运行 `tools/build_extension_mono.ps1`，需要将目标 Mono 本体的 Managed 程序集放入 `work/mono-reference`，并在 `vendor/bepinex-mono` 准备 BepInEx 5.4.23.5 Windows x64。原生核心构建依赖 YGOPro 版本的 Lua 和工具链，详见 `tools/build_core.ps1`。

安装器先通过两个 payload 构建脚本收集本机准备的运行依赖，再用 `tools/build_installer.ps1` 生成。仓库不包含 MDPro3 本体、用户提供的适配程序集、Unity 运行库、加载器二进制、卡图、数据库及玩家数据。仅克隆源码不会自动获得这些依赖。

## 第三方来源

核心来自 [Fluorohydride/ygopro-core](https://github.com/Fluorohydride/ygopro-core)，保留 `native/core/LICENSE` 中的 MIT 许可证。扩展使用 [BepInEx](https://github.com/BepInEx/BepInEx) 和 Harmony；这些依赖及 MDPro3、Unity 的版权和许可证属于原作者，本仓库不对其重新授权。

最近修复：[0.2.3 超量转连接的参数问题](docs/0.2.3-Link转换修复.md)。

最新修复：[0.2.4 自定义连接怪兽的表示限制](docs/0.2.4-连接表示限制.md)，已同步两个版本。

最近修复：[0.2.5 自定义连接箭头场面刷新](docs/0.2.5-连接箭头刷新.md)。
