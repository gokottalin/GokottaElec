# Agent20 第2次测试问题上报

日期码：2605  
测试日期：2026-05-12  
测试版本：GokottaElec V1.3  
测试重点：网页版 `web-miniapp`  
输出对象：Agent0  
测试原则：只上报问题，不修改实现。

## 本次测试范围

本轮使用 20 个电路测试网页版 Sample 加载、生成预览、SVG/IR/ERC 展示、复制、下载和移动端首屏布局。

测试输入：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\agent20-test02-20-circuits.cnl
```

本轮本地 Web API mock：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\mock-web-server.mjs
```

测试 URL：

```text
http://127.0.0.1:51320/gokotta-elec.html
```

Browser 插件状态：

```text
Browser plugin invocation failed: Failed to connect to browser-use backend "iab". No Codex IAB backends were discovered.
Fallback: 使用 bundled Playwright 进行 localhost Web 自动化测试。
```

## 20 个电路

1. `AGENT20_T02_01_VOLTAGE_DIVIDER_5V_HALF`
2. `AGENT20_T02_02_VOLTAGE_DIVIDER_12V_THIRD`
3. `AGENT20_T02_03_SENSOR_BIAS_DIVIDER_3V3`
4. `AGENT20_T02_04_RC_LOWPASS_AUDIO`
5. `AGENT20_T02_05_RC_LOWPASS_ANTIALIAS`
6. `AGENT20_T02_06_RC_LOWPASS_DEBOUNCE`
7. `AGENT20_T02_07_NPN_COMMON_EMITTER_SMALL_SIGNAL`
8. `AGENT20_T02_08_NPN_COMMON_EMITTER_HIGH_GAIN`
9. `AGENT20_T02_09_NPN_COMMON_EMITTER_LOW_VOLTAGE`
10. `AGENT20_T02_10_PNP_COMMON_EMITTER_SMALL_SIGNAL`
11. `AGENT20_T02_11_PNP_COMMON_EMITTER_INVERTER_STAGE`
12. `AGENT20_T02_12_NPN_EMITTER_FOLLOWER_BUFFER`
13. `AGENT20_T02_13_NPN_EMITTER_FOLLOWER_LOW_Z`
14. `AGENT20_T02_14_NPN_LOW_SIDE_RED_LED_SWITCH`
15. `AGENT20_T02_15_NPN_LOW_SIDE_BLUE_LED_SWITCH`
16. `AGENT20_T02_16_PNP_HIGH_SIDE_GREEN_LED_SWITCH`
17. `AGENT20_T02_17_PNP_HIGH_SIDE_AMBER_LED_SWITCH`
18. `AGENT20_T02_18_CMOS_INVERTER_5V`
19. `AGENT20_T02_19_CMOS_INVERTER_3V3`
20. `AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2`

## 执行记录

