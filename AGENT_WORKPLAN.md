# GokottaElec Agent0 统筹与七 Agent 分工

日期：2026-05-15
当前项目版本：`V1.6`
项目目录：`E:\Project\2605_Elec`
仓库：`https://github.com/gokottalin/GokottaElec`

本文档由 Agent0 维护，是后续开发的分工入口。它以用户当前指定的新编制为准：

```text
Agent0  - 项目统筹、分工、合并、发布闸门
Agent1  - CNL parser
Agent2  - 拓展器件库
Agent3  - ERC 规则引擎
Agent4  - 电路图生成器
Agent5  - 软件-网页同步与接口
Agent6  - 美观设计与体验一致性
Agent20 - 测试
```

历史报告中出现的旧职责边界只作为问题背景和证据保留。后续任务分配、验收和冲突裁决均以本文为准。

## 1. Agent0 总控原则

Agent0 不替代各 Agent 的专业职责，主要负责让所有改动能在同一条产品链路中闭环：

```text
CNL 输入
  -> Parser 清洗与拆分
  -> 器件库和端子模型归一
  -> IR JSON
  -> ERC 规则检查
  -> SVG 原理图生成
  -> Web/API/桌面端展示
  -> Agent20 回归测试
```

Agent0 的固定职责：

1. 拆分需求，指定主责 Agent 和协同 Agent。
2. 维护跨 Agent 的文件归属和接口边界。
3. 审查所有跨层改动的同步影响。
4. 统一合并、回归、版本递增、发布说明和 Git 发布节奏。
5. 对 P0/P1 问题拥有最终阻断权。
6. 在职责冲突时给出裁决，不让同一问题在多个 Agent 之间漂移。

Agent0 不直接长期持有核心实现文件。只有在阻断集成、修复小范围冲突或补齐文档时，Agent0 才临时修改实现。

## 2. 总体分工矩阵

| Agent | 主责领域 | 主要写入范围 | 必须交付 |
|---|---|---|---|
| Agent1 | CNL parser | `scripts/parse-cnl.mjs`, `scripts/build-paste.mjs`, CNL 文档和 parser 相关样例 | 语法解析、兼容清洗、多电路拆分、行号诊断 |
| Agent2 | 拓展器件库 | `components/`, `scripts/model-packages.mjs`, `scripts/validate-model-packages.mjs`, 器件库文档 | 器件类型、端子、别名、型号封装、边界条件 |
| Agent3 | ERC 规则引擎 | `scripts/erc-rules.mjs`, `scripts/erc-check.mjs`, ERC 文档 | ERC 规则、严重级别、错误码、诊断输出 |
| Agent4 | 电路图生成器 | `scripts/render-svg.mjs`, SVG 渲染相关文档和样例 | SVG 符号、布局、连线、fallback 控制、图面完整性 |
| Agent5 | 软件-网页同步与接口 | `web-miniapp/`, `WEB_INTEGRATION_REQUIREMENTS.md`, `WEB_TEAM_HANDOFF.md`, Web API adapter 说明 | `/api/elec/*` 契约、网页骨架、桌面/网页同步 |
| Agent6 | 美观设计与体验一致性 | `docs/design-system.md`, `web-miniapp/*.css`, UI 相关资源和桌面 UI 说明 | 视觉规范、交互状态、响应式体验、桌面/网页一致性 |
| Agent20 | 测试 | `AGENT20_*.md`, `output/agent20-test-*` 中的测试产物和脚本 | 问题报告、复测报告、自动化和截图证据 |

共享文件规则：

1. `README.md`, `llm-handoff/`, `llm-interface/`, `samples/` 可能被多个 Agent 影响，但必须由 Agent0 确认最终一致性。
2. `web-miniapp/gokotta-elec.css` 同时涉及 Agent5 和 Agent6。Agent5 只处理功能状态必需样式，Agent6 负责视觉系统和体验细节。
3. `schema/circuit-ir.schema.json` 属于跨层契约文件。修改前必须由 Agent0 指定主责，至少通知 Agent1、Agent2、Agent3、Agent4、Agent5。
4. `package.json` 脚本变动会影响所有 Agent 和 Agent20，必须由 Agent0 合并。

