# Agent0 V1.6 发布报告 - 2605-05

## 发布结论

GokottaElec V1.6 已满足发布条件，可以同步至 Git。

本次发布重点不是单点修词，而是把复测暴露的显示与电路生成问题收束为可复用规则：

- CNL parser：增强括号/型号/封装/引脚包解析，避免复杂器件描述丢失结构信息。
- 器件库：补充 model package、封装引脚映射与文档说明，为后续扩展器件提供统一入口。
- ERC：增加未使用网络与复杂生成场景校验，规则文档同步更新。
- SVG 生成：加入重复文本、端口标签、器件布局与复杂样例的生成约束。
- Web 同步：网页保留多电路 `circuits[]`，支持选择器、IR/ERC/SVG 同步更新与下载文件名同步。
- 体验一致性：统一视觉层级、移动端溢出与控件状态。
- 测试：建立 V1.5 baseline、complex20 复测与 V1.6 发布门禁。

## 版本变更

- `VERSION`: `V1.6`
- `package.json`: `version=1.6.0`, `gokottaVersion=V1.6`
- launcher GUI/CLI: `AppVersion=V1.6`
- CLI help: `GokottaElec V1.6`
- README / release policy / handoff 文档同步 V1.6

## 发布包

- 本地目录：`release/GokottaElec`
- 本地压缩包：`release/GokottaElec-V1.6.zip`
- ZIP 大小：675485 bytes
- SHA256：`6F3638032FB1FDB85A9ADC805BE3AA25CF73F231716831A48297E8275591702A`

`release/` 为本地发布产物目录，保持 Git ignore，不纳入源码提交。

## 发布门禁

已通过：

- JS syntax check：`parse-cnl`、`erc-check`、`erc-rules`、`render-svg`、`build-paste`、`elec-cli`、`generate-20-circuit-gallery`、`web-miniapp/gokotta-elec`
- Model package validation：`OK: components/model-packages.v0.1.json`
- CLI help：输出 `GokottaElec V1.6`
- 官方 samples 01-05：全部 build OK
- 20 电路 gallery：生成 OK
- complex20 visual inspect：`passed=true`, `issueCounts={}`
- core baseline：`passed=true`, `generatedCircuitCount=20`, `fallbackCount=0`, `missingRefdesCount=0`
- web baseline：`passed=true`, `optionCount=20`, `selectedIndex=19`, `mobile.horizontalOverflow=false`
- launcher GUI build：通过
- launcher CLI build：通过

## Git 发布动作

建议执行：

1. 提交源码与报告：`Release GokottaElec V1.6`
2. 创建标签：`v1.6`
3. 推送：`main` 与 `v1.6`