批量核心构建：

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\bulk-build
```

结果：

```text
20/20 OK
```

网页版逐个 Sample 自动化：

```powershell
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
& 'C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' output\agent20-test-2605-02\run-web-20-check.cjs
```

结果摘要：

```text
testedCount=20
fallbackCount=0
failedCount=0
publicNetMissingCount=0
textOverflowCount=0
mobileHasHorizontalOverflow=false
missingRefdesCount=11
```

复制和下载控件：

```text
copyIrEnabled=true
downloadEnabled=true
copiedIrLooksValid=true
copiedBasicLooksValid=true
copiedFullLooksValid=true
downloadLooksSvg=true
```

## 问题列表

### A20-T02-P1：BJT 放大器/LED 开关类 SVG 丢失已声明的 `SIGNAL_SOURCE V2`

严重级别：P1  
影响模块：SVG 渲染 / 网页预览 / 元器件完整性  
关联文件：

```text
D:\Project\2605-Elec\scripts\render-svg.mjs
```

复现步骤：

1. 启动本轮 mock API。
2. 打开：

```text
http://127.0.0.1:51320/gokotta-elec.html
```

3. 从 Sample 下拉中依次选择第 7 至第 17 个电路。
4. 点击 `生成预览`。
5. 对照左侧 CNL 输入和右侧 SVG 原理图。

实际结果：

- 左侧 CNL 中存在：

```text
器件 V2 是 SIGNAL_SOURCE ...
连接 VIN/CTRL: V2.OUT ...
连接 GND: V2.REF ...
```

- 右侧 SVG 中没有 `V2` 文字，也没有信号源符号。
- SVG 只显示 `VIN` 或 `CTRL` 输入标签，等价于把已声明元器件 `V2` 从原理图中省略。
- 自动化结果中第 7 至第 17 个电路均出现 `refdesMissing=["V2"]`。

受影响电路：

```text
07 AGENT20_T02_07_NPN_COMMON_EMITTER_SMALL_SIGNAL
08 AGENT20_T02_08_NPN_COMMON_EMITTER_HIGH_GAIN
09 AGENT20_T02_09_NPN_COMMON_EMITTER_LOW_VOLTAGE
10 AGENT20_T02_10_PNP_COMMON_EMITTER_SMALL_SIGNAL
11 AGENT20_T02_11_PNP_COMMON_EMITTER_INVERTER_STAGE
12 AGENT20_T02_12_NPN_EMITTER_FOLLOWER_BUFFER
13 AGENT20_T02_13_NPN_EMITTER_FOLLOWER_LOW_Z
14 AGENT20_T02_14_NPN_LOW_SIDE_RED_LED_SWITCH
15 AGENT20_T02_15_NPN_LOW_SIDE_BLUE_LED_SWITCH
16 AGENT20_T02_16_PNP_HIGH_SIDE_GREEN_LED_SWITCH
17 AGENT20_T02_17_PNP_HIGH_SIDE_AMBER_LED_SWITCH
```

期望结果：

- SVG 中应渲染每个 IR `devices[]` 中的元器件，至少应出现对应 `refdes`。
- 如果设计上要把信号源折叠为输入端口，IR 或 SVG 应显式标记该折叠行为，并且前端应避免让用户误判“元器件缺失”。
- 对于游客体验，左侧 CNL 出现 `V2`，右侧原理图完全不出现 `V2`，应视为元器件少画。

证据文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\web-20-results.json
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\web-20\07-agent20_t02_07_npn_common_emitter_small_signal.png
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\web-20\14-agent20_t02_14_npn_low_side_red_led_switch.png
```

定位线索：

- `renderCommonEmitter` 中输入侧使用 `inputPortToNet(...)` + `drawCapacitor(...)`，未绘制 `SIGNAL_SOURCE V2`。
- `renderEmitterFollower` 中输入侧使用 `inputPortToNet(...)` + `drawCapacitor(...)`，未绘制 `SIGNAL_SOURCE V2`。
- `renderBjtLedSwitch` 中控制侧使用 `inputPortToNet(...)` + `drawResistor(...)`，未绘制 `SIGNAL_SOURCE V2`。

### A20-T02-P2：网页版接收多电路构建响应时只展示第 1 个电路，19 个电路被用户界面吞掉

严重级别：P1  
影响模块：Web 小程序 / 多电路体验 / API 契约  
关联文件：

```text
D:\Project\2605-Elec\web-miniapp\gokotta-elec.js
D:\Project\2605-Elec\web-miniapp\gokotta-elec.html
```

复现步骤：

1. 将本轮 20 个 CNL 电路整体粘贴到 `textarea#cnlInput`。
2. 后端 `/api/elec/build` 返回：

```text
ok=true
circuits.length=20
```

3. 点击 `生成预览`。
4. 观察 `SVG 原理图` 和 `IR JSON` 区域。

实际结果：

- API 返回 20 个电路。
- Web UI 只展示第 1 个电路：

```text
svgTitle=AGENT20_T02_01_VOLTAGE_DIVIDER_5V_HALF
irCircuitId=AGENT20_T02_01_VOLTAGE_DIVIDER_5V_HALF
```

