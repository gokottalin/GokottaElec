# Agent20 Complex20 P2 复测关闭报告 2605-05

日期：2026-05-15  
执行角色：Agent20  
复测对象：

```text
AGENT20_ISSUES_2605_COMPLEX20.md
AGENT0_COMPLEX20_FIX_AND_ASSIGNMENT_2605_04.md
output\twenty-circuits\
output\agent20-complex-20-2605-04\
```

## 1. 结论

本轮复测通过，原 Complex20 两类 P2 问题均已关闭。

```text
A20-C20-P2-01 EXTRA_DECLARED_NET N_BIAS：已关闭
A20-C20-P2-02 TEXT_OVERLAP VCC：已关闭
P0：0
P1：0
P2：0
P3：0
阻断发布：否
```

## 2. 复测命令

```powershell
node --check scripts\parse-cnl.mjs
node --check scripts\erc-check.mjs
node --check scripts\render-svg.mjs
node --check scripts\build-paste.mjs
node --check web-miniapp\gokotta-elec.js
node --check scripts\generate-20-circuit-gallery.mjs
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent0-workplan-2605-05\sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent0-workplan-2605-05\sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent0-workplan-2605-05\sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent0-workplan-2605-05\sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent0-workplan-2605-05\sample-05
node scripts\generate-20-circuit-gallery.mjs
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
node output\agent20-v15-baseline-2605-03\run-core-baseline.mjs
node output\agent20-v15-baseline-2605-03\run-web-baseline.mjs
```

## 3. 结果摘要

```text
syntax checks：通过
validate-model-packages：通过
official samples：5/5 通过
Complex20 generatedCircuitCount：20
Complex20 visual inspection：passed=true
Complex20 issueCounts：{}
V1.5 core baseline：passed=true，generatedCircuitCount=20，fallbackCount=0，missingRefdesCount=0
V1.5 web baseline：passed=true，multi.optionCount=20，mobile.horizontalOverflow=false
```

## 4. 关闭依据

`A20-C20-P2-01` 关闭依据：

```text
1. scripts\generate-20-circuit-gallery.mjs 已删除 07-13 中未连接的 N_BIAS 声明。
2. enforceGalleryIrRules(irData) 会阻断重复 net、重复 refdes、连接引用未声明 net、声明但未使用 net。
3. 最新 visual inspection 中 EXTRA_DECLARED_NET=0。
```

`A20-C20-P2-02` 关闭依据：

```text
1. scripts\render-svg.mjs 已通过 claimPort/netLabel 对同 kind/net/坐标的端口和标签去重。
2. PNP 共射类电路 10、11 不再出现同坐标重复 VCC 标签。
3. 最新 visual inspection 中 TEXT_OVERLAP=0。
```

## 5. 证据文件

```text
output\agent20-complex-20-2605-04\complex-20-visual-inspection.json
output\agent20-complex-20-2605-04\complex-20-visual-summary.txt
output\twenty-circuits\gallery.png
output\agent20-v15-baseline-2605-03\core-baseline-results.json
output\agent20-v15-baseline-2605-03\web-baseline-results.json
```

## 6. 备注

本报告只记录复测关闭结果，不覆盖原始问题报告。原问题报告继续保留为发现过程和复现证据。
