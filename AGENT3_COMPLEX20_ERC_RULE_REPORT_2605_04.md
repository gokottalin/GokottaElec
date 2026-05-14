# Agent3 Complex20 ERC Rule Report 2605-04

日期：2026-05-15  
执行角色：Agent3  
任务来源：`AGENT0_COMPLEX20_FIX_AND_ASSIGNMENT_2605_04.md`

## 1. 修改摘要

本轮将 Complex20 中暴露的“声明 net 但未被 `connections[]` 使用”问题补入 ERC 网络风险检查。

修改文件：

```text
scripts/erc-rules.mjs
scripts/erc-check.mjs
docs/erc-rules-v0.1.md
```

## 2. 规则说明

规则：`UNUSED_NET`

触发条件：

```text
IR 的 nets[] 中声明了某个 net，但 connections[] 中没有任何 connection.net 引用该 net。
```

诊断等级：

```text
WARNING
```

诊断示例：

```text
WARNING: UNUSED_NET [N_UNUSED]: Net N_UNUSED is declared but not used by any connection
```

说明：

```text
1. 错误码保持为既有 UNUSED_NET，避免破坏 Web diagnostics 或既有脚本判断。
2. ERC 现在会区分“声明但未被 connection 使用”和“connection 使用了 net 但没有有效端子”。
3. 该规则不改变 ok 语义；WARNING 不阻断 build。
```

## 3. Web Diagnostics 影响

```text
输出字段未变化。
level/code/message/target 结构未变化。
Web 端只会看到更明确的 UNUSED_NET 文案。
无需新增 API 字段。
```

## 4. 验证结果

语法检查：

```powershell
node --check scripts\erc-rules.mjs
node --check scripts\erc-check.mjs
node --check scripts\elec-cli.mjs
```

结果：全部通过。

正例/反例：

```text
最小坏例：N_UNUSED 只在 nets[] 中声明，未被 connections[] 使用。
结果：输出 WARNING UNUSED_NET [N_UNUSED]，文案为 declared but not used by any connection。
```

Agent3 基线：

```powershell
node scripts\validate-model-packages.mjs
node scripts\elec-cli.mjs build samples\Sample-01-voltage-divider.txt output\agent3-sample-01
node scripts\elec-cli.mjs build samples\Sample-02-npn-low-side-switch.txt output\agent3-sample-02
node scripts\elec-cli.mjs build samples\Sample-05-opamp-noninverting-amplifier.txt output\agent3-sample-05
```

结果：全部通过，3 个样例 ERC 均为 `OK`。

Complex20 回归：

```powershell
node scripts\generate-20-circuit-gallery.mjs
$base='C:\Users\10731\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
$env:NODE_PATH="$base;$base\.pnpm\node_modules"
node output\agent20-complex-20-2605-04\inspect-twenty-circuits.mjs
```

结果：

```text
passed=true
issueCounts={}
20/20 ERC scan: ErrorCount=0, WarningCount=0
```

## 5. Agent20 回归建议

后续 Agent20 可增加一个 ERC 负例夹具：

```text
nets[] 中包含 N_UNUSED；
connections[] 不引用 N_UNUSED；
期望 diagnostics 中出现 WARNING UNUSED_NET，target=N_UNUSED；
build 仍保持 ok=true。
```

