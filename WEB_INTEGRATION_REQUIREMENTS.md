# GokottaElec 网页版对接需求

本文档定义 `GokottaMaker` 网站接入 `GokottaElec` 的最小接口契约。更面向网页团队的总览交付文档见 `WEB_TEAM_HANDOFF.md`。

当前 GokottaElec 版本：`V1.2`

目标 GitHub 仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 一期目标

在 `GokottaMaker` 中增加一个 `GokottaElec` 小工具页面：

- 用户可以粘贴 LLM 输出的 CNL 文本。
- 用户可以从官方 Sample 中选择示例。
- 点击“生成预览”后调用后端接口。
- 页面展示返回的 SVG 原理图。
- 页面展示 IR JSON、ERC 和解析日志。
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

## 必须实现的接口

### GET /api/elec/samples

用于返回官方 Sample。

成功响应：

```json
{
  "ok": true,
  "version": "V1.2",
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
  "version": "V1.2",
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
  "version": "V1.2",
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
  "version": "V1.2",
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
- 前端优先读取 `artifacts.svg`、`artifacts.ir`、`artifacts.ercText`。
- 如果没有 `artifacts`，前端回退读取 `circuits[0].svg`、`circuits[0].ir`、`circuits[0].erc`。

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
