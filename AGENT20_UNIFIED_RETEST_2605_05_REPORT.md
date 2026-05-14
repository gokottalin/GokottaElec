# Agent20 统一复测报告 2605-05

日期：2026-05-15  
执行角色：Agent20  
复测范围：核心链路、SVG 显示、Web 多电路、移动端、复制/下载  
复测产物目录：`E:\Project\2605_Elec\output\agent20-unified-retest-2605-05\`  

## 1. 结论

本轮统一复测通过。

```text
P0：0
P1：0
P2：0
P3：0
阻断发布：否
```

覆盖结果：

```text
核心链路：通过
SVG 显示：通过
Web 多电路：通过
移动端：通过
复制/下载：通过
```

## 2. 执行命令

统一命令结果文件：

```text
output/agent20-unified-retest-2605-05/unified-retest-command-results.json
output/agent20-unified-retest-2605-05/logs/
```

本轮实际执行：

```powershell
node --check scripts\parse-cnl.mjs
node --check scripts\erc-check.mjs
node --check scripts\erc-rules.mjs
node --check scripts\render-svg.mjs
node --check scripts\build-paste.mjs
node --check scripts\elec-cli.mjs
node --check scripts\generate-20-circuit-gallery.mjs
node --check web-miniapp\gokotta-elec.js
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent20-unified-retest-2605-05\samples\sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent20-unified-retest-2605-05\samples\sample-02
node scripts\elec-cli.mjs build samples\Sample-03-pnp-high-side-switch.txt output\agent20-unified-retest-2605-05\samples\sample-03
node scripts\elec-cli.mjs build samples\Sample-04-cmos-inverter-nmos-pmos.txt output\agent20-unified-retest-2605-05\samples\sample-04
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent20-unified-retest-2605-05\samples\sample-05
node scripts\generate-20-circuit-gallery.mjs
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
node output\agent20-v15-baseline-2605-03\run-core-baseline.mjs
node output\agent20-v15-baseline-2605-03\run-web-baseline.mjs
```

命令状态：

```text
18/18 OK
```

## 3. 核心链路

核心结果文件：

```text
output/agent20-v15-baseline-2605-03/core-baseline-results.json
```

结果摘要：

```text
generatedCircuitCount=20
fallbackCount=0
missingRefdesCount=0
parserInvalidRejected=true
ercWarningObserved=true
ercRequiredParameterObserved=true
passed=true
```

官方 Sample：

```text
Sample 01：OK
Sample 02：OK
Sample 03：OK
Sample 04：OK
Sample 05：OK
```

## 4. SVG 显示

SVG/图片检查结果：

```text
output/agent20-complex-20-2605-04/complex-20-visual-inspection.json
output/agent20-complex-20-2605-04/complex-20-visual-summary.txt
```

结果摘要：

```text
circuitCount=20
imageCount=20
passed=true
issueCounts={}
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
8. 文字重叠。
```

图片证据：

```text
output/twenty-circuits/gallery.png
output/twenty-circuits/gallery.html
output/twenty-circuits/*/*.png
```

总览图尺寸：

```text
output/twenty-circuits/gallery.png 2400x3200
```

## 5. Web 多电路

Web 结果文件：

```text
output/agent20-v15-baseline-2605-03/web-baseline-results.json
```

多电路结果：

```text
optionCount=20
selectedIndex=19
summary=20 / 20
irCircuitId=AGENT20_V15_20_OPAMP_NONINVERTING_GAIN_2
downloadDisabled=false
copyIrDisabled=false
horizontalOverflow=false
textOverflow=[]
passed=true
```

截图证据：

```text
output/agent20-v15-baseline-2605-03/evidence/web-multi-selected-20.png
```

截图尺寸：

```text
1440x2978
```

## 6. 移动端

移动端结果：

```text
viewport=390x844
irCircuitId=AGENT20_V15_01_VOLTAGE_DIVIDER_5V_HALF
downloadDisabled=false
copyIrDisabled=false
horizontalOverflow=false
textOverflow=[]
```

截图证据：

```text
output/agent20-v15-baseline-2605-03/evidence/web-mobile-first-circuit.png
```

截图尺寸：

```text
390x3063
```

## 7. 复制/下载

复制结果：

```text
copiedIrLooksValid=true
copiedBasicLooksValid=true
copiedFullLooksValid=true
```

下载结果：

```text
suggestedFilename=AGENT20_V15_20_OPAMP_NONINVERTING_GAIN_2.svg
downloadLooksSvg=true
downloadedSize=5636 bytes
```

下载证据：

```text
output/agent20-v15-baseline-2605-03/evidence/downloaded-selected-20.svg
```

## 8. 备注

本轮仅执行复测与报告记录，不修改实现文件。  
当前 `AGENT20_ISSUES_2605_COMPLEX20.md` 中已知的两类 P2 在本轮统一复测中保持关闭状态。
