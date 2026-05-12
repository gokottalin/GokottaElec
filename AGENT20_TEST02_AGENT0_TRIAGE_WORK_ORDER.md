# Agent20 Test 2605-02 Agent0 复核与现有 Agent 分工

日期：2026-05-12  
测试目录：`D:\Project\2605-Elec\output\agent20-test-2605-02\`  
输入文件：`D:\Project\2605-Elec\output\agent20-test-2605-02\agent20-test02-20-circuits.cnl`  
当前主线版本：`GokottaElec V1.4`  
本文原则：只使用现有 Agent0-Agent5 职责，不新增职责，不调用 Agent6。

## 1. Agent0 已完成工作

Agent0 已完成以下复核：

1. 检查当前 Git 状态。
   - `main` 已同步 `origin/main`。
   - 工作区存在 Agent5 网页相关未提交文件：
     - `WEB_INTEGRATION_REQUIREMENTS.md`
     - `WEB_TEAM_HANDOFF.md`
     - `web-miniapp/README.md`
     - `web-miniapp/gokotta-elec.css`
     - `web-miniapp/gokotta-elec.html`
     - `web-miniapp/gokotta-elec.js`
   - 存在 Agent20 新报告：`AGENT20_ISSUES_2605_TEST02.md`。

2. 读取 Agent20 第 2 次测试产物。
   - `bulk-build/summary.txt`
   - `web-20-results.json`
   - `web-controls-results.json`
   - `web-multipaste-results.json`
   - `evidence/web-20/`
   - `evidence/multi-paste/`
   - `evidence/controls/`

3. 使用当前 V1.4 主线重新执行核心批量构建：

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\agent0-v14-core-recheck
```

复核结果：

```text
20/20 OK
fallbackCount=0
missingRefdes=11
missingRefdes 全部为 V2
受影响电路为 07-17
```

4. 执行脚本基础检查：

```powershell
node --check scripts\render-svg.mjs
node --check scripts\build-paste.mjs
npm.cmd run validate:models
```

复核结果：全部通过。

## 2. 当前不应发布新版本

本轮仍存在两个 P1：

- A20-T02-P1：SVG 少画已声明的 `SIGNAL_SOURCE V2`。
- A20-T02-P2：网页多电路响应只展示第 1 个电路，剩余 19 个被 UI 吞掉。

因此 Agent0 当前不应执行 `V1.5` 发布、tag 或 release 打包。  
Agent0 后续职责是在 Agent4 与 Agent5 修复后统一合并、回归、版本递增和发布。

## 3. 问题 A20-T02-P1：SVG 丢失 SIGNAL_SOURCE V2

严重级别：P1  
主责 Agent：Agent4-电路图生成器  
协同 Agent：Agent1、Agent2、Agent3  
Agent0 状态：已复核，当前不直接修复，交由 Agent4 主责实现。

### 事实

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

这些电路的 IR 中均存在：

```text
V2 = SIGNAL_SOURCE
V2.OUT -> VIN 或 CTRL
V2.REF -> GND
```

但对应 SVG 中没有 `V2` refdes，也没有信号源符号。当前 SVG 只画了 `VIN` 或 `CTRL` 输入标签。

### Agent4 任务

写入范围：

```text
scripts\render-svg.mjs
```

必须完成：

1. 在以下专用 renderer 中画出 `SIGNAL_SOURCE V2`：
   - `renderCommonEmitter`
   - `renderEmitterFollower`
   - `renderBjtLedSwitch`
2. 信号源至少应包含：
   - refdes：`V2`
   - 源符号
   - `OUT` 连接到 `VIN` 或 `CTRL`
   - `REF` 连接到 `GND`
   - amplitude/frequency/waveform 等关键参数，如果 IR 中存在则显示为 value。
3. 不允许只用输入端口标签替代已经声明的器件。
4. 如果后续确实设计为“折叠信号源”，必须在 SVG 或 IR metadata 中显式记录折叠行为；本轮建议直接画出 V2。

