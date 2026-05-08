# GokottaElec Git 上传指南

目标仓库：

```text
https://github.com/gokottalin/GokottaElec.git
```

## 首次上传

在 `D:\Project\2605-Elec` 中执行：

```powershell
git init
git branch -M main
git remote add origin https://github.com/gokottalin/GokottaElec.git
git add .
git commit -m "Release GokottaElec V1.0"
git push -u origin main
```

## 后续普通更新

普通更新版本号增加 `0.1`：

```text
V1.0 -> V1.1 -> V1.2
```

提交示例：

```powershell
git add .
git commit -m "Update GokottaElec to V1.1"
git push
```

## 后续重大更新

重大更新版本号增加 `1.0`：

```text
V1.0 -> V2.0
```

提交示例：

```powershell
git add .
git commit -m "Release GokottaElec V2.0"
git push
```

## 发布包

`dist/` 和 `release/` 已被 `.gitignore` 忽略。建议把：

```text
release/GokottaElec.zip
```

上传到 GitHub Releases，而不是提交到源码仓库。
