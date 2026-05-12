# GokottaElec 网页版对接需求

本文档定义 `GokottaMaker` 网站接入 `GokottaElec` 的最小接口契约。更面向网页团队的总览交付文档见 `WEB_TEAM_HANDOFF.md`。

当前 GokottaElec 版本：`V1.5`

目标 GitHub 仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 一期目标

在 `GokottaMaker` 中增加一个 `GokottaElec` 小工具页面：

- 用户可以粘贴 LLM 输出的 CNL 文本。
- 用户可以从官方 Sample 中选择示例。
- 用户可以复制基础或完整 LLM 对接 Markdown。
- 点击“生成预览”后调用后端接口。
- 页面展示返回的 SVG 原理图。
- 页面展示 IR JSON、ERC 和解析日志。
- 当一次构建返回多个电路时，页面必须允许用户切换每个电路结果。
- 预留后续保存工程、分享链接、导出文件的入口。

## 推荐页面文件

建议在 `GokottaMaker` 中新增：

```text
tools/gokotta-elec.html
tools/gokotta-elec.js
styles/gokotta-elec.css
tools/assets/gokotta-elec-icon.png
```

当前可复制的骨架位于：

```text
web-miniapp/gokotta-elec.html
web-miniapp/gokotta-elec.js
web-miniapp/gokotta-elec.css
web-miniapp/assets/gokotta-elec-icon.png
```

页面主要 DOM：

- `textarea#cnlInput`：CNL 输入框。
- `button#renderButton`：生成预览。
- `select#sampleSelect`：选择 Sample。
- `div#svgPreview`：SVG 预览容器。
- `pre#diagnosticsLog`：解析、ERC、渲染日志。
- `pre#irViewer`：IR JSON 展示。
- `button#downloadSvgButton`：下载 SVG。
- `button#copyIrButton`：复制 IR。
- `button#copyBasicHandoffButton`：复制基础 LLM 对接 Markdown。
- `button#copyFullHandoffButton`：复制完整 LLM 对接 Markdown。
- `div#circuitResultBar`：多电路结果选择区，仅在 `circuits.length > 1` 时显示。
- `select#circuitSelect`：选择 `/api/elec/build` 返回的某一个电路。
- `span#circuitSummary`：显示当前序号和总数，例如 `20 / 20`。
- `div#sampleTitle`：显示当前 Sample 完整标题，避免长标题在下拉框中被截断。

## 必须实现的接口

### GET /api/elec/samples

用于返回官方 Sample。

成功响应：

```json
{
  "ok": true,
  "version": "V1.5",
  "samples": [
    {
      "id": "sample-01-voltage-divider",
      "title": "Sample 01 - 电阻分压",
      "source": "电路 SAMPLE_01 版本 0.1.0。..."
    }
  ]
}
```

失败响应：

```json
{
  "ok": false,
  "version": "V1.5",
  "samples": [],
  "diagnostics": [
    {
      "level": "ERROR",
      "code": "SAMPLES_UNAVAILABLE",
      "message": "Sample 文件不可用。"
    }
  ]
}
```

### POST /api/elec/build

用于接收 CNL 文本，返回 SVG、IR 和 ERC 诊断。

请求：

```json
{
  "source": "电路 WEB_SAMPLE_01 版本 0.1.0。...",
  "inputType": "cnl",
  "options": {
    "runErc": true,
    "renderSvg": true,
    "allowWarnings": true
  }
}
```

成功响应：

```json
{
  "ok": true,
  "version": "V1.5",
  "circuits": [
    {
      "id": "WEB_SAMPLE_01",
      "ok": true,
      "svg": "<svg ...></svg>",
      "ir": {},
      "erc": "OK\n",
      "warnings": []
    }
  ],
  "artifacts": {
    "svg": "<svg ...></svg>",
    "ir": {},
    "ercText": "OK\n"
  },
  "diagnostics": []
}
```

失败响应：

```json
{
  "ok": false,
  "version": "V1.5",
  "circuits": [],
  "artifacts": {},
  "diagnostics": [
    {
      "level": "ERROR",
      "code": "CNL_PARSE",
      "message": "解析失败说明",
      "line": 12,
      "target": ""
    }
  ]
}
```

兼容要求：