## 3. Agent1 - CNL parser

### 职责

Agent1 负责把受控自然语言电路描述转成稳定、可诊断、可继续处理的 IR 输入。

必须维护：

1. CNL 基础语法和兼容层清洗。
2. 多电路粘贴输入的 circuit block 拆分。
3. 器件、网络、连接、参数、约束的解析。
4. 解析错误的行号、目标对象和可读诊断。
5. 与 `llm-handoff/` 中 CNL 输出契约的一致性。

### 不负责

1. 不定义新器件的端子语义，该工作交给 Agent2。
2. 不决定 ERC 合法性，该工作交给 Agent3。
3. 不决定 SVG 如何画，该工作交给 Agent4。
4. 不调整网页展示，该工作交给 Agent5/Agent6。

### 交付物

每次 parser 改动必须交付：

```text
1. 修改摘要
2. CNL 兼容性说明
3. IR 字段影响
4. 新增或更新的样例
5. 对 Agent2/3/4/5 的同步说明
6. 验收命令和结果
```

### 验收基线

```powershell
node --check scripts\parse-cnl.mjs
node scripts\parse-cnl.mjs samples\Sample-01-voltage-divider.txt output\sample-01.ir.json
node scripts\build-paste.mjs samples\Sample-01-voltage-divider.txt output\agent1-sample-01
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent1-cli-sample-01
```

当 CNL 语法有变化时，Agent1 必须额外更新 `docs/circuit-cnl-v0.1.md` 和 LLM handoff 相关文件。

## 4. Agent2 - 拓展器件库

### 职责

Agent2 负责让系统认识更多器件，并保证器件定义对 parser、ERC、SVG 和 LLM 契约都是稳定的。

必须维护：

1. `components/core-components.v0.1.json` 中的器件类型、端子和基本规则。
2. `components/model-packages.v0.1.json` 中的型号、封装、PinMap 和别名。
3. 器件端子命名规范，例如 `A/B`, `POS/NEG`, `OUT/REF`, `D/G/S`, `IN+/IN-/OUT`。
4. 器件库校验脚本。
5. LLM handoff 中器件库和 PinMap 增强文件。

### 不负责

1. 不把 parser 写成只适配某个器件的特殊分支，必要时交给 Agent1 设计通用规则。
2. 不把 ERC 规则硬编码进器件库，规则由 Agent3 负责。
3. 不直接规定图形布局，渲染由 Agent4 负责。

### 交付物

每次器件库改动必须交付：

```text
1. 新增/修改器件列表
2. 每个器件的端子表
3. 参数字段和单位约定
4. PinMap 或封装影响
5. 对 CNL、ERC、SVG、Web 的影响说明
6. validate:models 结果
```

### 验收基线

```powershell
node --check scripts\validate-model-packages.mjs
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent2-sample-01
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent2-sample-05
```

如果新增器件会进入 SVG，必须通知 Agent4 增加或确认符号绘制；如果新增器件会进入网页 Sample，必须通知 Agent5。

## 5. Agent3 - ERC 规则引擎

### 职责

Agent3 负责电气规则检查，确保 IR 中的电路不是“能解析但明显错误”的假成功。

必须维护：

1. `scripts/erc-rules.mjs` 中的规则定义。
2. `scripts/erc-check.mjs` 中的检查入口和诊断输出。
3. 诊断等级：`INFO`, `WARNING`, `ERROR`。
4. 错误码稳定性和文档化。
5. `ok` 判定和 Web API 响应语义的一致性。

### 不负责

1. 不修改 parser 来规避错误输入。
2. 不修改器件库来掩盖规则缺失。
3. 不把视觉提示写进规则引擎。