- 页面没有电路列表、下一个/上一个、结果下拉或任何 `circuits[]` 选择入口。
- 用户无法在页面中查看第 2 至第 20 个电路的 SVG、IR、ERC。
- 截图中左侧输入区域当前可见的是第 20 个电路文本，但右侧仍显示第 1 个电路 SVG，形成明显上下文错配。

期望结果：

- 当 `/api/elec/build` 返回 `circuits.length > 1` 时，网页版应显示可选择的电路列表。
- SVG、IR、ERC 应随选中电路同步切换。
- 如果网页版一期只支持单电路，应在提交前阻止多电路输入，或在结果区明确提示“只显示第 1 个，共 N 个”。

证据文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\web-multipaste-results.json
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\multi-paste\web-multipaste-20-result.png
```

定位线索：

- `web-miniapp\gokotta-elec.js:69` 的 `normalizeBuildResponse(data)` 只读取 `data.artifacts` 或 `data.circuits[0]`。
- `web-miniapp\gokotta-elec.js:74` 只设置单个 `svg`。
- `web-miniapp\gokotta-elec.js:75` 只设置单个 `ir`。
- `web-miniapp\gokotta-elec.js:146` 直接把当前 SVG 写入 `#svgPreview`。
- `web-miniapp\gokotta-elec.html` 没有多电路结果列表 DOM。

### A20-T02-P3：20 个长 Sample 标题在网页版下拉框中被截断，用户难以区分相近电路

严重级别：P3  
影响模块：Web 小程序 / Sample 选择体验  
关联文件：

```text
D:\Project\2605-Elec\web-miniapp\gokotta-elec.css
```

复现步骤：

1. `/api/elec/samples` 返回 20 个 Sample。
2. 每个 Sample title 包含编号和电路 ID，例如：

```text
07 - AGENT20_T02_07_NPN_COMMON_EMITTER_SMALL_SIGNAL
14 - AGENT20_T02_14_NPN_LOW_SIDE_RED_LED_SWITCH
20 - AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2
```

3. 打开网页版并查看 `select#sampleSelect`。

实际结果：

- 下拉框闭合状态只显示标题前半截。
- 多个相近电路名在闭合状态下无法完整区分。
- `web-miniapp\gokotta-elec.css:159` 设置了：

```css
select {
  max-width: 240px;
}
```

期望结果：

- 当 Sample 数量较多且标题较长时，用户应能明确识别当前选中的完整电路。
- 可以通过更宽的选择器、标题 tooltip、单独的 Sample 列表、短标题 + 详情区域等方式解决。

证据文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\web-20\07-agent20_t02_07_npn_common_emitter_small_signal.png
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\web-20\20-agent20_t02_20_opamp_noninverting_gain2.png
```

## 本轮明确通过的检查

```text
20/20 单电路 Web 生成成功。
20/20 未出现 fallback 摘要图。
20/20 未检测到 public net 标签缺失。
20/20 未检测到文本出 SVG viewBox。
20/20 未检测到 refdes 文本互相覆盖。
移动端 390x844 首屏未检测到页面级横向溢出。
复制 IR 成功。
复制基础 LLM 成功。
复制完整 LLM 成功。
下载 SVG 成功。
```

## 本轮产物索引

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\
```

关键文件：

```text
D:\Project\2605-Elec\output\agent20-test-2605-02\agent20-test02-20-circuits.cnl
D:\Project\2605-Elec\output\agent20-test-2605-02\bulk-build\summary.txt
D:\Project\2605-Elec\output\agent20-test-2605-02\web-20-results.json
D:\Project\2605-Elec\output\agent20-test-2605-02\web-controls-results.json
D:\Project\2605-Elec\output\agent20-test-2605-02\web-multipaste-results.json
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\web-20\
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\multi-paste\web-multipaste-20-result.png
D:\Project\2605-Elec\output\agent20-test-2605-02\evidence\controls\controls-after-copy-download.png
```
