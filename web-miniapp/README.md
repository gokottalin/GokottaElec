# GokottaElec 网页小程序骨架

这个目录是给 `GokottaMaker` 集成使用的最小前端小程序，不是完整网站。

建议复制到 GokottaMaker：

```text
D:\Project\26-WEB\GokottaMaker\tools\gokotta-elec.html
D:\Project\26-WEB\GokottaMaker\tools\gokotta-elec.js
D:\Project\26-WEB\GokottaMaker\styles\gokotta-elec.css
D:\Project\26-WEB\GokottaMaker\tools\assets\gokotta-elec-icon.png
```

也可以保持当前文件名直接放入网站根目录。

前端默认调用：

```http
GET  /api/elec/samples
POST /api/elec/build
GET  /api/elec/llm-handoff?mode=basic
GET  /api/elec/llm-handoff?mode=full
```

如果后端接口还没有接入，页面会显示“接口未接入”的诊断信息。

V1.3 起，网页骨架同步桌面端的 `复制基础LLM` 与 `复制完整LLM` 按钮。后端应读取 `llm-handoff/`、`schema/`、`docs/`、`samples/` 中对应文件，拼接为 Markdown 后返回给前端复制。

V1.5 起，网页骨架必须保留 `/api/elec/build` 返回的完整 `circuits[]`。当一次粘贴生成多个电路时，页面会显示结果选择器，切换时同步更新 SVG、IR、ERC 日志和 SVG 下载文件名。

视觉风格以已选候选图标 5 为准，图标源文件位于：

```text
assets/gokotta-elec-icon.png
```

后端接口详细要求见：

```text
../WEB_INTEGRATION_REQUIREMENTS.md
```
