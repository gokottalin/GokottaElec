# Agent20 第1次测试问题上报

日期码：2605  
测试日期：2026-05-11  
输出对象：Agent0  
测试原则：只上报问题，不修改实现。

## 本次测试电路

电路 ID：`AGENT20_TEST01_COMPARATOR_OPEN_DRAIN`

测试类型：比较器开漏输出 + 电阻上拉 + 输入信号 + 分压参考。

输入文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\comparator-open-drain-pullup.cnl
```

主要验证目标：

- CNL 解析能否接受 `COMPARATOR_SINGLE`。
- ERC 能否正确识别 `output_stage=open_drain` 且存在上拉电阻。
- SVG 渲染是否能生成可读原理图，而不是仅输出器件摘要。
- 桌面 exe 命令行入口是否能给用户返回可见构建结果。
- Web 小程序在无后端时是否有明确状态提示。

## 执行记录

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\build-paste
```

结果：退出码 `0`，生成 `summary.txt`、`.cnl`、`.ir.json`、`.erc.txt`、`.svg`。

```powershell
node scripts\elec-cli.mjs build output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\cli-build
```

结果：退出码 `0`，控制台输出 `OK`、`IR`、`ERC`、`SVG` 路径。

```powershell
.\dist\GokottaElec.exe output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\dist-exe-build
```

结果：退出码 `0`，产物生成成功，但 PowerShell 控制台没有任何状态输出。

```powershell
npm.cmd run build:sample1
```

结果：退出码 `0`，官方 Sample 01 基线构建成功。

浏览器验证：

- 打开生成的比较器 SVG。
- 打开 `web-miniapp/gokotta-elec.html`。
- 点击 `生成预览`。
- Web 页面显示 `接口未接入或生成失败`，诊断日志为 `Failed to fetch`。该状态符合当前无后端环境预期，本轮不列为问题。

## 问题列表

### A20-T01-P1：`COMPARATOR_SINGLE` 通过解析和 ERC 后只生成 fallback 摘要图，没有生成原理图拓扑

严重级别：P2  
影响模块：SVG 渲染 / 用户预览体验  
关联文件：

```text
D:\Project\2605-Elec\scripts\render-svg.mjs
```

复现步骤：

1. 使用本报告中的比较器开漏输出 CNL。
2. 执行：

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\build-paste
```

3. 打开生成的 SVG：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.svg
```

实际结果：

- ERC 输出为 `OK`。
- SVG 第 29 行包含：

```text
No dedicated schematic recognizer matched; showing deterministic symbol summary.
```

- 画面只显示 `V1`、`V2`、`U1`、`R1`、`R2`、`R3` 的摘要卡片。
- 没有比较器三角符号、输入端 `IN+` / `IN-`、开漏输出 `OUT`、上拉电阻 `R3` 到 `VCC`、参考分压 `R1/R2`、电源轨和地线的拓扑连接。

期望结果：

- 如果 `COMPARATOR_SINGLE` 是 V1.3 支持器件，并且 ERC 支持 `open_drain` 上拉规则，则 SVG 渲染应至少提供比较器专用拓扑图。
- 如果当前版本不计划渲染比较器，构建或预览层应明确标注为“支持校验但不支持专用原理图渲染”，避免用户误以为已经生成完整电路图。

证据文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\evidence\comparator-svg-fallback.png
```

定位线索：

- `renderFallback` 位于 `scripts/render-svg.mjs:917`。
- 当前专用渲染器选择链位于 `scripts/render-svg.mjs:931`，未包含比较器渲染分支。

### A20-T01-P2：`dist\GokottaElec.exe` 命令行构建成功但控制台无任何输出

严重级别：P2  
影响模块：发布版 exe / 命令行用户体验  
关联文件：

```text
D:\Project\2605-Elec\launcher\Program.cs
```

复现步骤：

1. 在 PowerShell 中执行：

```powershell
.\dist\GokottaElec.exe output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\dist-exe-build
```

2. 查看命令执行后的控制台输出。
3. 查看输出目录：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\dist-exe-build
```

实际结果：

- 进程退出码为 `0`。
- `.erc.txt`、`.ir.json`、`.svg` 文件均生成成功。
- PowerShell 控制台没有显示 `OK`、输出路径、失败原因或任何完成状态。

期望结果：

- exe 命令行模式应向调用终端输出与 `node scripts\elec-cli.mjs build ...` 等价或兼容的状态文本。
- 成功时至少输出 `OK`、`IR`、`ERC`、`SVG` 路径。
- 失败时至少输出解析或 ERC 诊断。

定位线索：

- `RunCommandLine` 位于 `launcher\Program.cs:1244`。
- `ProcessStartInfo.RedirectStandardOutput=false` 位于 `launcher\Program.cs:1262`。
- `ProcessStartInfo.RedirectStandardError=false` 位于 `launcher\Program.cs:1263`。
- 当前 exe 是桌面入口，实际命令行调用时标准输出不可见。

### A20-T01-P3：文档引用了当前仓库不存在的校验脚本和 npm script

严重级别：P3  
影响模块：文档 / Agent 对接 / 验证流程  
关联文件：

```text
D:\Project\2605-Elec\docs\erc-rules-v0.1.md
D:\Project\2605-Elec\docs\component-library-notes-v0.1.md
D:\Project\2605-Elec\package.json
```

复现步骤：

1. 检查脚本文件是否存在：

```powershell
Test-Path scripts\validate-ir.mjs
Test-Path scripts\validate-model-packages.mjs
```

2. 检查 `package.json` 是否存在 `validate:models` script。

实际结果：

- `scripts\validate-ir.mjs = False`
- `scripts\validate-model-packages.mjs = False`
- `package.json` 中不存在 `validate:models`。
- 但是文档仍引用这些入口：
  - `docs\erc-rules-v0.1.md:3` 引用 `scripts/validate-ir.mjs`。
  - `docs\component-library-notes-v0.1.md:104` 引用 `npm.cmd run validate:models`。
  - `docs\component-library-notes-v0.1.md:105` 引用 `node scripts/validate-model-packages.mjs`。
  - `docs\component-library-notes-v0.1.md:110` 再次引用 `validate-ir.mjs`。

期望结果：

- 文档中的验证入口必须与仓库实际文件和 `package.json` script 一致。
- 如果旧脚本已被 `scripts\elec-cli.mjs validate` 或 `scripts\erc-check.mjs` 替代，文档应统一更新为真实入口。

## 本轮未列为问题的观察

- 本轮比较器电路的 CNL 解析、ERC、IR 生成均成功。
- 本轮未发现生成 SVG 中有明显符号互相重叠。
- Web 小程序在本地 file 模式下显示 `Sample API 未接入` 和 `Failed to fetch`，符合当前骨架未接后端的预期状态。
- 浏览器控制台出现 Codex/Electron preload 相关错误，来源不是项目页面脚本，本轮不计入项目问题。

## 本轮产物索引

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\
```

关键文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\comparator-open-drain-pullup.cnl
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\summary.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.erc.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\evidence\comparator-svg-fallback.png
D:\Project\2605-Elec\output\agent20-test-2605-01\evidence\web-api-missing-state.png
```
