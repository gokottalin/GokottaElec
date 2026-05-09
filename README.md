# GokottaElec

GokottaElec 用于把受控自然语言电路描述转换为脚本生成的电路原理图，并保留给其他 LLM 对接的 CNL 输出契约。

当前版本：`V1.2`

GitHub 目标仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 目录结构

- `dist/GokottaElec.exe`：Windows 桌面程序入口。
- `samples/`：5 个全新 Sample 输入文件，可直接加载或拖入命令行验证。
- `llm-handoff/`：给其他 LLM 使用的完整对接文件。
- `llm-interface/`：给其他 LLM 使用的精简对接入口。
- `web-miniapp/`：给 GokottaMaker 集成的网页版小程序骨架。
- `WEB_INTEGRATION_REQUIREMENTS.md`：GokottaMaker 后端/前端接口需求。
- `AGENT_WORKPLAN.md`：后续 Agent 分工规划。
- `RELEASE_POLICY.md`：版本递增与发版规则。
- `GIT_PUBLISH_GUIDE.md`：GitHub 上传指南。
- `components/`：器件库、端子、边界条件、型号封装引脚映射。
- `schema/`：电路 IR 结构约束。
- `scripts/`：CNL 解析、ERC 检查、SVG 原理图渲染脚本。
- `docs/`：CNL、ERC、器件库和 LLM 契约说明。
- `docs/design-system.md`：Agent6 维护的界面规范。
- `output/`：运行时输出目录，程序会自动创建。

## 使用方式

双击：

```powershell
dist\GokottaElec.exe
```

软件左侧输入栏上方有 Sample 下拉框，可直接载入 5 个内置示例。

软件图标位于 `launcher/GokottaElec.ico`。

命令行处理 `.txt` 或 `.cnl`：

```powershell
dist\GokottaElec.exe samples\Sample-01-voltage-divider.txt output\sample-01
dist\GokottaElec.exe "C:\Users\10731\Downloads\deepseek_cnl_20260507_72c637.txt" output\deepseek-test
```

程序会生成：

- `.cnl`：兼容层清洗后的 CNL
- `.ir.json`：规范化电路中间表示
- `.erc.txt`：电气规则检查报告
- `.svg`：完全脚本生成的原理图

## LLM 对接

给其他 LLM 最少提供：

- `llm-handoff/01_必需_系统提示词_直接复制给LLM.txt`
- `llm-handoff/02_必需_CNL输出契约_必须遵守.md`
- `llm-handoff/03_可选增强_输出模板_让LLM套用.txt`

要提高器件和引脚准确性，再提供：

- `llm-handoff/11_可选增强_完整器件库_端子和边界条件.json`
- `llm-handoff/12_可选增强_型号封装引脚库_PinMap.json`

## Sample

- `samples/Sample-01-voltage-divider.txt`：电阻分压。
- `samples/Sample-02-npn-low-side-switch.txt`：NPN 低边 LED 开关。
- `samples/Sample-03-pnp-high-side-switch.txt`：PNP 高边 LED 开关。
- `samples/Sample-04-cmos-inverter-nmos-pmos.txt`：NMOS + PMOS CMOS 反相器。
- `samples/Sample-05-opamp-noninverting-amplifier.txt`：运放同相放大器。

运行环境需要能访问 `node.exe`。

## Web 对接

网页版发布在 GokottaMaker。当前仓库已提供最小小程序骨架：

- `web-miniapp/gokotta-elec.html`
- `web-miniapp/gokotta-elec.css`
- `web-miniapp/gokotta-elec.js`

GokottaMaker 需要实现：

```http
GET  /api/elec/samples
POST /api/elec/build
```

详细接口见 `WEB_INTEGRATION_REQUIREMENTS.md`。

## 版本规则

- 当前版本为 `V1.2`。
- 普通更新增加 `0.1`，例如 `V1.0 -> V1.1`。
- 重大更新增加 `1.0`，例如 `V1.0 -> V2.0`。
