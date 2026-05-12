# GokottaElec V1.5 发布报告

日期：2026-05-13  
发布角色：Agent0  
发布版本：`V1.5` / `1.5.0`  
上一版本：`V1.4`  
发布性质：普通更新，版本号增加 `0.1`。

## 1. 发布内容

本次 V1.5 发布整合 Agent20 Test02 的优化结果：

- Agent4：SVG 渲染器补全 `SIGNAL_SOURCE V2`，BJT 放大器、射随器、BJT LED 开关不再少画已声明信号源。
- Agent5：网页小程序支持 `/api/elec/build` 返回的完整 `circuits[]`，新增多电路结果选择器。
- Agent5：网页 Sample 长标题增加完整标题显示和 tooltip，避免长标题不可辨认。
- Agent5：网页切换电路时同步更新 SVG、IR、ERC 日志和 SVG 下载文件名。
- Agent0：统一版本号为 `V1.5`，同步 README、发布规则、Web 对接文档与 release 包。

## 2. 关键文件

```text
scripts/render-svg.mjs
web-miniapp/gokotta-elec.html
web-miniapp/gokotta-elec.css
web-miniapp/gokotta-elec.js
web-miniapp/README.md
WEB_INTEGRATION_REQUIREMENTS.md
WEB_TEAM_HANDOFF.md
README.md
RELEASE_POLICY.md
AGENT_WORKPLAN.md
VERSION
package.json
launcher/Program.cs
launcher/ConsoleProgram.cs
scripts/elec-cli.mjs
```

## 3. 发布前验证

脚本语法：

```text
node --check scripts/render-svg.mjs
node --check scripts/build-paste.mjs
node --check scripts/elec-cli.mjs
node --check scripts/validate-model-packages.mjs
node --check web-miniapp/gokotta-elec.js
```

结果：全部通过。

核心构建：

```text
node scripts/build-paste.mjs output/agent20-test-2605-02/agent20-test02-20-circuits.cnl output/agent20-test-2605-02/v15-release-candidate
```

结果：

```text
20/20 OK
fallbackCount=0
missingRefdesCount=0
```

官方 Sample：

```text
npm.cmd run build:sample1
npm.cmd run build:sample2
npm.cmd run build:sample3
npm.cmd run build:sample4
npm.cmd run build:sample5
```

结果：全部通过。

模型验证：

```text
npm.cmd run validate:models
```

结果：`OK: components\model-packages.v0.1.json`。

## 4. Agent20 Web 复测

使用 Agent20 Test02 mock Web API：

```text
http://127.0.0.1:51320/gokotta-elec.html
```

复测命令：

```text
output/agent20-test-2605-02/run-web-20-check.cjs
output/agent20-test-2605-02/run-web-multipaste-check.cjs
output/agent20-test-2605-02/run-web-controls-check.cjs
```

结果：

```text
testedCount=20
fallbackCount=0
failedCount=0
publicNetMissingCount=0
textOverflowCount=0
mobileHasHorizontalOverflow=false
copyIrEnabled=true
downloadEnabled=true
copiedIrLooksValid=true
copiedBasicLooksValid=true
copiedFullLooksValid=true
downloadLooksSvg=true
```

补充多电路切换验证：

```text
optionCount=20
selectedIndex=19
summary=20 / 20
svgTitle=AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2
irCircuitId=AGENT20_T02_20_OPAMP_NONINVERTING_GAIN2
downloadDisabled=false
```

证据文件：

```text
output/agent20-test-2605-02/web-20-results.json
output/agent20-test-2605-02/web-multipaste-results.json
output/agent20-test-2605-02/web-controls-results.json
output/agent20-test-2605-02/v15-release-multipaste-switch-results.json
output/agent20-test-2605-02/evidence/v15-release-switch/v15-multipaste-selected-20.png
```

## 5. 发布产物

发布目录：

```text
release/GokottaElec/
```

压缩包：

```text
release/GokottaElec-V1.5.zip
```

发布包要求：

```text
包含 GokottaElec.exe
包含 GokottaElecCLI.exe
包含 LLM 对接文件、器件库、脚本、文档、Sample、web-miniapp
不包含 output/
不包含 launcher/bin/
不包含 launcher/obj/
```

## 6. Git 发布

目标仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

发布步骤：

```text
git commit -m "Release GokottaElec V1.5"
git tag -a v1.5 -m "GokottaElec V1.5"
git push origin main
git push origin v1.5
```

执行状态由最终发布命令输出确认。
