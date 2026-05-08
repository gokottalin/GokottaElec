# GokottaElec 网页小程序骨架

这个目录是给 `GokottaMaker` 集成使用的最小前端小程序，不是完整网站。

建议复制到 GokottaMaker：

```text
D:\Project\26-WEB\GokottaMaker\tools\gokotta-elec.html
D:\Project\26-WEB\GokottaMaker\tools\gokotta-elec.js
D:\Project\26-WEB\GokottaMaker\styles\gokotta-elec.css
```

也可以保持当前文件名直接放入网站根目录。

前端默认调用：

```http
GET  /api/elec/samples
POST /api/elec/build
```

如果后端接口还没有接入，页面会显示“接口未接入”的诊断信息。

后端接口详细要求见：

```text
../WEB_INTEGRATION_REQUIREMENTS.md
```
