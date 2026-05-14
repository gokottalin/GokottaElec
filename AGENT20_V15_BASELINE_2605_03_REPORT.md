# Agent20 V1.5 基线测试报告 2605-03

日期：2026-05-14  
执行角色：Agent20  
测试目录：`E:\Project\2605_Elec\output\agent20-v15-baseline-2605-03\`  
测试目标：按 `AGENT_WORKPLAN.md` 中 Agent20 职责，对当前 V1.5 工作区建立基线回归参照。  

## 1. 结论

本轮基线测试通过。

```text
P0：0
P1：0
P2：0
P3：0
阻断发布：否
```

未发现新的 Agent20 问题项，因此本轮不新增 `AGENT20_ISSUES_*.md`。

## 2. 覆盖范围

本轮覆盖 Agent20 最低测试范围：

```text
1. 官方 5 个 Sample：通过
2. 多电路一次性粘贴：20/20 通过
3. CNL parser 失败路径：通过，非法 refdes 被拒绝
4. ERC WARNING：通过，REFDES_PREFIX warning 可观察且不阻断构建
5. ERC ERROR：通过，REQUIRED_PARAMETER error 阻断构建
6. SVG refdes 完整性：通过，missingRefdesCount=0
7. Web 单电路展示：通过
8. Web 多电路展示与第 20 个电路切换：通过
9. LLM handoff 复制：基础/完整均通过
10. SVG 下载：通过，文件名跟随当前电路 ID
11. 移动端布局：通过，无页面级横向溢出
12. 发布前 smoke test：通过
```

## 3. 核心链路结果

执行命令：

```powershell
node output\agent20-v15-baseline-2605-03\run-core-baseline.mjs
```

结果摘要：

```text
syntax checks：7/7 通过
validate models：通过
official samples：5/5 通过
generatedCircuitCount=20
build-paste：20/20 通过
fallbackCount=0
missingRefdesCount=0
parserInvalidRejected=true
ercWarningObserved=true
ercRequiredParameterObserved=true
```

核心结果文件：

```text
output/agent20-v15-baseline-2605-03/core-baseline-results.json
output/agent20-v15-baseline-2605-03/agent20-v15-20-circuits.cnl
output/agent20-v15-baseline-2605-03/twenty-build/summary.txt
output/agent20-v15-baseline-2605-03/logs/
```

## 4. Web 自动化结果

执行命令：

```powershell
$env:NODE_PATH='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
node output\agent20-v15-baseline-2605-03\run-web-baseline.mjs
```

结果摘要：

```text
single.irCircuitId=AGENT20_V15_01_VOLTAGE_DIVIDER_5V_HALF
multi.optionCount=20
multi.selectedIndex=19
multi.summary=20 / 20
multi.irCircuitId=AGENT20_V15_20_OPAMP_NONINVERTING_GAIN_2
desktop.horizontalOverflow=false
desktop.textOverflowCount=0
mobile.horizontalOverflow=false
mobile.textOverflowCount=0
copiedIrLooksValid=true
copiedBasicLooksValid=true
copiedFullLooksValid=true
downloadLooksSvg=true
downloadFilename=AGENT20_V15_20_OPAMP_NONINVERTING_GAIN_2.svg
```

Web 结果文件：

```text
output/agent20-v15-baseline-2605-03/web-baseline-results.json
output/agent20-v15-baseline-2605-03/evidence/web-single-first-circuit.png
output/agent20-v15-baseline-2605-03/evidence/web-multi-selected-20.png
output/agent20-v15-baseline-2605-03/evidence/web-mobile-first-circuit.png
output/agent20-v15-baseline-2605-03/evidence/downloaded-selected-20.svg
```

## 5. 执行备注

1. 本轮遵守 Agent20 边界，只新增测试脚本、测试输入、日志、截图证据和本报告，未修改实现文件。
2. 当前工作区没有历史 `output\agent20-test-2605-02\` 目录，因此本轮基于现有 `output\twenty-circuits\` 生成新的 Agent20 V1.5 基线输入。
3. Codex in-app Browser 连接在本轮超时，Web 自动化改用同一工作区的 Playwright + 系统 Chrome 执行，并生成截图证据。
