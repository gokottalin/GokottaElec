# GokottaElec 后续 Agent 分工规划

本文档修订原则：**不重排、不覆盖原有 5 个 Agent 的固定分工**。原 Agent0-Agent4 已经存在并有既定职责，后续继续按原职责执行。本文只在原体系上新增两个 Agent，并规定桌面软件与网页版之间的同步机制。

## 总原则

1. 原 Agent0-Agent4 的编号、名称、职责边界不变。
2. 本轮只新增两个 Agent：Agent5 与 Agent6。
3. GokottaElec 不是一次性发布物，后续仍会持续修改桌面软件功能。
4. 任何桌面软件、CNL 格式、渲染器、ERC、Sample、LLM 对接格式的变化，都必须同步评估网页端影响。
5. 网页版发布在 `GokottaMaker`，但核心能力仍以 GokottaElec 的 CNL/IR/ERC/SVG 引擎为准。
6. 当前软件版本为 `V1.4`；普通更新增加 `0.1`，重大更新增加 `1.0`。
7. 目标 GitHub 仓库为 `https://github.com/gokottalin/GokottaElec.git`。

## 原有 Agent0-Agent4

原有 5 个 Agent 按你已经确定的固定分工继续工作。本文不重新指定它们的职责，避免破坏既有协作结构。

它们后续只需要额外遵守一条同步规则：

> 当任一原有 Agent 修改 GokottaElec 的功能、格式、接口、样例、渲染行为或校验规则时，必须输出一份“变更同步说明”，交给 Agent5 判断网页端是否需要同步。

变更同步说明至少包含：

```text
变更标题：
影响模块：
是否影响 CNL 输入：
是否影响 IR JSON：
是否影响 ERC 输出：
是否影响 SVG 渲染：
是否影响 Sample：
是否影响 LLM 对接文件：
是否影响网页版 API：
建议同步动作：
```

## 新增 Agent5：软件-网页同步与接口 Agent

Agent5 是新增的第一个 Agent。

### 核心职责

Agent5 专门负责 GokottaElec 与 GokottaMaker 之间的同步，不替代原有 Agent 的软件开发职责。

职责包括：

- 维护桌面端与网页端共用的接口契约。
- 维护 `WEB_INTEGRATION_REQUIREMENTS.md`。
- 判断每次软件变更是否需要同步到网页端。
- 维护 `/api/elec/*` 的请求/响应格式。
- 维护网页小程序与后端 adapter 的对接说明。
- 保证桌面端 Sample 与网页端 Sample 来源一致。
- 保证 LLM 对接文件变更后，网页端提示词/模板也同步。
- 与 GokottaMaker 项目对接，但不越权重构 GokottaMaker 全站。

### 写入范围

在 `D:\Project\2605-Elec` 中：

```text
WEB_INTEGRATION_REQUIREMENTS.md
web-miniapp/
README.md 中的 Web 对接部分
llm-handoff/
llm-interface/
samples/
```

在 `D:\Project\26-WEB\GokottaMaker` 中，后续允许它负责或协助：

```text
tools/gokotta-elec.html
tools/gokotta-elec.js
styles/gokotta-elec.css
lib/elec-adapter.js
server.js 中 /api/elec/* 路由
```

### 必须维护的最小接口

```http
GET  /api/elec/samples
POST /api/elec/build
```

后续可扩展：

```http
POST /api/elec/validate
POST /api/elec/render
GET  /api/elec/jobs/:jobId
GET  /api/elec/jobs/:jobId/files/:kind
```

### Agent5 的同步判定规则

如果出现以下任一情况，Agent5 必须介入：

- 新增、删除或修改器件类型。
- 修改器件端子命名，例如 BJT、MOS、运放端子。
- 修改 CNL 语法。
- 修改兼容层清洗规则。
- 修改 ERC 规则或错误码。
- 修改 SVG 输出结构。
- 新增桌面端功能，例如导出、缩放、批量生成、错误定位。
- 修改 Sample。
- 修改 LLM 输出契约。

Agent5 的输出必须说明：

```text
网页端是否需要同步：是/否
需要同步的文件：
需要同步的接口：
是否需要更新 Sample：
是否需要更新 LLM 对接文件：
是否需要通知 Agent6 调整界面：
```

## 新增 Agent6：美观设计与体验一致性 Agent

Agent6 是新增的第二个 Agent，也是你要求的“专门做软件美观设计”的 Agent。

### 核心职责

Agent6 负责 GokottaElec 的视觉设计、交互体验和桌面端/网页端视觉一致性。

职责包括：

- 维护 GokottaElec 淡蓝色系品牌视觉。
- 优化桌面软件 WinForms 界面布局、按钮、状态、空状态、日志区域。
- 优化网页小程序视觉，但不负责后端接口实现。
- 建立桌面端与网页端共同的设计规范。
- 确保软件后续新增功能时，桌面端和网页端都有一致的交互表达。
- 输出界面改动建议，不直接改变核心 CNL/IR/ERC 逻辑。

### 写入范围

在 `D:\Project\2605-Elec` 中：

```text
launcher/Program.cs 中 UI 相关部分
launcher/GokottaElec.ico
web-miniapp/
docs/design-system.md
README.md 中界面说明部分
```

在 `D:\Project\26-WEB\GokottaMaker` 中，后续允许它负责或协助：

```text
tools/gokotta-elec.html
tools/gokotta-elec.js 中交互状态
styles/gokotta-elec.css
网站导航入口的视觉接入
```

### Agent6 必须关注的状态

- 初始空状态。
- 正在生成。
- 成功预览。
- 解析失败。
- ERC 警告。
- ERC 错误。
- 多电路选择。
- SVG 预览缩放。
- 下载/复制成功。
- 后端接口未接入。

## 软件变更同步流程

任何原有 Agent 或新增 Agent 修改 GokottaElec 功能时，必须按下面流程走：

1. 功能实现 Agent 完成桌面端或核心引擎变更。
2. 该 Agent 输出“变更同步说明”。
3. Agent5 判断网页端是否需要同步。
4. 如果需要界面调整，Agent5 通知 Agent6。
5. Agent6 给出桌面端/网页端视觉与交互方案。
6. Agent5 更新 Web 接口文档或网页小程序骨架。
7. 原 QA/验证 Agent 按既有职责验证桌面端与网页端。
8. Agent0 或既有总控 Agent 合并最终结果。

## 发布与 Git 规则

Git 仓库应纳入：

```text
components/
docs/
launcher/
llm-handoff/
llm-interface/
samples/
schema/
scripts/
web-miniapp/
README.md
WEB_INTEGRATION_REQUIREMENTS.md
AGENT_WORKPLAN.md
package.json
.gitignore
.gitattributes
```

Git 仓库不建议纳入：

```text
output/
dist/
release/
node_modules/
launcher/bin/
launcher/obj/
```

`dist/GokottaElec.exe` 和 `release/GokottaElec.zip` 更适合通过 GitHub Releases 发布。

版本规则见：

```text
RELEASE_POLICY.md
```

Git 上传步骤见：

```text
GIT_PUBLISH_GUIDE.md
```

## 给 GokottaMaker 项目的当前需求

GokottaMaker 不需要一次做完整网页版。当前只需要预留一个小程序入口，并实现最小 API：

```http
GET  /api/elec/samples
POST /api/elec/build
```

前端小程序骨架位于：

```text
web-miniapp/
```

详细接口需求位于：

```text
WEB_INTEGRATION_REQUIREMENTS.md
```

后续桌面软件功能变化时，由 Agent5 判断网页端同步范围，由 Agent6 统一界面体验。