### 交付物

每次 ERC 改动必须交付：

```text
1. 新增/修改/删除的规则
2. 触发条件
3. 诊断等级和错误码
4. 对 Web diagnostics 展示的影响
5. 正例和反例测试
6. 对 Agent20 的回归建议
```

### 验收基线

```powershell
node --check scripts\erc-rules.mjs
node --check scripts\erc-check.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent3-sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent3-sample-02
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent3-sample-05
```

如果 ERC 输出字段或错误码变化，Agent3 必须通知 Agent5 更新接口文档，并通知 Agent6 确认错误/警告状态的视觉表达。

## 6. Agent4 - 电路图生成器

### 职责

Agent4 负责从 IR 生成可读、完整、可验证的 SVG 原理图。

必须维护：

1. `scripts/render-svg.mjs` 中的符号绘制和布局逻辑。
2. 器件 refdes、value、关键参数的显示。
3. 网络标签、连线、交叉、端点和接地符号。
4. 专用 renderer 和 fallback renderer 的边界。
5. SVG 产物的可测试性，例如 title、refdes、viewBox、文本不溢出。

### 不负责

1. 不删除 IR 中的器件来让图更容易画。
2. 不改变 ERC 结论。
3. 不调整网页整体界面风格。

### 交付物

每次 SVG 改动必须交付：

```text
1. 影响的 renderer 列表
2. 新增/修改的符号
3. 是否会改变 SVG 结构
4. 是否影响 Web 下载/预览
5. fallback 数量变化
6. 关键截图或 SVG 检查结果
```

### 验收基线

```powershell
node --check scripts\render-svg.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent4-sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent4-sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent4-sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent4-sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent4-sample-05
```

Agent4 修改 renderer 时，必须特别关注 Agent20 历史问题：

```text
1. 已声明器件不能在 SVG 中无说明地消失。
2. 多器件电路不能退回不可读 fallback。
3. SVG 中 refdes、value 和网络标签不能互相覆盖。
```

## 7. Agent5 - 软件-网页同步与接口

### 职责

Agent5 负责 GokottaElec 与网页端、GokottaMaker 集成之间的契约一致性。

必须维护：

1. `web-miniapp/gokotta-elec.html`
2. `web-miniapp/gokotta-elec.js`
3. `web-miniapp/README.md`
4. `WEB_INTEGRATION_REQUIREMENTS.md`
5. `WEB_TEAM_HANDOFF.md`
6. `/api/elec/*` 的请求和响应契约。

### 必须支持的 Web API

```http
GET  /api/elec/samples
POST /api/elec/build
GET  /api/elec/llm-handoff?mode=basic
GET  /api/elec/llm-handoff?mode=full
```

`POST /api/elec/build` 必须以 `circuits[]` 为主数据源，不能只展示第一个电路后丢弃其余结果。

### 不负责

1. 不重新实现 CNL parser、ERC 或 SVG renderer。
2. 不让浏览器直接执行本地脚本。
3. 不决定视觉系统主风格，该工作交给 Agent6。

### 交付物

每次接口或网页同步改动必须交付：

```text
1. API 变更摘要
2. 请求/响应示例
3. 单电路和多电路行为说明
4. 与桌面端功能的同步差异
5. 后端未接入时的降级行为
6. 对 Agent6 的视觉状态需求
```

### 验收基线

```powershell
node --check web-miniapp\gokotta-elec.js
```

网页行为验收至少覆盖：

```text
1. Sample 加载成功和 Sample API 未接入两种状态。
2. 单电路构建成功。
3. 多电路构建成功，且可切换每个 circuits[]。
4. SVG、IR、ERC、下载文件名随当前电路同步。
5. 基础 LLM 和完整 LLM 复制入口可用。
6. 后端错误能显示 diagnostics。
```

