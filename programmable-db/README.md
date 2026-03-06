# Programmable DB MVP (.NET + Vue)

一个起步版可编程数据库微服务：

- 连接管理 API（创建/列出/测试）
- 通用 CRUD API（当前 MVP 实现 SQLite）
- 受限 SQL 查询 API（仅 SELECT、禁止多语句）
- Vue 管理页由 ASP.NET Core `wwwroot` 静态托管

## 运行

```bash
cd programmable-db/backend
dotnet run
```

访问：
- UI: `http://localhost:5000/` 或启动日志中的地址
- Swagger: `/swagger`

## 当前状态

- 已预留多数据库 provider 字段：`sqlite/mysql/sqlserver/postgres`
- 本次 MVP 优先实现 SQLite 执行链路；其他 provider 返回“计划中”提示
