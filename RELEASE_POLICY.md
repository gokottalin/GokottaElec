# GokottaElec 版本规则

当前软件版本：`V1.1`

目标 GitHub 仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 版本递增规则

- 普通功能更新、界面优化、兼容性增强、Sample 更新：版本号增加 `0.1`
  - 示例：`V1.0` -> `V1.1` -> `V1.2`
- 重大更新、架构重构、CNL/IR 重大不兼容变化、核心渲染引擎大改：版本号增加 `1.0`
  - 示例：`V1.0` -> `V2.0`

## 每次发版必须同步

每次版本更新必须同步修改：

- `VERSION`
- `package.json` 中的 `version` 与 `gokottaVersion`
- `README.md`
- `release/GokottaElec.zip`
- 桌面程序标题中的版本号

如果变更影响网页端，还必须同步：

- `WEB_INTEGRATION_REQUIREMENTS.md`
- `web-miniapp/`
- GokottaMaker 中的 `/api/elec/*` 接口

## GitHub Release 建议

源码推送到 GitHub 仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

可执行文件和 zip 发布包建议放到 GitHub Releases，不建议直接提交到源码仓库。

Release Tag 建议使用：

```text
v1.0
v1.1
v2.0
```