## 8. Agent6 - 美观设计与体验一致性

### 职责

Agent6 负责让桌面端和网页端看起来像同一款成熟工具，而不是多个模块拼在一起。

必须维护：

1. `docs/design-system.md`
2. `web-miniapp/gokotta-elec.css`
3. `web-miniapp/assets/gokotta-elec-icon.png`
4. 桌面 UI 相关说明和资源。
5. 交互状态、空状态、错误状态、移动端布局规则。

### 设计基线

当前视觉基线来自 `docs/design-system.md`：

```text
Shell blue:   #EDF7FD
Panel soft:   #F6FBFF
Surface:      #FFFFFF
Border blue:  #B7D8EE
Brand navy:   #0D4885
Brand blue:   #137CD6
Circuit cyan: #4DBEF1
Text blue:    #123657
Muted blue:   #526F85
```

Agent6 必须覆盖的状态：

```text
初始空状态
正在生成
成功预览
解析失败
ERC WARNING
ERC ERROR
后端未接入
多电路结果切换
复制成功/失败
下载成功/失败
移动端窄屏
```

### 不负责

1. 不改变 CNL、IR、ERC 或 SVG 的核心逻辑。
2. 不把视觉样式写进业务判断。
3. 不为了美观隐藏错误信息、器件 refdes 或测试可见标识。

### 交付物

每次 UI/UX 改动必须交付：

```text
1. 改动前后的状态列表
2. 桌面端和移动端影响
3. 是否影响 Agent5 的 DOM 或 API 绑定
4. 可访问性和文本溢出检查
5. 截图或手工验收记录
```

### 验收基线

```text
1. 390x844 移动端无页面级横向溢出。
2. 桌面宽屏下输入、预览、日志和 IR 区域不互相遮挡。
3. 所有按钮文字不溢出。
4. 错误信息可读，不能只靠颜色表达。
5. SVG 预览区域不因装饰影响原理图识别。
```

## 9. Agent20 - 测试

### 职责

Agent20 只做测试、复测和问题上报，不修改实现。

必须维护：

1. `AGENT20_ISSUES_*.md` 问题报告。
2. `AGENT20_*_REPORT.md` 复测和发布验证报告。
3. `output/agent20-test-*` 下的测试输入、mock server、自动化脚本和截图证据。
4. 测试结论中的 P0/P1/P2/P3 分级。

### 测试范围

Agent20 至少覆盖：

```text
1. 官方 5 个 Sample。
2. 多电路一次性粘贴。
3. CNL parser 失败路径。
4. ERC WARNING 和 ERROR。
5. SVG refdes 完整性。
6. Web 单电路和多电路展示。
7. LLM handoff 复制。
8. SVG 下载。
9. 移动端布局。
10. 发布前 smoke test。
```

### 问题报告模板

```text
问题编号：
严重级别：P0/P1/P2/P3
影响模块：
主责建议：
复现步骤：
实际结果：
期望结果：
证据文件：
定位线索：
阻断发布：是/否
```

### 严重级别

```text
P0：数据损坏、安全风险、构建全局不可用，立即阻断。
P1：核心链路错误、用户主流程不可用、发布阻断。
P2：功能缺口或局部体验问题，可进入当前迭代修复。
P3：文案、视觉、轻微体验问题，可排入 polish。
```

## 10. 标准协作流程

### 10.1 需求进入

Agent0 接收需求后必须输出：

```text
任务标题
目标版本
主责 Agent
协同 Agent
影响文件
验收标准
是否阻断发布
```

### 10.2 开发执行

主责 Agent 修改前必须确认：

```text
1. 是否会影响 CNL 输入。
2. 是否会影响 IR JSON。
3. 是否会影响 ERC 输出。
4. 是否会影响 SVG 渲染。
5. 是否会影响 Web API。
6. 是否会影响 UI/UX。
7. 是否需要 Agent20 新增测试。
```

### 10.3 变更同步说明

