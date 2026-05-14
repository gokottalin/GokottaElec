# Agent0 Complex20 修复与分工记录 2605-04

日期：2026-05-14  
执行角色：Agent0  
输入报告：

```text
AGENT20_V15_BASELINE_2605_03_REPORT.md
AGENT20_COMPLEX20_VISUAL_REPORT.md
AGENT20_ISSUES_2605_COMPLEX20.md
```

## 1. 结论

根据 Agent20 最新测试结果，本轮发现 2 类 P2 问题。Agent0 已先完成可直接修复的实现工作，并完成复测：

```text
A20-C20-P2-01：07-13 电路存在多余声明节点 N_BIAS -> 已修复
A20-C20-P2-02：10、11 电路 VCC 标签重复绘制       -> 已修复
```

复测结果：

```text
complex-20 visual inspection passed=true
issueCounts={}
20/20 PNG 非空且尺寸正确
20/20 SVG/IR 视觉与节点检查通过
官方 5 个 Sample 构建通过
validate-model-packages 通过
```

## 2. Agent0 已完成工作

### 2.1 修复多余 `N_BIAS`

修改文件：

```text
scripts/generate-20-circuit-gallery.mjs
```

修复内容：

```text
commonEmitterNpn
commonEmitterPnp
emitterFollowerNpn
```

上述 3 个 fixture 生成函数曾在 `nets[]` 中声明 `N_BIAS`，但所有连接实际使用的是 `N_IN`。本轮删除未连接的 `N_BIAS` 声明，避免 IR 中出现声明但未使用的节点。

### 2.2 修复 PNP 共射重复 `VCC`

修改文件：

```text
scripts/render-svg.mjs
```

修复内容：

PNP 共射渲染中，rail 起点已经调用过一次 `powerPort(powerNet, qX + 24, railY)`。在 PNP emitter 分支中再次用同一坐标调用 `powerPort(powerNet, emitterNode.x, railY)` 会产生完全重叠的 `VCC` 标签。本轮保留 rail 起点标签，删除重复标签，只保留 junction。

## 3. 已执行验证

语法检查：

```powershell
node --check scripts\generate-20-circuit-gallery.mjs
node --check scripts\render-svg.mjs
node --check scripts\parse-cnl.mjs
node --check web-miniapp\gokotta-elec.js
```

20 复杂电路重新生成：

```powershell
node scripts\generate-20-circuit-gallery.mjs
```

Agent20 视觉检查复测：

```powershell
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

结果：

```text
testId=agent20-complex-20-2605-04
circuitCount=20
imageCount=20
passed=true
issueCounts={}
```

核心回归：

```powershell
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent0-after-complex20\sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent0-after-complex20\sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent0-after-complex20\sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent0-after-complex20\sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent0-after-complex20\sample-05
```

结果：

```text
OK: components\model-packages.v0.1.json
Sample 01-05 全部 OK
```

备注：

```text
output\agent20-test-2605-01\comparator-open-drain-pullup.cnl 当前工作区不存在，因此 Test01 比较器输入未在本轮直接复跑。
```

## 4. 2026-05-15 规则化升级

用户要求：复测出来的所有显示问题，尤其是电路生成问题，必须通过规则解决，不能靠手动单点修补。

Agent0 已将本轮两个问题升级为通用规则：

### 4.1 生成阶段规则

修改文件：

```text
scripts\generate-20-circuit-gallery.mjs
```

新增规则入口：

```text
enforceGalleryIrRules(irData)
```

规则内容：

```text
1. net id 不允许重复。
2. refdes 不允许重复。
3. connections[] 引用的 net 必须已经在 nets[] 中声明。
4. nets[] 中声明的 net 必须至少被 connections[] 使用一次。
```

作用：

```text
以后不只是 N_BIAS，任何未使用声明节点都会在生成 20 电路图库时直接失败。
```

### 4.2 渲染阶段规则

修改文件：

```text
scripts\render-svg.mjs
```

新增规则入口：

```text
claimPort(...)
netLabel(...)
```

规则内容：

```text
1. powerPort / negativePort / groundPort 对同一 kind + net + 坐标只绘制一次。
2. netLabel 对同一 net + 坐标 + anchor + class 只绘制一次。
```

作用：

```text
以后不只是 VCC，任何同坐标重复电源、地、负电源或 net 标签都会被 helper 层去重。
```

### 4.3 规则化复测结果

执行命令：

```powershell
node --check scripts\generate-20-circuit-gallery.mjs
node --check scripts\render-svg.mjs
node scripts\generate-20-circuit-gallery.mjs
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

