# Agent4 Complex20 SVG 规则复核报告 2605-05

日期：2026-05-15  
执行角色：Agent4 - 电路图生成器  
依据文件：

```text
AGENT0_COMPLEX20_FIX_AND_ASSIGNMENT_2605_04.md
AGENT0_V15_FIRST_ROUND_TRIAGE_2605_05.md
AGENT20_COMPLEX20_CLOSEOUT_2605_05.md
AGENT3_COMPLEX20_ERC_RULE_REPORT_2605_04.md
```

## 1. 任务结论

根据 Agent0 和 Agent20 的最新结果，Complex20 中与 SVG 相关的 P2 已关闭。本轮 Agent4 继续完成 SVG 端规则复核，并补齐一个通用去重边界：

```text
同 kind + net + 坐标的 groundPort 现在只绘制一次，不再因 labelSide 不同重复绘制同一地端口。
```

当前 Agent4 结论：

```text
fallbackCount=0
missingRefdesCount=0
Complex20 visual inspection passed=true
issueCounts={}
```

## 2. 修改文件

```text
scripts/render-svg.mjs
```

本轮补充修改：

```text
groundPort(...) 调用 claimPort("ground", netId, x, y)
```

效果：

```text
1. powerPort / negativePort / groundPort 的去重规则保持为 kind + net + 坐标。
2. 同坐标重复地端口不再因为 labelSide 差异重复输出符号。
3. 不影响不同坐标的同名 VCC/GND 标签。
4. 不隐藏器件 refdes、value 或不同位置的可测试网络文本。
```

## 3. 复核范围

Agent4 已复核当前 renderer 规则：

```text
1. claimPort(...) 对端口符号做同坐标去重。
2. netLabel(...) 对同 net + 坐标 + anchor + class 的标签去重。
3. COMPARATOR_SINGLE 不会被普通 voltage divider / RC renderer 误识别。
4. 20 电路图库 07-17 中已声明 SIGNAL_SOURCE V2 均进入 SVG。
```

## 4. 验证命令

```powershell
node --check scripts\render-svg.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent4-rule-closeout\sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent4-rule-closeout\sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent4-rule-closeout\sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent4-rule-closeout\sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent4-rule-closeout\sample-05
node scripts\generate-20-circuit-gallery.mjs
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
node output\agent20-v15-baseline-2605-03\run-core-baseline.mjs
node scripts\validate-model-packages.mjs
```

## 5. 验证结果

```text
render-svg syntax：OK
official samples：5/5 OK
Complex20 generatedCircuitCount：20
Complex20 visual inspection：passed=true
Complex20 issueCounts：{}
SVG fallbackCount：0
SVG missingRefdesCount：0
V1.5 core baseline：passed=true
validate-model-packages：OK
```

## 6. 交接说明

本轮 Agent4 没有修改 CNL、ERC、Web API 或 UI 文件。  
SVG 输出结构仍为现有 text/path/port 结构，仅减少同坐标重复端口和重复标签。

Agent5/Agent6 无需因本次 Agent4 改动同步 API 或 DOM 绑定；如后续将 twenty-circuits gallery 接入网页，再按 Agent0 建议由 Agent5/Agent6 处理。