任何实现 Agent 完成改动后，必须给 Agent0 提交：

```text
变更标题：
主责 Agent：
协同 Agent：
修改文件：
影响层：CNL / 器件库 / ERC / SVG / Web API / UI / Test
兼容性：
新增诊断或错误码：
新增样例或测试：
已执行命令：
需要其他 Agent 跟进：
发布风险：
```

### 10.4 Agent0 合并闸门

Agent0 合并前至少执行：

```powershell
node --check scripts\parse-cnl.mjs
node --check scripts\erc-check.mjs
node --check scripts\render-svg.mjs
node --check scripts\build-paste.mjs
node --check web-miniapp\gokotta-elec.js
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent0-sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent0-sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent0-sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent0-sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent0-sample-05
```

如果本机有 `npm`，也可以执行：

```powershell
npm run validate:models
npm run build:sample1
npm run build:sample2
npm run build:sample3
npm run build:sample4
npm run build:sample5
```

当前工作机环境中 `node` 可用，`npm` 未进入 PATH，因此本文优先使用 `node scripts\*.mjs` 命令。

## 11. 迭代节奏

### 阶段 A：基础稳定

目标：让 CNL -> IR -> ERC -> SVG 的核心链路稳定。

主责顺序：

```text
Agent1 parser
Agent2 器件库
Agent3 ERC
Agent4 SVG
Agent20 核心回归
```

Agent0 在本阶段重点检查 schema、样例、错误码和 renderer 是否互相匹配。

### 阶段 B：网页接入

目标：让 `web-miniapp` 和未来 GokottaMaker API 接入稳定。

主责顺序：

```text
Agent5 Web/API
Agent6 UI 状态
Agent20 Web 自动化
Agent0 集成复核
```

Agent0 在本阶段重点检查 `circuits[]` 多电路响应、LLM handoff、SVG 下载和移动端体验。

### 阶段 C：体验一致性

目标：让桌面端、网页端和文档表现一致。

主责顺序：

```text
Agent6 视觉规范
Agent5 DOM/API 同步
Agent4 SVG 图面可读性
Agent20 截图证据
```

### 阶段 D：发布

目标：形成可发布版本。

发布前条件：

```text
1. 无 P0/P1 未关闭问题。
2. 官方 Sample 全部通过。
3. Agent20 回归报告通过。
4. README、WEB 文档、LLM handoff 与实际行为一致。
5. VERSION 和 package.json 版本一致。
6. Agent0 发布报告完成。
```

## 12. Agent0 并行调度协议

Agent0 后续分工必须优先给出可并行顺序，而不是只列职责。默认输出格式：

```text
1. 让 AgentX 和 AgentY 先并行处理：...
2. 等 AgentX/AgentY 输出后，让 AgentZ 处理：...
3. 让 Agent20 复测：...
4. Agent0 合并和发布判断：...
```

### 12.1 默认并行批次

如果任务尚未证明存在硬依赖，Agent0 优先这样派：

```text
第一批并行：
Agent1 - CNL/parser 影响面分析
Agent2 - 器件库/端子/PinMap 影响面分析
Agent3 - ERC 规则和诊断影响面分析
Agent6 - UI/体验风险分析

第二批并行：
Agent4 - 基于 Agent1/2/3 的稳定 IR 和规则结果处理 SVG
Agent5 - 基于 Agent1/2/3 的稳定 API 字段处理 Web/API

第三批：
Agent20 - 对核心链路、SVG、Web、移动端执行复测
Agent0 - 汇总、裁决、合并、版本判断
```

### 12.2 常见任务的推荐派工顺序

新增器件或端子：

```text
1. Agent2 先处理器件定义，同时 Agent1 评估 CNL 语法是否需要变化。
2. Agent3 根据 Agent2 的端子语义补 ERC。
3. Agent4 补 SVG 符号和布局，同时 Agent5 评估 Web/API 是否要同步。
4. Agent6 检查图面和页面体验。
5. Agent20 复测。
```

