# Agent20 Test 2605-01 问题分析与现有 Agent 分工落位

日期：2026-05-11  
测试输出目录：`D:\Project\2605-Elec\output\agent20-test-2605-01\`  
测试电路：`AGENT20_TEST01_COMPARATOR_OPEN_DRAIN`  
输入文件：`D:\Project\2605-Elec\output\agent20-test-2605-01\comparator-open-drain-pullup.cnl`

## 0. 本文档原则

本文档只做一件事：把 Agent20 本轮测试发现的问题，落到现有 Agent 职责体系中。

本文档不做以下事情：

- 不重新规划 Agent 职责。
- 不新建 Agent 职责名称。
- 不覆盖 `AGENT_WORKPLAN.md` 中已经确认的分工原则。
- 不改变原 Agent0-Agent4 的固定职责。
- 不让 Agent6 介入本轮实现，除非用户后续明确授权。

现有职责边界沿用当前仓库文档：

- Agent0：顶层规范、汇总、封装、合并、版本与发布协调。
- Agent1：CNL parser，负责受控中文到 AST/IR 的解析链路。
- Agent2：器件库、端子、封装、PinMap、边界条件。
- Agent3：ERC/DRC 规则引擎、验证入口、电气规则一致性。
- Agent4：图形生成，负责从 IR 输出 SVG/KiCad/交互式电路图。
- Agent5：GokottaElec 与 GokottaMaker 的网页同步、接口、LLM 对接文件同步。
- Agent6：美观设计与体验一致性。本轮不调用。
- Agent20：测试与问题上报。本轮已完成只上报，不修改实现。

## 1. Agent0 已完成的前置工作

Agent0 已完成：

1. 读取 `output\agent20-test-2605-01\` 下的测试输入、构建输出、截图证据。
2. 确认三条链路均能生成产物：
   - `build-paste`
   - `cli-build`
   - `dist-exe-build`
3. 确认比较器测试电路的解析与 ERC 成功。
4. 确认比较器 SVG 进入 fallback 摘要图。
5. 将三个问题交给现有 Agent 做只读复核：
   - Pauli 复核 SVG fallback。
   - Confucius 复核 exe 命令行无输出。
   - Pascal 复核文档与真实脚本入口不一致。
6. 三个复核 Agent 均未修改文件，未动用 Agent6。

## 2. 本轮测试电路事实

当前测试电路是比较器开漏输出加上拉电阻：

```text
U1 = COMPARATOR_SINGLE, model=LM393, output_stage=open_drain
U1.V+  -> VCC
U1.V-  -> GND
U1.IN+ -> VIN
U1.IN- -> VREF
U1.OUT -> VOUT
R3.A   -> VCC
R3.B   -> VOUT
R1/R2  -> VCC - VREF - GND reference divider
V2     -> VIN signal source referenced to GND
```

关键产物：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\summary.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.ir.json
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.erc.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\cli-build\comparator-open-drain-pullup.ir.json
D:\Project\2605-Elec\output\agent20-test-2605-01\cli-build\comparator-open-drain-pullup.erc.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\cli-build\comparator-open-drain-pullup.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\dist-exe-build\comparator-open-drain-pullup.ir.json
D:\Project\2605-Elec\output\agent20-test-2605-01\dist-exe-build\comparator-open-drain-pullup.erc.txt
D:\Project\2605-Elec\output\agent20-test-2605-01\dist-exe-build\comparator-open-drain-pullup.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\evidence\comparator-svg-fallback.png
D:\Project\2605-Elec\output\agent20-test-2605-01\evidence\web-api-missing-state.png
```

## 3. 问题 A20-T01-P1：COMPARATOR_SINGLE 进入 SVG fallback

严重级别：P2  
现有职责归属：Agent4  
同步评估：Agent5 需要知情，因为 SVG 输出能力变化会影响网页端预览能力说明。  
Agent6：本轮不介入。

### 3.1 事实

ERC 输出为：

```text
OK
```

但生成 SVG 包含：

```text
No dedicated schematic recognizer matched; showing deterministic symbol summary.
```

