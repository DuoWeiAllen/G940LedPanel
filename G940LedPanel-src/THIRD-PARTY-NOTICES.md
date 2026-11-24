# 第三方声明

本项目以 **MIT 许可**发布（见 `LICENSE`）。下面列出开发过程中参考过的外部资料，
以及它们各自的许可，和本项目与之的边界。

一句话原则：**只取「设备是什么行为」这一事实，不取「别人怎么写的」这段代码。**

---

## 1. Linux 内核 G940 驱动（参考协议事实）

| 项 | 内容 |
|---|---|
| 来源 | `drivers/hid/hid-lg.c`（Linux 内核主线）<br>`chrisboyle/G940-linux`（同一份 LED 支持代码的前身，已提交上游） |
| 许可 | **GPL-2.0** / GPL-2.0-or-later |
| 本项目如何使用 | 仅参考其记录的设备协议事实 |
| 是否复制代码 | **否** |

参考到的**事实**（这些是设备固件的行为，不是任何人的表达）：

- 8 颗灯挂在 HID **Feature 报告 ID = 3** 上
- 前 8 个位是 P1~P8 的红灯位，后 8 个位是 P1~P8 的绿灯位
- 同位红绿都置 1 得到琥珀色
- 不存在单独的"使能"前置命令

本项目的实现（`Core/G940Hid.cs`）是用 C# 重写的字节数组加一次 Win32 调用，
与上述 C 代码在结构、函数、命名、注释上均无对应关系，不构成衍生作品。

> 这些事实另有两条**完全独立**的来源可以印证，不依赖上述 GPL 代码：
> ① 设备自身返回的描述符与 caps（`HidP_GetCaps`）；
> ② 罗技 SDK 中 `_SetLEDs(device, byte red, byte green)` 的两字节签名。

---

## 2. G940LedInterface（1.3.38 起不再使用）

| 项 | 内容 |
|---|---|
| 来源 | [cyberluke/G940LedInterface](https://github.com/cyberluke/G940LedInterface) |
| 许可 | **Unlicense**（公有领域贡献） |
| 曾经的角色 | v1.3.37 及以前，通过 `G940LedInterface.dll` 间接调用罗技 Logitech SDK 驱动面板 LED |
| 现状 | **1.3.38 起已完全移除**：不再分发该 DLL，也不再引用其任何接口 |

移除原因与许可无关（Unlicense 允许自由使用），而是技术上不再需要：
自研 HID 直写已经真机验证可用，且不依赖 Windows 专属的静态链接库，
为将来移植保留可能。

---

## 3. 系统 API（无版权障碍）

| 来源 | 用途 |
|---|---|
| Windows `hid.dll`（`HidD_SetFeature` / `HidP_GetCaps` 等） | 读写 HID Feature 报告 |
| Windows `setupapi.dll` | 枚举 HID 设备接口 |
| Windows `dinput8.dll` | 读取摇杆/油门轴与按钮 |

均为操作系统公开文档化的 API，按系统许可在 Windows 上调用，不涉及再分发。

---

## 4. 运行时依赖（NuGet，随程序分发）

| 包 | 许可 |
|---|---|
| `System.Text.Json` | MIT |
| `Microsoft.NETFramework.ReferenceAssemblies` | MIT（仅编译期，不随程序分发） |

---

## 5. 商标

Logitech、G940、Logitech Flight System G940 是罗技（Logitech）的商标。
本项目为第三方独立开发的兼容工具，**与罗技公司无隶属、赞助或背书关系**，
不包含罗技的任何代码或资源文件。

---

## 维护约定

- 新增任何第三方代码或资源，必须在本文件登记：来源、许可、用途、是否复制代码
- 只参考协议事实、未复制代码的，要在对应源文件的注释里注明出处
- **不得把 GPL 代码复制进本项目**——一旦复制，整个项目必须改为 GPL，
  这与当前的 MIT 声明冲突