新增 CNL 语法：

```text
1. Agent1 先处理 parser，同时 Agent2 判断是否涉及新器件或新端子。
2. Agent3 补规则，Agent4 补渲染，Agent5 补 Web/API 文档。
3. Agent6 做体验检查。
4. Agent20 复测。
```

显示、图面、SVG 问题：

```text
1. Agent4 先处理渲染规则，同时 Agent20 固化复现脚本和视觉检查。
2. Agent6 并行评估视觉规范和可读性。
3. 如果 SVG 结构或输出字段变化，再让 Agent5 同步 Web。
4. Agent0 合并后让 Agent20 复测。
```

Web/API 问题：

```text
1. Agent5 先处理 API 和 web-miniapp，同时 Agent20 固化 Web 自动化复现。
2. Agent6 并行处理交互和移动端体验。
3. 如果 API 字段来自 IR/ERC/SVG，再回传 Agent1/3/4。
4. Agent20 复测。
```

ERC 问题：

```text
1. Agent3 先处理规则，同时 Agent1/Agent2 并行确认输入和器件语义。
2. Agent5 根据诊断字段同步 Web 展示。
3. Agent6 处理 warning/error 状态体验。
4. Agent20 复测。
```

发布前收口：

```text
1. Agent20 先跑完整回归。
2. Agent0 根据 P0/P1/P2/P3 裁决是否允许发布。
3. 如只剩 P3，Agent6/Agent5 可并行 polish；如有 P0/P1，回到主责 Agent 修复。
4. Agent0 最终合并、版本、发布报告。
```

### 12.3 必须串行的情况

以下情况不并行实现，只能按前后置顺序：

```text
1. schema/circuit-ir.schema.json 改动：Agent0 指定主责后，Agent1/2/3/4/5 依次确认。
2. 器件端子语义未定：必须 Agent2 先定，Agent1/3/4/5 再跟。
3. CNL 语法未定：必须 Agent1 先定，Agent2/3/4/5 再跟。
4. ERC 错误码或 ok 语义未定：必须 Agent3 先定，Agent5/6 再展示。
5. SVG 输出结构未定：必须 Agent4 先定，Agent5/6 再接入。
6. 存在 P0/P1：先由主责 Agent 修复，Agent20 复测通过后才进入 polish 或发布。
```

### 12.4 Agent0 每次派工必须回答

```text
1. 哪些 Agent 可以立即并行？
2. 哪些 Agent 必须等待谁的输出？
3. 哪些文件不能同时改？
4. Agent20 需要新增什么复测？
5. 什么条件下 Agent0 才能合并？
```

## 13. 冲突裁决规则

1. Parser 和器件库冲突时，先由 Agent2 定义器件事实，再由 Agent1 调整语法接受方式。
2. 器件库和 ERC 冲突时，先由 Agent2 明确端子/参数含义，再由 Agent3 写规则。
3. ERC 和 Web 冲突时，Agent3 保持诊断语义，Agent5 适配展示。
4. SVG 和 UI 冲突时，Agent4 保证图面信息完整，Agent6 优化容器和体验。
5. Agent20 和实现 Agent 对问题严重级别有分歧时，Agent0 裁决是否阻断发布。
6. 同一文件多 Agent 同时需要修改时，由 Agent0 指定单一主责，其他 Agent 以建议或补丁说明形式参与。

## 14. 禁止事项

```text
1. 禁止通过删除 IR 器件来绕过 SVG 渲染缺陷。
2. 禁止把多电路结果强行截断为第一个电路。
3. 禁止让 Web 前端直接执行用户输入或本地命令。
4. 禁止在 P0/P1 未关闭时发布版本。
5. 禁止未同步文档就改变 CNL、IR、ERC、SVG 或 API 契约。
6. 禁止 Agent20 在测试报告之外直接修改实现。
7. 禁止为了视觉简洁隐藏错误、警告或关键 refdes。
```