出现 fallback 的文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\cli-build\comparator-open-drain-pullup.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\dist-exe-build\comparator-open-drain-pullup.svg
D:\Project\2605-Elec\output\agent20-test-2605-01\build-paste\01-agent20_test01_comparator_open_drain\agent20_test01_comparator_open_drain.svg
```

### 3.2 根因

`COMPARATOR_SINGLE` 已经存在于器件库和 CNL 契约中：

```text
components\core-components.v0.1.json
docs\circuit-cnl-v0.1.md
docs\llm-cnl-contract-v0.1.md
```

当前缺口在图形生成层。`scripts\render-svg.mjs` 的渲染选择链路没有比较器专用分支。

### 3.3 指定给现有职责 Agent 的任务

#### Agent4 执行

Agent4 按既有“图形生成”职责处理。

写入范围：

```text
scripts\render-svg.mjs
必要时补充 docs 中的渲染支持说明
```

任务要求：

1. 新增 `COMPARATOR_SINGLE` 专用 SVG renderer。
2. 建议函数名：`renderComparatorOpenDrainPullup()`。
3. 调用顺序应放在 `renderOpampNonInverting()` 与 `renderVoltageDivider()` 之前。
4. 当前测试电路至少要画出：
   - 比较器符号 U1。
   - `IN+` 连接 `VIN`。
   - `IN-` 连接 `VREF`。
   - `OUT` 连接 `VOUT`。
   - `R3` 从 `VCC` 上拉到 `VOUT`。
   - `R1/R2` 从 `VCC` 分压到 `VREF` 再到 `GND`。
   - `V1` 电源。
   - `V2` 输入信号源。

识别边界：

```text
必须存在 COMPARATOR_SINGLE。
IN+, IN-, OUT, V+, V- 必须全部接网。
V+ 与 V- 禁止同网。
output_stage=open_drain 或 open_collector 时，OUT 必须有上拉路径。
本轮先支持 V- = GND；V- = VEE 可后续扩展。
```

#### Agent5 执行同步判断

Agent5 不替代 Agent4 实现 SVG renderer。  
Agent5 只判断并维护网页端/LLM 对接说明：

```text
比较器 SVG 已支持或未支持的能力说明
GokottaMaker 网页端预览能力说明
LLM handoff 中 COMPARATOR_SINGLE 的示例或边界条件是否需要更新
```

### 3.4 验收命令

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\build-paste-after-fix
Select-String -Path output\agent20-test-2605-01\build-paste-after-fix\**\*.svg -Pattern "No dedicated schematic recognizer matched"
```

验收标准：

```text
构建退出码为 0。
ERC 为 OK。
SVG 不包含 fallback 文案。
SVG 中出现 U1、R1、R2、R3、VIN、VREF、VOUT、VCC、GND。
5 个官方 Sample 仍全部通过。
```

## 4. 问题 A20-T01-P2：发布版 exe 命令行模式无可见输出

严重级别：P2  
现有职责归属：Agent0  
同步评估：Agent5 需要知情，因为 README、网页文档、发布说明可能要同步 CLI 入口。  
Agent6：本轮不介入。

### 4.1 事实

Node CLI 有输出：

```powershell
node scripts\elec-cli.mjs build output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\cli-build
```

但发布版 GUI exe 作为命令行入口时，PowerShell 无可见输出：

```powershell
.\dist\GokottaElec.exe output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\dist-exe-build
```

文件生成成功，但终端不显示 `OK`、`IR`、`ERC`、`SVG` 路径。

### 4.2 根因

当前 `GokottaElec.exe` 是 Windows GUI 子系统程序：

```text
Subsystem=2 WINDOWS_GUI
```

命令行分支在 `launcher\Program.cs` 中：

```csharp
return RunCommandLine(repoRoot, cliPath, args);
```

`RunCommandLine` 中没有捕获并重放 Node 子进程输出：

```csharp
psi.RedirectStandardOutput = false;
psi.RedirectStandardError = false;
```

### 4.3 指定给现有职责 Agent 的任务

#### Agent0 执行

Agent0 按既有“封装、发布、合并、版本协调”职责处理。

推荐实现：

1. 保留 `GokottaElec.exe` 为 GUI 程序，不改变双击体验。
2. 新增控制台入口：

```text
GokottaElecCLI.exe
```

3. `GokottaElecCLI.exe` 用 console target 编译，转发到：

```text
node scripts\elec-cli.mjs ...
```

4. `GokottaElecCLI.exe` 必须在 PowerShell 中稳定等待，并显示 stdout/stderr。

不推荐：

```text
不要把主 GokottaElec.exe 改成 console target。
```

#### Agent5 执行同步判断

Agent5 负责判断并同步：

```text
README 命令行示例
WEB_TEAM_HANDOFF.md
WEB_INTEGRATION_REQUIREMENTS.md
GokottaMaker 后端 adapter 调用说明
LLM/Agent 验证命令说明
```

### 4.4 验收命令

```powershell
.\dist\GokottaElecCLI.exe output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\cli-exe-after-fix
```

验收标准：

```text
PowerShell 显示 OK 或 IR/ERC/SVG 路径。
退出码为 0。
输出目录生成 .ir.json、.erc.txt、.svg。
双击 GokottaElec.exe 仍打开 GUI。
双击 GokottaElec.exe 不出现控制台窗口。
```

## 5. 问题 A20-T01-P3：文档引用不存在的验证脚本和 npm script