- `diagnostics` 必须始终是数组。
- `level` 使用 `INFO`、`WARNING` 或 `ERROR`。
- `ok: true` 可以包含 `WARNING`。
- `ERROR` 应使顶层 `ok` 为 `false`。
- 前端必须保留完整 `circuits[]`，不能只展示 `circuits[0]` 后丢弃其他电路。
- 当前推荐前端以 `circuits[]` 为主数据源。
- `artifacts.svg`、`artifacts.ir`、`artifacts.ercText` 只作为旧式单电路响应的兼容回退。
- 当 `circuits.length === 1` 时，可以隐藏结果选择区，保持单电路体验。
- 当 `circuits.length > 1` 时，必须显示结果选择区；切换时 SVG、IR JSON、ERC/diagnostics、下载 SVG 内容和文件名必须同步切换。
- 推荐下载文件名使用当前电路 ID，例如 `AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2.svg`。

### GET /api/elec/llm-handoff

用于同步桌面端 V1.5 的 `复制基础LLM` 与 `复制完整LLM` 功能，返回可直接粘贴给其他 LLM 的 Markdown 对接包。

查询参数：

```text
mode=basic | full
```

成功响应：

```json
{
  "ok": true,
  "version": "V1.5",
  "mode": "basic",
  "markdown": "# GokottaElec LLM 基础对接包\n..."
}
```

也可以返回 `text/markdown; charset=utf-8`，前端会把响应正文当作 Markdown 复制。

失败响应：

```json
{
  "ok": false,
  "version": "V1.5",
  "diagnostics": [
    {
      "level": "ERROR",
      "code": "LLM_HANDOFF_UNAVAILABLE",
      "message": "LLM 对接文件不可用。"
    }
  ]
}
```

`mode=basic` 至少拼接：

```text
llm-handoff/README_先读_给其他LLM的文件说明.md
llm-handoff/01_必需_系统提示词_直接复制给LLM.txt
llm-handoff/02_必需_CNL输出契约_必须遵守.md
llm-handoff/03_可选增强_输出模板_让LLM套用.txt
```

`mode=full` 在 basic 基础上继续拼接：

```text
llm-handoff/11_可选增强_完整器件库_端子和边界条件.json
llm-handoff/12_可选增强_型号封装引脚库_PinMap.json
schema/circuit-ir.schema.json
docs/circuit-cnl-v0.1.md
docs/llm-cnl-contract-v0.1.md
docs/erc-rules-v0.1.md
docs/component-library-notes-v0.1.md
samples/Sample-01-voltage-divider.txt
samples/Sample-02-npn-low-side-switch.txt
samples/Sample-03-pnp-high-side-switch.txt
samples/Sample-04-cmos-inverter-nmos-pmos.txt
samples/Sample-05-opamp-noninverting-amplifier.txt
```

## 后端实现建议

网站项目不建议直接复制桌面 exe。推荐从 GokottaElec 项目复制运行核心到网站服务端：

```text
gokotta-elec-core/
  components/
  schema/
  scripts/
  package.json
```

`server.js` 可用 `child_process.spawn` 调用：

```text
node gokotta-elec-core/scripts/build-paste.mjs <temp-input.txt> <temp-output-dir>
```

接口返回时读取输出目录中的：

- `*.svg`
- `*.ir.json`
- `*.erc.txt`
- `summary.txt`

## 安全限制

必须限制：

- 单次输入文本大小：建议 200 KB 以内。
- 单次最多电路数量：建议 10 个以内。
- 临时输出目录必须在服务端受控目录内。
- 不允许用户传入任意输出路径。
- 不允许接口直接执行用户提供的命令。
- 不要拼接 shell 命令字符串，使用参数数组调用子进程。
- SVG 返回前至少检查产物类型，后续再做 SVG 清洗。

## 后续预留接口

后续可增加：

```http
POST /api/elec/validate
POST /api/elec/render
GET  /api/elec/jobs/:jobId
GET  /api/elec/jobs/:jobId/files/:kind
```

## 当前不要求网站一期实现

- 用户账户工程库。
- 在线编辑器完整历史版本。
- 电路图库管理。
- 仿真。
- SPICE 导出。
- PCB 导出。
- 完整移动端复杂交互。
