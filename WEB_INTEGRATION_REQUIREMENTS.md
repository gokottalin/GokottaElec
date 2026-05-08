# GokottaElec 网页版对接需求

本文档给 `GokottaMaker` 网站项目使用。目标不是一次做完整网页版，而是先做一个可嵌入网站的小程序入口，预留后续完整云端渲染接口。

当前 GokottaElec 版本：`V1.0`

目标 GitHub 仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 一期目标

在 `GokottaMaker` 中增加一个 `GokottaElec` 小工具页面：

- 用户可以粘贴 LLM 输出的 CNL 文本。
- 点击“生成预览”后调用后端接口。
- 页面展示返回的 SVG 原理图。
- 页面展示 ERC / 解析日志。
- 预留后续保存工程、登录用户工程库、分享链接、导出文件的接口。

## 推荐页面

建议新增：

```text
/tools/gokotta-elec.html
/tools/gokotta-elec.js
/styles/gokotta-elec.css
```

如果网站当前不使用 `/tools/` 目录，也可以放在：

```text
/gokotta-elec.html
/gokotta-elec.js
```

页面必须包含：

- `textarea#cnlInput`：CNL 输入框。
- `button#renderButton`：生成预览。
- `select#sampleSelect`：选择 Sample。
- `div#svgPreview`：SVG 预览容器。
- `pre#diagnosticsLog`：解析、ERC、渲染日志。
- `button#downloadSvgButton`：预留下载 SVG。
- `button#saveProjectButton`：预留保存项目。

## 前端调用接口

一期建议接口：

```http
POST /api/gokotta-elec/render
Content-Type: application/json
```

请求：

```json
{
  "source": "电路 SAMPLE_01 版本 0.1.0。...",
  "mode": "preview",
  "returnSvg": true,
  "returnIr": true
}
```

成功响应：

```json
{
  "ok": true,
  "circuits": [
    {
      "id": "SAMPLE_01",
      "svg": "<svg ...></svg>",
      "ir": {},
      "erc": "OK\n",
      "warnings": []
    }
  ],
  "diagnostics": []
}
```

失败响应：

```json
{
  "ok": false,
  "circuits": [],
  "diagnostics": [
    {
      "level": "ERROR",
      "code": "CNL_PARSE",
      "message": "解析失败说明",
      "line": 12
    }
  ]
}
```

## 后端实现建议

`GokottaMaker` 当前是 Node 服务，建议先在 `server.js` 增加一个 API 分支：

```text
POST /api/gokotta-elec/render
```

后端可以先调用 GokottaElec 的脚本：

```powershell
node scripts/build-paste.mjs input.txt output/web-preview
```

网站项目不建议直接复制桌面 exe。推荐从 GokottaElec 项目复制以下运行核心到网站服务端：

```text
gokotta-elec-core/
  components/
  schema/
  scripts/
  package.json
```

然后 `server.js` 用 `child_process.spawn` 调用：

```text
node gokotta-elec-core/scripts/build-paste.mjs <temp-input.txt> <temp-output-dir>
```

接口返回时读取每个输出目录中的：

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
- SVG 返回前可先只作为文本插入安全容器，后续再做 SVG 清洗。

## 预留接口

后续可增加：

```http
GET  /api/gokotta-elec/samples
POST /api/gokotta-elec/render
POST /api/gokotta-elec/projects
GET  /api/gokotta-elec/projects/:id
GET  /api/gokotta-elec/projects/:id/export
```

## 前端视觉方向

页面应保持 GokottaMaker 的技术网站风格，但工具区要更像工程软件：

- 左侧：CNL 输入。
- 右侧：原理图预览。
- 底部：日志、ERC、导出按钮。
- 主色建议沿用 GokottaElec 淡蓝色系。
- SVG 预览区域使用白色画布、浅灰边框、可滚动、可缩放。

## GokottaElec 当前可提供的核心能力

- CNL `.txt` 输入。
- 多电路拆分。
- CNL 兼容层清洗。
- CNL 转 IR。
- ERC 检查。
- SVG 原理图生成。
- LLM 对接文件在 `llm-handoff/` 和 `llm-interface/`。

## 当前不要求网站一期实现

- 用户账户工程库。
- 在线编辑器完整历史版本。
- 电路图库管理。
- 仿真。
- SPICE 导出。
- PCB 导出。
- 完整移动端复杂交互。