### Agent4 验收命令

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\agent4-after-fix
```

验收标准：

```text
20/20 OK
fallbackCount=0
07-17 SVG 均包含 >V2<
missingRefdesCount=0
所有官方 5 个 Sample 仍通过
```

## 4. 问题 A20-T02-P2：网页多电路响应只展示第 1 个

严重级别：P1  
主责 Agent：Agent5-软件与网页同步  
协同 Agent：Agent0  
Agent0 状态：已确认该问题仍存在于当前 web-miniapp 文件。

### 事实

`/api/elec/build` 返回：

```text
ok=true
circuits.length=20
```

但当前 `web-miniapp/gokotta-elec.js` 的 `normalizeBuildResponse(data)` 只读取：

```text
data.artifacts
data.circuits[0]
```

页面没有多电路结果列表、结果下拉、上一页/下一页或其他 `circuits[]` 切换入口。  
这会导致用户粘贴 20 个电路时，右侧只能看到第 1 个电路。

### Agent5 任务

写入范围：

```text
web-miniapp\gokotta-elec.html
web-miniapp\gokotta-elec.css
web-miniapp\gokotta-elec.js
WEB_INTEGRATION_REQUIREMENTS.md
WEB_TEAM_HANDOFF.md
```

必须完成：

1. `normalizeBuildResponse(data)` 必须保留完整 `circuits[]`。
2. 页面必须提供多电路结果选择入口。
3. 选择任意电路时，以下区域必须同步切换：
   - SVG 原理图
   - IR JSON
   - ERC/diagnostics
   - 下载 SVG 的文件名或内容
   - 复制 IR 的内容
4. 当 `circuits.length === 1` 时，单电路体验不能退化。
5. 当 `circuits.length > 1` 时，必须明确显示总数和当前选中项。

### Agent5 验收命令

使用 Agent20 已提供的 mock server 与 Playwright 脚本：

```powershell
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
& 'C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' output\agent20-test-2605-02\run-web-multipaste-check.cjs
```

验收标准：

```text
apiOk=true
apiCircuitCount=20
UI 可以访问 20 个 circuits
切换到第 20 个时 svgTitle 与 irCircuitId 均为 AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2
不存在“输入区显示第 20 个，预览区仍显示第 1 个”的上下文错配
```

## 5. 问题 A20-T02-P3：网页 Sample 下拉标题被截断

严重级别：P3  
主责 Agent：Agent5-软件与网页同步  
协同 Agent：Agent0  
Agent0 状态：已确认当前 CSS 仍存在 `select { max-width: 240px; }`。

### Agent5 任务

写入范围：

```text
web-miniapp\gokotta-elec.css
web-miniapp\gokotta-elec.html
web-miniapp\gokotta-elec.js
```

必须完成：

1. 让当前选中的 Sample 标题可识别。
2. 不能造成桌面或移动端横向溢出。
3. 可选实现方式：
   - 放宽或移除 `select` 的固定 `max-width: 240px`
   - 在 `select` 外显示完整标题
   - 增加 title tooltip
   - 改为可扫描的 Sample 列表
4. 不能牺牲移动端首屏布局。

验收标准：

```text
20 个长标题 Sample 可以区分
390x844 移动端 bodyScrollWidth <= clientWidth
复制和下载按钮仍可用
```

## 6. Agent1 分工

主责范围：CNL parser。  
本轮没有发现 parser 主故障，但 Agent1 需要在 Agent4/Agent5 修复后确认：

```text
20 个 circuit block 仍能正确拆分
SIGNAL_SOURCE V2 仍进入 IR devices[]
V2.OUT / V2.REF 连接关系不被兼容层改写丢失
```

验收命令：

```powershell
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\agent1-after-fix
```

## 7. Agent2 分工

主责范围：器件库、端子、封装、PinMap、边界条件。  
本轮没有发现器件库主故障，但 Agent2 需要确认：

```text
SIGNAL_SOURCE 端子 OUT / REF 定义正确
BJT_NPN / BJT_PNP / LED / MOS / OPAMP 与 20 个测试电路中使用的端子一致
不需要通过新增错误端子来绕过 SVG 渲染缺口
```

验收命令：

```powershell
npm.cmd run validate:models
```

## 8. Agent3 分工

主责范围：ERC/DRC 规则引擎与验证入口。  
本轮没有发现 ERC 主故障，但 Agent3 需要确认：

```text
Agent4 画出 V2 后，20/20 ERC 仍为 OK
不存在因为显示 V2 而误触发浮空端子或未连接端子错误
validate:models 仍通过
```

验收命令：

```powershell
npm.cmd run validate:models
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\agent3-after-fix
```

## 9. Agent20 后续回归

Agent20 只做测试和问题上报，不修改实现。  
Agent4 与 Agent5 完成后，Agent20 应复测：

```text
1. 20 个电路 Web 单独 Sample 预览。
2. 20 个电路一次性粘贴后多电路切换。
3. 07-17 中 V2 是否全部出现在 SVG。
4. 复制 IR、复制基础 LLM、复制完整 LLM、下载 SVG。
5. 390x844 移动端无页面级横向溢出。
```

## 10. Agent0 后续合并入口

Agent4 与 Agent5 修复后，Agent0 执行：

```powershell
node --check scripts\render-svg.mjs
node --check scripts\build-paste.mjs
npm.cmd run validate:models
npm.cmd run build:sample1
npm.cmd run build:sample2
npm.cmd run build:sample3
npm.cmd run build:sample4
npm.cmd run build:sample5
node scripts\build-paste.mjs output\agent20-test-2605-02\agent20-test02-20-circuits.cnl output\agent20-test-2605-02\agent0-final-after-fix
```

如果全部通过，再考虑：

```text
版本从 V1.4 升级到 V1.5
更新 release 包
提交、tag、push
```

## 11. 禁止事项

```text
不要重新规划 Agent 职责。
不要新增 Agent。
不要动用 Agent6，除非用户明确授权。
不要把 SIGNAL_SOURCE 从 IR 或 CNL 中删除来消除 V2 缺失。
不要把网页多电路响应强行改成只返回第 1 个电路。
不要在 P1 未修复前发布 V1.5。
不要覆盖或回滚当前 Agent5 已有未提交工作，除非用户明确要求。
```
