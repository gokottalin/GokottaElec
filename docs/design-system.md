# GokottaElec Design System

Agent6 视觉基准：以已选软件图标候选 5 为主视觉，保持成熟蓝色层次、清晰电路线条和简洁平面设计。界面必须优先服务电路输入、诊断、预览和导出，不用装饰压过原理图本身。

## Brand Mark

- 桌面图标：`launcher/GokottaElec.ico`
- 桌面页头预览图：`launcher/GokottaElec.png`
- 网页小程序图标：`web-miniapp/assets/gokotta-elec-icon.png`

图标特征：

- 圆角方形蓝色底。
- 深蓝到亮蓝的层次。
- 白色粗电路线与圆形节点。
- 局部青蓝走线作为辅助层，不使用红、紫、橙作为主色。

## Color Tokens

```text
Shell blue:      #EDF7FD
Panel soft:      #F6FBFF
Surface:         #FFFFFF
Border blue:     #B7D8EE
Brand navy:      #0D4885
Brand blue:      #137CD6
Circuit cyan:    #4DBEF1
Text blue:       #123657
Muted blue:      #526F85
Success teal:    #0F766E
Warning amber:   #8A5A00
Error red:       #B42318
```

## Interface Rules

- 主背景使用浅蓝工作区，不使用深色大面积背景。
- 工具面板使用白色或极浅蓝表面，边框统一为浅蓝。
- 主要操作按钮使用 `#137CD6`，普通按钮使用白底蓝灰边框。
- 空状态、预览状态、日志区域可以保留电路网格或节点感，但不得影响内容阅读。
- SVG 预览区域优先保证电路图清晰，装饰只放在容器背景层。
- 诊断结果不能只靠颜色表达，必须保留文本等级、错误码或说明。
- 窄屏下按钮可换行或分列，但文字不能溢出按钮。

## State Treatment

- 初始空状态：蓝色电路图标、短提示、诊断日志保持信息态。
- 正在生成：主按钮禁用，预览区显示轻量动效，日志显示当前 API 或本地构建动作。
- 成功预览：白色 SVG 画布，浅蓝或成功色边框，日志显示 `OK` 或诊断文本。
- 解析失败：预览区进入错误态，日志显示 parser 诊断，不隐藏行号和错误码。
- ERC WARNING：预览仍可显示，日志使用琥珀提示，保留 WARNING 文本。
- ERC ERROR：预览和日志进入错误态，不用视觉美化削弱阻断信息。
- 后端未接入：保持蓝色产品基调，但日志明确说明缺少对应 API。
- 多电路结果：结果切换条显示当前序号和总数，SVG、IR、ERC、下载文件名随当前电路同步。
- 复制成功/失败：诊断日志给出成功或失败反馈。
- 下载成功/失败：诊断日志给出文件名或失败原因。

## Responsive Rules

- 390x844 移动端不得出现页面级横向滚动。
- 输入、预览、诊断和 IR 面板在 900px 以下改为单列。
- 560px 以下按钮使用稳定网格，避免长文案撑破按钮。
- SVG 预览容器可内部滚动，但不能把整页撑宽。

## Agent6 Handoff Checklist

每次 UI/UX 改动后，Agent6 至少记录：

```text
1. 覆盖的状态列表。
2. 桌面端和移动端影响。
3. 是否影响 Agent5 的 DOM 或 API 绑定。
4. 可访问性和文本溢出检查。
5. 截图或手工验收记录。
```