## 15. Agent0 当前下一步

当前已完成：

```text
1. 仓库已克隆到 E:\Project\2605_Elec。
2. 本机 Node 环境可运行核心脚本。
3. 官方 5 个 Sample 构建通过。
4. Web 静态预览可通过 http://127.0.0.1:5173/gokotta-elec.html 打开。
5. 七 Agent 新分工已落入本文档。
6. Agent20 V1.5 基线测试已建立，核心链路和 Web 基线均通过。
7. Complex20 图库两个 P2 问题已按规则化方式修复并复测通过。
8. Agent5-Agent6 已完成 web-miniapp 接口状态和移动端体验一致性检查。
```

已确认的当前闸门结果：

```text
1. node --check parser/ERC/SVG/build-paste/Web/Complex20 generator：通过。
2. validate-model-packages：通过。
3. 官方 Sample 01-05：通过。
4. Complex20 visual inspection：passed=true，issueCounts={}。
5. Agent20 V1.5 core baseline：passed=true，20/20。
6. Agent20 V1.5 web baseline：passed=true，移动端无横向溢出。
7. 当前 P0/P1：0。
```

第一轮剩余问题优先级：

```text
P0/P1：无。
P2：无未关闭项；Complex20 的 EXTRA_DECLARED_NET 与 TEXT_OVERLAP 已关闭。
已完成：Agent3 已新增“声明但未连接 net”的 `UNUSED_NET` ERC WARNING，用于把 fixture 类问题前移到核心链路。
P3：Agent6 可继续复核 output\twenty-circuits\gallery.png 的视觉密度、留白和标题层级。
已完成：Agent2 已复核器件库、PinMap 和 component-library-notes 一致性，validate-model-packages 通过。
待同步：Agent5 仅在 twenty-circuits gallery 需要进入网页时补 Web 文档/API 说明。
```

下一步建议：

```text
1. Agent6 对 Complex20 gallery 做最终视觉密度检查。
2. Agent5 如后续将 twenty-circuits gallery 接入网页，再补 Web 文档/API 说明。
3. Agent0 在 P3/backlog 项确认不阻断后执行发布判断。
```

## 16. 2026-05-15 执行状态

按第 15 节“当前下一步”执行后的状态：

```text
1. Agent0 当前版本 baseline/收口报告：已完成，见 AGENT0_V15_FIRST_ROUND_TRIAGE_2605_05.md。
2. Agent20 V1.5 基线测试：已完成，见 AGENT20_V15_BASELINE_2605_03_REPORT.md。
3. Agent1-Agent4 核心链路排查：已完成当前闸门复核，无 P0/P1/P2 未关闭；Agent3 的 P3 增强已实现。
4. Agent5-Agent6 Web/体验检查：已完成当前 Web baseline，多电路、下载、移动端溢出均通过。
5. Agent0 第一轮问题优先级汇总：已完成，当前阻断项为 0。
```

本轮关闭的 Agent20 问题：

```text
A20-C20-P2-01：07-13 电路存在多余声明节点 N_BIAS
A20-C20-P2-02：10、11 电路 VCC 标签重复绘制在同一坐标
```

复测关闭报告：

```text
AGENT20_COMPLEX20_CLOSEOUT_2605_05.md
```

当前非阻断后续建议：

```text
已完成：Agent3 declared-but-unused net 的 ERC WARNING，见 scripts\erc-rules.mjs、scripts\erc-check.mjs、docs\erc-rules-v0.1.md、AGENT3_COMPLEX20_ERC_RULE_REPORT_2605_04.md。
P3-backlog-01：Agent6 可继续抽查 gallery.png 的视觉密度和标题层级。
P3-backlog-02：Agent5 如后续将 twenty-circuits gallery 接入网页，再补 Web 文档说明。
```
