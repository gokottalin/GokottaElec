# Agent20 20 个复杂电路图片与视觉检查报告

日期：2026-05-14  
执行角色：Agent20  
测试对象：`scripts\generate-20-circuit-gallery.mjs` 生成的 20 个电路  
输出目录：`E:\Project\2605_Elec\output\twenty-circuits\`  
检查目录：`E:\Project\2605_Elec\output\agent20-complex-20-2605-04\`  

## 1. 结论

20 个电路均已输出单独 PNG 图片和 SVG 文件，图片非空白，尺寸为 `1120x760`。  
本轮发现 2 类 P2 问题：

```text
P0：0
P1：0
P2：2 类
P3：0
阻断发布：否，建议进入当前迭代修复
```

问题摘要：

```text
1. 07-13 共 7 个电路声明了未连接、不可见的 N_BIAS 节点。
2. 10、11 两个 PNP 共射电路的 VCC 标签在同一坐标重复绘制。
```

未发现：

```text
1. PNG 缺失或空白。
2. ERC validate 失败。
3. 连接中引用未声明 net。
4. SVG 中出现未声明 net 标签。
5. 器件 refdes 缺失。
6. 符号主体重叠。
7. 图形越界。
```

## 2. 执行命令

生成 20 个电路图片：

```powershell
node scripts\generate-20-circuit-gallery.mjs
```

执行节点和视觉检查：

```powershell
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

检查结果：

```text
circuitCount=20
imageCount=20
passed=false
issueCounts={"EXTRA_DECLARED_NET":7,"TEXT_OVERLAP":2}
```

## 3. 图片输出清单

```text
01 output/twenty-circuits/01-voltage-divider-5v-half/01-voltage-divider-5v-half.png
02 output/twenty-circuits/02-voltage-divider-12v-third/02-voltage-divider-12v-third.png
03 output/twenty-circuits/03-sensor-bias-divider-3v3/03-sensor-bias-divider-3v3.png
04 output/twenty-circuits/04-rc-lowpass-audio/04-rc-lowpass-audio.png
05 output/twenty-circuits/05-rc-lowpass-antialias/05-rc-lowpass-antialias.png
06 output/twenty-circuits/06-rc-lowpass-debounce/06-rc-lowpass-debounce.png
07 output/twenty-circuits/07-npn-common-emitter-small-signal/07-npn-common-emitter-small-signal.png
08 output/twenty-circuits/08-npn-common-emitter-high-gain/08-npn-common-emitter-high-gain.png
09 output/twenty-circuits/09-npn-common-emitter-low-voltage/09-npn-common-emitter-low-voltage.png
10 output/twenty-circuits/10-pnp-common-emitter-small-signal/10-pnp-common-emitter-small-signal.png
11 output/twenty-circuits/11-pnp-common-emitter-inverter-stage/11-pnp-common-emitter-inverter-stage.png
12 output/twenty-circuits/12-npn-emitter-follower-buffer/12-npn-emitter-follower-buffer.png
13 output/twenty-circuits/13-npn-emitter-follower-low-z/13-npn-emitter-follower-low-z.png
14 output/twenty-circuits/14-npn-low-side-red-led-switch/14-npn-low-side-red-led-switch.png
15 output/twenty-circuits/15-npn-low-side-blue-led-switch/15-npn-low-side-blue-led-switch.png
16 output/twenty-circuits/16-pnp-high-side-green-led-switch/16-pnp-high-side-green-led-switch.png
17 output/twenty-circuits/17-pnp-high-side-amber-led-switch/17-pnp-high-side-amber-led-switch.png
18 output/twenty-circuits/18-cmos-inverter-5v/18-cmos-inverter-5v.png
19 output/twenty-circuits/19-cmos-inverter-3v3/19-cmos-inverter-3v3.png
20 output/twenty-circuits/20-opamp-noninverting-gain-2/20-opamp-noninverting-gain-2.png
```

总览图：

```text
output/twenty-circuits/gallery.png
output/twenty-circuits/gallery.html
```

## 4. 逐项结果

```text
OK    01 CIRCUIT_01_VOLTAGE_DIVIDER_5V_HALF
OK    02 CIRCUIT_02_VOLTAGE_DIVIDER_12V_THIRD
OK    03 CIRCUIT_03_SENSOR_BIAS_DIVIDER_3V3
OK    04 CIRCUIT_04_RC_LOWPASS_AUDIO
OK    05 CIRCUIT_05_RC_LOWPASS_ANTIALIAS
OK    06 CIRCUIT_06_RC_LOWPASS_DEBOUNCE
ISSUE 07 CIRCUIT_07_NPN_COMMON_EMITTER_SMALL_SIGNAL       EXTRA_DECLARED_NET N_BIAS
ISSUE 08 CIRCUIT_08_NPN_COMMON_EMITTER_HIGH_GAIN          EXTRA_DECLARED_NET N_BIAS
ISSUE 09 CIRCUIT_09_NPN_COMMON_EMITTER_LOW_VOLTAGE        EXTRA_DECLARED_NET N_BIAS
ISSUE 10 CIRCUIT_10_PNP_COMMON_EMITTER_SMALL_SIGNAL       EXTRA_DECLARED_NET N_BIAS, TEXT_OVERLAP VCC
ISSUE 11 CIRCUIT_11_PNP_COMMON_EMITTER_INVERTER_STAGE     EXTRA_DECLARED_NET N_BIAS, TEXT_OVERLAP VCC
ISSUE 12 CIRCUIT_12_NPN_EMITTER_FOLLOWER_BUFFER           EXTRA_DECLARED_NET N_BIAS
ISSUE 13 CIRCUIT_13_NPN_EMITTER_FOLLOWER_LOW_Z            EXTRA_DECLARED_NET N_BIAS
OK    14 CIRCUIT_14_NPN_LOW_SIDE_RED_LED_SWITCH
OK    15 CIRCUIT_15_NPN_LOW_SIDE_BLUE_LED_SWITCH
OK    16 CIRCUIT_16_PNP_HIGH_SIDE_GREEN_LED_SWITCH
OK    17 CIRCUIT_17_PNP_HIGH_SIDE_AMBER_LED_SWITCH
OK    18 CIRCUIT_18_CMOS_INVERTER_5V
OK    19 CIRCUIT_19_CMOS_INVERTER_3V3
OK    20 CIRCUIT_20_OPAMP_NONINVERTING_GAIN_2
```

## 5. 证据文件

```text
output/agent20-complex-20-2605-04/inspect-twenty-circuits.mjs
output/agent20-complex-20-2605-04/complex-20-visual-inspection.json
output/agent20-complex-20-2605-04/complex-20-visual-summary.txt
```

详细问题单见：

```text
AGENT20_ISSUES_2605_COMPLEX20.md
```
