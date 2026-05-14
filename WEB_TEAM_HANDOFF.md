# GokottaElec 网页开发团队对接说明

本文档给 `GokottaMaker` 网页开发团队使用，目标是把 `GokottaElec` 作为一个网页小工具接入网站。当前不要求一次性做完整云端 EDA，只需要实现 Sample 加载、CNL 提交、SVG 预览、IR/ ERC 诊断展示这条最小链路。

当前 GokottaElec 版本：`V1.6`

项目仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 1. 项目定位

`GokottaElec` 的核心能力是把 LLM 输出的受控自然语言电路描述，也就是 CNL，转换为：

- 清洗后的 `.cnl`
- 规范化 `.ir.json`
- ERC 检查报告 `.erc.txt`
- SVG 原理图 `.svg`

网页端不要重新实现 CNL 解析、ERC 或 SVG 渲染。网页端只负责：

- 提供 CNL 输入界面。
- 调用后端 `/api/elec/*`。
- 展示后端返回的 SVG、IR JSON 和 diagnostics。
- 提供与桌面端一致的基础/完整 LLM 对接 Markdown 复制入口。
- 对 `/api/elec/build` 返回的多个 `circuits[]` 提供结果切换，不吞掉第 2 个之后的电路。
- 保持 Sample、接口字段和 LLM 输出契约与本仓库同步。

## 2. 当前文件分布

`D:\Project\2605-Elec` 中与网页接入直接相关的文件：

```text
WEB_TEAM_HANDOFF.md                 本文档，给网页开发团队看的总说明
WEB_INTEGRATION_REQUIREMENTS.md     API 契约详细说明
web-miniapp/                        可复制到 GokottaMaker 的前端骨架
samples/                            官方 Sample 来源
scripts/build-paste.mjs             推荐后端调用的批量构建入口
scripts/elec-cli.mjs                CLI 入口
components/                         器件库
schema/                             IR Schema
llm-handoff/                        给其他 LLM 的完整对接文件
llm-interface/                      给其他 LLM 的精简对接入口
docs/                               CNL、ERC、设计规范等说明
```

建议复制到 `D:\Project\26-WEB\GokottaMaker` 的前端文件：

```text
tools/gokotta-elec.html
tools/gokotta-elec.js
styles/gokotta-elec.css
tools/assets/gokotta-elec-icon.png
```

这些源文件位于：

```text
web-miniapp/gokotta-elec.html
web-miniapp/gokotta-elec.js
web-miniapp/gokotta-elec.css
web-miniapp/assets/gokotta-elec-icon.png
```

## 3. 页面风格与交互要求

网页工具页应沿用 GokottaMaker 的站点风格，但工具主体要像工程软件，避免做成营销页。

推荐布局：

- 左侧：CNL 输入区和 Sample 选择。
- 右侧：SVG 原理图预览。
- 底部：诊断日志和 IR JSON。
- 主色：沿用 GokottaElec 淡蓝色系。
- SVG 区域：白色画布、浅灰边框、可滚动，后续可扩展缩放。

页面需要覆盖以下状态：

- 初始空状态。
- Sample API 未接入。
- 正在生成。
- 成功预览。
- 解析失败。
- ERC WARNING。
- ERC ERROR。
- 后端接口未接入。
- 下载/复制成功。

当前 `web-miniapp/gokotta-elec.js` 已默认调用：

```http
GET  /api/elec/samples
POST /api/elec/build
GET  /api/elec/llm-handoff?mode=basic
GET  /api/elec/llm-handoff?mode=full
```

如果后端未接入，页面会自动显示 fallback Sample 和错误诊断。

## 4. 必须实现的最小 API

### 4.1 获取 Sample

```http
GET /api/elec/samples
```

成功响应：

```json
{
  "ok": true,
  "version": "V1.6",
  "samples": [
    {
      "id": "sample-01-voltage-divider",
      "title": "Sample 01 - 电阻分压",
      "source": "电路 SAMPLE_01 版本 0.1.0。..."
    }
  ]
}
```

字段说明：

- `samples[].id`：稳定 ID，建议由文件名转小写短横线得到。
- `samples[].title`：前端下拉框显示名。
- `samples[].source`：完整 CNL 文本，必须来自本仓库 `samples/`。

### 4.2 构建电路

```http
POST /api/elec/build
Content-Type: application/json
```

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
  "version": "V1.6",
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
  "version": "V1.6",
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