结果：

```text
passed=true
issueCounts={}
```

V1.5 基线复测：

```powershell
node output\agent20-v15-baseline-2605-03\run-core-baseline.mjs
node output\agent20-v15-baseline-2605-03\run-web-baseline.mjs
```

结果：

```text
core baseline passed=true
web baseline passed=true
fallbackCount=0
missingRefdesCount=0
Web multi optionCount=20
mobile horizontalOverflow=false
```

## 5. 后续分工

### Agent1 - CNL parser

当前无阻断任务。后续如 Complex20 gallery 要导出 CNL 输入，Agent1 负责确认：

```text
1. fixture 中的 N_IN / VIN 命名能稳定进入 IR。
2. 多电路粘贴后不会重新生成未连接 net。
3. parser 诊断中能区分“声明未使用 net”和“连接引用未知 net”。
```

### Agent2 - 拓展器件库

当前 `validate-model-packages` 通过。Agent2 后续负责复核当前未提交的器件库扩展：

```text
1. components\model-packages.v0.1.json
2. llm-handoff\12_可选增强_型号封装引脚库_PinMap.json
3. docs\component-library-notes-v0.1.md
```

验收重点：

```text
端子命名、PinMap、别名和 LLM handoff 内容必须一致。
```

### Agent3 - ERC 规则引擎

当前无代码改动。Agent3 后续可补一个 P3 级规则建议：

```text
对 IR 中 declared nets 未被 connections 使用的情况输出 WARNING。
```

这可以把本轮 `N_BIAS` fixture 问题提前暴露到核心链路，而不是只由 Agent20 的视觉检查发现。

### Agent4 - 电路图生成器

本轮 `VCC` 重叠已修复。Agent4 后续负责做一个更通用的图面防重机制：

```text
1. 评估 powerPort / groundPort / netLabel 是否需要同坐标同文本去重。
2. 保持当前 SVG 可测试文本不被隐藏。
3. 不影响多处不同位置的同名电源标签。
```

### Agent5 - 软件-网页同步与接口

当前 Complex20 修复不改变 Web API。Agent5 后续负责确认：

```text
1. 如果 twenty-circuits gallery 要进入网页，Web 只读取最新生成的 SVG/PNG。
2. 多电路 circuits[] 仍以完整数组展示，不退回单电路。
3. Web 文档中不需要新增 API 字段。
```

### Agent6 - 美观设计与体验一致性

Agent6 后续负责复核 `output\twenty-circuits\gallery.png` 和单张 PNG：

```text
1. 20 张图在画布内视觉密度是否一致。
2. 电源、地、输入、输出标签位置是否符合设计系统。
3. 是否需要为复杂图统一留白和标题层级。
```

### Agent20 - 测试

Agent20 后续复测任务：

```powershell
node scripts\generate-20-circuit-gallery.mjs
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

验收标准：

```text
passed=true
issueCounts={}
EXTRA_DECLARED_NET=0
TEXT_OVERLAP=0
```

Agent20 还应新增一份复测关闭报告，不直接覆盖原始问题报告。

## 6. Agent0 合并提醒

当前工作区除本轮修复外，还有其他未提交改动。Agent0 后续合并时必须区分来源，避免覆盖其他 Agent 已完成工作：

```text
AGENT_WORKPLAN.md
components/model-packages.v0.1.json
docs/component-library-notes-v0.1.md
docs/design-system.md
llm-handoff/*PinMap.json
scripts/parse-cnl.mjs
web-miniapp/*
```

本轮 Agent0 直接改动的实现文件为：

```text
scripts/generate-20-circuit-gallery.mjs
scripts/render-svg.mjs
```
