# 决构编辑器 · MDPro3

部署到已有 MDPro3 本体的局面编辑扩展，用于 Combo 推演和卡片效果测试。YGOPro 版本见 [duel-structure-editor-ygopro](https://github.com/BAQjun/duel-structure-editor-ygopro)。

- 自定义双方开局，生成和编辑卡片、怪兽形态、状态与限制。
- 编辑超量素材、指示物、基本分、抽卡和卡组顺序。
- 双方视角切换、暂停、回溯、逐点前进、全局存档与分支恢复。
- 复用 MDPro3 原生界面和效果记录，编辑记录随回溯及存档恢复。
- Windows 安装器自动识别兼容的 IL2CPP / Mono 本体并选择组件。

## 适配范围

支持 MDPro3 IL2CPP v1.4.8 与 MyCard Mono 1.4.7-2 / Unity 6000.0.24f1 / Windows x64，安装器会检查本体兼容性。两种运行版本均已完成本体启动验证。

v0.3.3 在完整 MyCard Mono 本体中验证了编辑器入口、卡片数据库、素材与指示物编辑、存档与回溯，以及卸载后恢复原生主菜单。共享核心通过 139 项断言，Mono 素材与指示物界面通过 23 项检查；原有卡组及本体 Managed 程序集保持不变。这些测试不代表所有卡片效果均经过穷尽验证。

下载：[v0.3.3 安装器与源码](https://github.com/BAQjun/duel-structure-editor-mdpro3/releases/tag/v0.3.3)。可直接更新旧版。变更说明见 [0.3.3 Mono 修复](docs/0.3.3-Mono兼容修复.md)。

## 源码结构

- `extension`：IL2CPP 扩展、界面与原生交互。
- `extension-mono`：Mono 兼容层及最小成员访问适配器。
- `upstream/Assets/Scripts/MDPro3/DuelEditor`：编辑器自有的共享核心封装，不包含完整 MDPro3 本体源码。
- `native`：修改后的 YGOPro 核心与编辑状态接口。
- `installer`：Windows 安装器；`tools`、`tests`：构建、适配与验证工具。

## 构建

构建工具使用 Unity 6000.3.21f1 随附的编译工具、Python 3.14 和 LLVM-MinGW。工具和本体目录通过环境变量或 `build-config.local.json` 配置，无指定盘符要求。详见[路径配置与迁移](docs/路径配置与迁移.md)。

IL2CPP 构建运行 `tools/build_extension.ps1`，需要自己的 MDPro3 互操作程序集及 BepInEx IL2CPP 组件。Mono 构建运行 `tools/build_extension_mono.ps1`，需要将目标 Mono 本体的 Managed 程序集放入 `work/mono-reference`，并在 `vendor/bepinex-mono` 准备 BepInEx 5.4.23.5 Windows x64。原生核心构建依赖 YGOPro 版本的 Lua 和工具链，详见 `tools/build_core.ps1`。

安装器先通过两个 payload 构建脚本收集本机准备的运行依赖，再用 `tools/build_installer.ps1` 生成。仓库不包含 MDPro3 本体、目标版本的适配程序集、Unity 运行库、加载器二进制、卡图、数据库及玩家数据。仅克隆源码不会自动获得这些依赖。

## 第三方来源

核心来自 [Fluorohydride/ygopro-core](https://github.com/Fluorohydride/ygopro-core)，保留 `native/core/LICENSE` 中的 MIT 许可证。扩展使用 [BepInEx](https://github.com/BepInEx/BepInEx) 和 Harmony；这些依赖及 MDPro3、Unity 的版权和许可证属于原作者，本仓库不对其重新授权。

最近修复：[0.2.3 超量转连接的参数问题](docs/0.2.3-Link转换修复.md)。

最新修复：[0.2.4 自定义连接怪兽的表示限制](docs/0.2.4-连接表示限制.md)，已同步两个版本。

最近修复：[0.2.5 自定义连接箭头场面刷新](docs/0.2.5-连接箭头刷新.md)。

最新修复：[自定义连接怪兽的实际连接关系](docs/0.2.6-自定义连接关系.md)，真实脚本验证女男爵可在自定义箭头指向下发动并破坏狮鹫，核心回归 609 项通过。

新增：[对应召唤与素材履历](docs/0.3.0-对应召唤与素材履历.md)，包含真实素材组、来源与去向、龙骑士素材计数及 I:P 素材赋予抗性。

修复：[0.3.1 切换视角时恢复原生连锁](docs/0.3.1-切换视角连锁恢复.md)，避免径武赋予效果的无效显示指向错误卡片；实际双方切换与三段连锁测试通过。

路径兼容更新：v0.3.2 移除开发机盘符与目录依赖，配置和迁移说明见[路径配置与迁移](docs/路径配置与迁移.md)。