- 前端以 `circuits[]` 为主数据源，并保留完整数组。
- `artifacts.svg`、`artifacts.ir`、`artifacts.ercText` 只作为旧式单电路响应的兼容回退。
- `diagnostics` 应始终是数组，成功时可以为空数组。
- `diagnostics[].target` 可以指向 circuit、net、device 或 terminal。单电路响应可以把 net 级 WARNING 放在顶层 `diagnostics`；多电路响应建议把诊断放入对应 `circuits[].diagnostics` / `circuits[].warnings`，或在顶层用 circuit id 作为 `target`。
- ERC 警告可以返回 `ok: true`，但 `diagnostics[].level` 应为 `WARNING`。
- 解析失败、ERC 错误或渲染失败应返回 `ok: false`。

多电路要求：

- 前端必须保存完整 `circuits[]`。
- `circuits.length > 1` 时必须显示结果选择入口。
- 切换结果时，SVG、IR JSON、ERC/diagnostics、下载 SVG 内容和文件名必须同步切换。
- Sample 标题较长时，应在下拉框外显示完整标题或提供等价的可识别文本。

### 4.3 复制 LLM 对接包

```http
GET /api/elec/llm-handoff?mode=basic
GET /api/elec/llm-handoff?mode=full
```

该接口同步桌面端 V1.6 的 `复制基础LLM` 与 `复制完整LLM` 按钮。后端读取本仓库中的 LLM 契约、器件库、Schema、文档和 Sample，拼接为 Markdown 返回。

推荐 JSON 响应：

```json
{
  "ok": true,
  "version": "V1.6",
  "mode": "full",
  "markdown": "# GokottaElec LLM 完整对接包\n..."
}
```

也可以返回 `text/markdown; charset=utf-8`。前端会直接复制响应正文。

## 5. 后端接入建议

推荐在 `GokottaMaker` 后端放置一个受控的 GokottaElec 核心目录，例如：

```text
gokotta-elec-core/
  components/
  schema/
  scripts/
  package.json
```

后端接到 `POST /api/elec/build` 后：

1. 校验 `source` 是字符串，大小不超过限制。
2. 写入服务端临时输入文件，不能使用用户传入的路径。
3. 创建服务端受控临时输出目录。
4. 调用：

```powershell
node gokotta-elec-core/scripts/build-paste.mjs <temp-input.txt> <temp-output-dir>
```

5. 读取输出目录中的 `.svg`、`.ir.json`、`.erc.txt` 和 `summary.txt`。
6. 组装为 `/api/elec/build` 的 JSON 响应。
7. 请求结束后按策略清理临时目录。

不要在网页端直接执行脚本，也不要允许用户指定任意输入/输出路径。

## 6. 安全与限制

建议一期限制：

- 单次输入文本最大 `200 KB`。
- 单次最多处理 `10` 个电路块。
- 单请求超时建议 `15s` 到 `30s`。
- 临时目录必须位于服务端受控目录下。
- 禁止把用户输入拼接成 shell 命令字符串。
- 使用 `child_process.spawn` 或等价 API，并用参数数组传参。
- SVG 返回前至少确认内容以 `<svg` 开始；后续可增加 SVG 清洗。
- API 只返回文本产物，不返回任意文件路径给前端读取。

## 7. CNL 和 LLM 契约

网页团队不需要维护 CNL 语法本身，但需要知道输入来源：

- 用户可以手动粘贴 CNL。
- Sample 来自 `samples/`。
- LLM 输出契约来自 `llm-handoff/` 和 `llm-interface/`。
- V1.6 的网页端 LLM 复制功能应与桌面端 `复制基础LLM`、`复制完整LLM` 的文件集合保持一致。

最少给其他 LLM 的文件：

```text
llm-handoff/01_必需_系统提示词_直接复制给LLM.txt
llm-handoff/02_必需_CNL输出契约_必须遵守.md
llm-handoff/03_可选增强_输出模板_让LLM套用.txt
```

如果这些文件变化，网页端提示词、示例说明和 Sample 加载逻辑都需要重新评估。

## 8. 当前不要求实现

一期不用实现：

- 用户账户工程库。
- 保存工程到数据库。
- 分享链接。
- SPICE 导出。
- PCB 导出。
- 在线仿真。
- 完整器件图库管理。
- 移动端复杂编辑器。

但 UI 可以保留“保存”“导出”等禁用或预留入口。

## 9. 与 Agent5 同步规则

网页团队后续收到以下变更时，需要通知 Agent5 判断同步范围：

- CNL 语法变化。
- IR JSON 字段变化。
- ERC 错误码或输出格式变化。
- SVG 输出结构变化。
- Sample 文件变化。
- LLM 对接文件变化。
- 新增、删除或修改器件类型。
- 修改器件端子命名。
- 新增桌面端功能并计划同步到网页端。

Agent5 每次会输出：

```text
网页端是否需要同步：是/否
需要同步的文件：
需要同步的接口：
是否需要更新 Sample：
是否需要更新 LLM 对接文件：
是否需要通知 Agent6 调整界面：
```