严重级别：P3  
现有职责归属：Agent3  
同步评估：Agent5 需要知情，因为验证入口会影响网页、LLM handoff、Agent handoff 文档。  
Agent6：本轮不介入。

### 5.1 事实

文档引用了不存在的入口：

```text
docs\erc-rules-v0.1.md: scripts/validate-ir.mjs
docs\component-library-notes-v0.1.md: npm.cmd run validate:models
docs\component-library-notes-v0.1.md: node scripts/validate-model-packages.mjs
docs\component-library-notes-v0.1.md: validate-ir.mjs
```

实际不存在：

```text
scripts\validate-ir.mjs = False
scripts\validate-model-packages.mjs = False
package.json 中不存在 validate:models
```

实际存在：

```text
node scripts\elec-cli.mjs validate <input.ir.json>
node scripts\erc-check.mjs <input.ir.json>
scripts\model-packages.mjs
```

### 5.2 指定给现有职责 Agent 的任务

#### Agent3 执行

Agent3 按既有“ERC/DRC 规则引擎与验证入口”职责处理。

任务要求：

1. 修正 `docs\erc-rules-v0.1.md` 中不存在的 `scripts/validate-ir.mjs`。
2. 统一为真实入口：

```powershell
node scripts\elec-cli.mjs validate <input.ir.json>
```

或：

```powershell
node scripts\erc-check.mjs <input.ir.json>
```

3. 对 model package 验证入口二选一：
   - 补实现：新增 `scripts\validate-model-packages.mjs`，并在 `package.json` 增加 `validate:models`。
   - 改文档：删除 `validate:models` 与 `validate-model-packages.mjs` 引用，说明当前由 `erc-check.mjs` 或 `elec-cli validate` 应用 model/package defaults。
4. 建议增加统一脚本：

```json
"validate:ir": "node scripts/elec-cli.mjs validate"
```

#### Agent2 协同确认

Agent2 不负责验证入口实现，但需要确认：

```text
components\core-components.v0.1.json
components\model-packages.v0.1.json
docs\component-library-notes-v0.1.md
```

中的器件库、封装、PinMap 说明与 Agent3 的真实验证入口一致。

#### Agent5 执行同步判断

Agent5 同步：

```text
LLM handoff 中的验证命令
WEB_TEAM_HANDOFF.md 中的后端验证命令
WEB_INTEGRATION_REQUIREMENTS.md 中的接口说明
```

### 5.3 验收标准

```text
文档中不存在指向缺失脚本的命令。
package.json 中列出的命令都真实存在。
LLM handoff、web handoff、docs 中的验证入口一致。
```

## 6. Agent1 本轮职责落位

本轮三个问题中没有发现 CNL parser 失败。

Agent1 不需要承担主修复，但需要在最终回归时确认：

```text
COMPARATOR_SINGLE 输入仍能从 CNL 正确解析为 IR。
open_drain 参数仍保留在 IR 中。
VIN、VREF、VOUT、VCC、GND 网络不被兼容层错误改名。
```

Agent1 验收输入：

```text
D:\Project\2605-Elec\output\agent20-test-2605-01\comparator-open-drain-pullup.cnl
```

## 7. Agent20 后续测试职责

Agent20 继续保持“测试与问题上报”职责，不修改实现。

Agent20 后续应补测：

```text
1. 比较器 open_drain + pullup 是否不再 fallback。
2. 比较器 open_drain 缺少 pullup 时 ERC 是否能报错或 warning。
3. GokottaElecCLI.exe 是否有可见输出。
4. 文档中的验证命令是否真实可执行。
5. 5 个官方 Sample 是否保持通过。
```

## 8. Agent0 最终合并与回归

Agent0 在各 Agent 完成后执行最终合并。

最终回归命令：

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\final-check
npm.cmd run build:sample1
npm.cmd run build:sample2
npm.cmd run build:sample3
npm.cmd run build:sample4
npm.cmd run build:sample5
```

如果新增 `GokottaElecCLI.exe`，还必须执行：

```powershell
.\dist\GokottaElecCLI.exe output\agent20-test-2605-01\comparator-open-drain-pullup.cnl output\agent20-test-2605-01\cli-exe-final-check
```

最终验收：

```text
比较器 SVG 不再 fallback。
5 个官方 Sample 不 fallback。
ERC 全部符合预期。
CLI 有可见输出。
GUI 双击体验不变。
文档命令真实存在。
release zip 不包含 output/ 临时目录。
```

## 9. 禁止事项

```text
不要重新规划 Agent 职责。
不要改变既有 Agent 分工体系。
不要动用 Agent6，除非用户后续明确授权。
不要把 GokottaElec.exe 主 GUI 改成控制台子系统。
不要只让 ERC 通过就认为 SVG 原理图正确。
不要在文档中写不存在的脚本或 npm command。
不要删除 Agent20 的原始测试输出和 evidence。
```

