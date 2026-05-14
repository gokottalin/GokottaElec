# Agent20 20 个复杂电路测试问题上报

日期：2026-05-14  
测试目录：`E:\Project\2605_Elec\output\twenty-circuits\`  
检查结果：`E:\Project\2605_Elec\output\agent20-complex-20-2605-04\complex-20-visual-inspection.json`  

## A20-C20-P2-01：07-13 电路存在多余声明节点 `N_BIAS`

问题编号：A20-C20-P2-01  
严重级别：P2  
影响模块：20 电路生成脚本 / IR fixture / SVG 节点展示  
主责建议：Agent0 分配给 20 电路测试 fixture 维护者，必要时由 Agent4 复核 SVG 显示策略  
阻断发布：否  

复现步骤：

```powershell
node scripts\generate-20-circuit-gallery.mjs
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

实际结果：

```text
07 CIRCUIT_07_NPN_COMMON_EMITTER_SMALL_SIGNAL    extraDeclaredNets=["N_BIAS"]
08 CIRCUIT_08_NPN_COMMON_EMITTER_HIGH_GAIN       extraDeclaredNets=["N_BIAS"]
09 CIRCUIT_09_NPN_COMMON_EMITTER_LOW_VOLTAGE     extraDeclaredNets=["N_BIAS"]
10 CIRCUIT_10_PNP_COMMON_EMITTER_SMALL_SIGNAL    extraDeclaredNets=["N_BIAS"]
11 CIRCUIT_11_PNP_COMMON_EMITTER_INVERTER_STAGE  extraDeclaredNets=["N_BIAS"]
12 CIRCUIT_12_NPN_EMITTER_FOLLOWER_BUFFER        extraDeclaredNets=["N_BIAS"]
13 CIRCUIT_13_NPN_EMITTER_FOLLOWER_LOW_Z         extraDeclaredNets=["N_BIAS"]
```

这些 IR 的 `nets[]` 声明了 `N_BIAS`，但 `connections[]` 中没有任何连接使用该节点，SVG 中也没有对应可见节点标签。

期望结果：

```text
每个声明的 net 都应被 connections[] 使用；如果 N_BIAS 不是实际节点，应从 fixture 的 nets[] 中删除。
```

证据文件：

```text
output/agent20-complex-20-2605-04/complex-20-visual-inspection.json
output/agent20-complex-20-2605-04/complex-20-visual-summary.txt
output/twenty-circuits/07-npn-common-emitter-small-signal/07-npn-common-emitter-small-signal.ir.json
output/twenty-circuits/13-npn-emitter-follower-low-z/13-npn-emitter-follower-low-z.ir.json
```

定位线索：

```text
scripts/generate-20-circuit-gallery.mjs 中 commonEmitterNpn/commonEmitterPnp/emitterFollowerNpn 的 net 列表包含 N_BIAS，
但对应 connections 未引用 N_BIAS。
```

## A20-C20-P2-02：10、11 电路的 `VCC` 标签重复绘制在同一坐标

问题编号：A20-C20-P2-02  
严重级别：P2  
影响模块：SVG renderer / PNP 共射类电路渲染  
主责建议：Agent4  
阻断发布：否  

复现步骤：

```powershell
node scripts\generate-20-circuit-gallery.mjs
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

实际结果：

```text
CIRCUIT_10_PNP_COMMON_EMITTER_SMALL_SIGNAL:
TEXT_OVERLAP net-label net-power:VCC vs net-label net-power:VCC at x=620.33 y=113 width=26.67 height=14 ratio=1

CIRCUIT_11_PNP_COMMON_EMITTER_INVERTER_STAGE:
TEXT_OVERLAP net-label net-power:VCC vs net-label net-power:VCC at x=620.33 y=113 width=26.67 height=14 ratio=1
```

SVG 证据中同一坐标重复出现 `VCC`：

```text
output/twenty-circuits/10-pnp-common-emitter-small-signal/10-pnp-common-emitter-small-signal.svg
output/twenty-circuits/11-pnp-common-emitter-inverter-stage/11-pnp-common-emitter-inverter-stage.svg
```

期望结果：

```text
同一个电源节点在同一坐标只绘制一次标签；如果需要多处 VCC 标签，应保证坐标不同且不重叠。
```

证据文件：

```text
output/agent20-complex-20-2605-04/complex-20-visual-inspection.json
output/twenty-circuits/10-pnp-common-emitter-small-signal/10-pnp-common-emitter-small-signal.png
output/twenty-circuits/11-pnp-common-emitter-inverter-stage/11-pnp-common-emitter-inverter-stage.png
```

定位线索：

```text
PNP 共射渲染中 VCC 电源标签被重复写入同一 x/y 坐标。
```
