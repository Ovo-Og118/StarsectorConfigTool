# Starsector 基础配置工具

一个用于修改《远星物语》（Starsector）基础配置的小工具。

涵盖运存、技能与等级、舰队与战斗规模等常用项。单文件 exe，Windows 自带 .NET Framework 运行时，**不需要安装任何环境**。

---

## 功能

### 运存（JVM 堆）

自动定位启动参数文件（`Miko_Simple.txt` / `vmparams` 等），修改 `-Xms` 与 `-Xmx`。

- 同时改写 `-Xms` 和 `-Xmx`，避免出现 `Xms > Xmx` 导致 JVM 拒绝启动
- 提供 4 / 6 / 8 / 12 / 16 / 24 / 32 GB 快捷按钮
- 会显示当前实际生效的数值和对应的参数文件路径

### 技能与等级

| 设置项 | 对应键 | 原版值 |
| --- | --- | --- |
| 玩家等级上限 | `playerMaxLevel` | 50 |
| 每级技能点 | `skillPointsPerLevel` | 2 |
| 每级故事点 | `storyPointsPerLevel` | 16 |
| 经验获取倍率 | `xpGainMult` | 1 |
| 军官等级上限 | `officerMaxLevel` | 5 |
| 军官升级经验倍率 | `officerXPRequiredMult` | 4 |

### 舰队与战斗

| 设置项 | 对应键 | 原版值 |
| --- | --- | --- |
| 基础军官上限 | `baseNumOfficers` | 8 |
| 舰队舰船上限 | `maxShipsInFleet` | 30 |
| 战斗规模上限 | `maxBattleSize` | 600 |
| 默认战斗规模 | `defaultBattleSize` | 400 |
| 永久插件上限 | `maxPermanentHullmods` | 2 |
| 每点幅能容量 | `fluxPerCapacitor` | 200 |

---

## 使用

1. 关闭游戏
2. 双击 `StarsectorConfigTool.exe`
3. 程序会自动寻找游戏目录；找不到就点「浏览...」选择**包含 `starsector-core` 文件夹的那一层**
4. 改数值 → 点「应用修改」
5. 重新启动游戏

> 界面上显示的是磁盘里的真实值。改完会重新读盘，所以不会出现"界面显示改了但文件没变"的情况。

---

## 安全性

这个工具动的是游戏配置文件，所以做了几件事：

**自动备份**

每次点「应用修改」，被改动的文件都会先复制一份到游戏目录下的 `_scf_backup` 文件夹，文件名带时间戳。随时可以点「备份还原...」还原任意一份，也可以直接去文件夹里手动拷回去。

**只改数值，不动格式**

`settings.json` 里混着 `#` 注释、尾随注释和制表符缩进，它不是合法的纯 JSON。如果按 JSON 解析再序列化写回，注释会全部丢掉，配置的可读性就毁了。

所以工具只在原文本上精确替换数字那一段，其余字节（缩进、注释、换行风格、编码）一律原样保留。测试里专门校验了「行数未变」「注释数量未变」「无 BOM」。

**键名必须唯一**

如果某个键在文件里出现多次，工具会拒绝修改该键而不是猜一个。找不到的键会跳过并在日志里说明。

**游戏运行检测**

检测到 `java` 进程时会提醒你先关游戏，因为游戏退出或存档时可能覆盖你的改动。

---

## 从源码构建

需要 [.NET SDK](https://dotnet.microsoft.com/download)（8.0 及以上）。

```powershell
git clone <this-repo>
cd StarsectorConfigTool
dotnet build -c Release
```

产物在 `bin\Release\net48\StarsectorConfigTool.exe`。

或者直接用附带的脚本：

```powershell
.\build.ps1
```

会把 exe 放到 `dist\` 里。

### 目标框架说明

项目目标框架是 **.NET Framework 4.8**，不是 .NET 8。原因：

- .NET Framework 4.8 是 **Windows 10 / 11 自带的**，用户不需要额外装运行时
- 产物是几十 KB 的单文件 exe，适合放 GitHub Release
- 如果用 .NET 8 的 WinForms，用户得先装 .NET Desktop Runtime；自包含发布则会变成 100 MB+

编译时会通过 NuGet 自动拉取 `Microsoft.NETFramework.ReferenceAssemblies`，所以**本机不需要装 .NET Framework 开发包**。

---

## 测试

`tests/CoreTest` 是一个控制台测试项目，直接引用主程序的 `Core` 代码，覆盖：

- 目录识别
- `settings.json` 读写（含格式保持校验）
- JVM 参数定位与读写
- 备份与还原

运行前准备一个测试用的目录副本（把 `settings.json`、`Miko_Simple.txt`、启动用的 `.bat` 拷进去），然后：

```powershell
cd tests\CoreTest
dotnet build -c Release
.\bin\Release\net48\CoreTest.exe "测试目录路径"
```

测试只操作副本，不会碰真实游戏文件。

---

## 常见问题

**改了没效果？**

游戏必须完全退出再重新启动。进游戏后等级/技能相关的数值可能需要升级一次或读档后才会刷新在界面上。

**运存页显示"不可用"？**

说明没找到含 `-Xmx` 的参数文件。常见于非整合包或用第三方启动器的情况。可以手动确认游戏根目录下哪个文件里有 `-Xmx`，然后在 issue 里告诉我文件名和启动方式。

**能不能改 mod 相关的东西？**

当前版本只做基础配置。像修改 jar 里的字节码（例如无人舰点数上限）那种属于另一类操作，风险和工作量都高得多，暂不包含。

**会不会和 mod 冲突？**

不会。工具只动 `starsector-core\data\config\settings.json` 和启动参数文件，不碰 `mods` 目录、不碰存档。

---

## 免责声明

修改游戏配置属于玩家自行决定的行为。使用前请了解：

- 工具会自动备份，但**请你自己也留一份重要文件的副本**
- 数值改得过于离谱可能导致游戏不稳定或存档异常
- 部分游戏模式（如铁人模式）不提供容错空间，建议谨慎

本项目与 Fractal Softworks 无关。

---

## 许可

[MIT](LICENSE)
