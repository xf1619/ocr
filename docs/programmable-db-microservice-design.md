# 可编程数据库微服务设计方案（.NET + Vue）

## 1. 目标与范围

构建一个“数据库能力统一网关”微服务：

- 所有数据库增删改查通过统一 API 完成。
- 支持多数据库：SQLite、MySQL、SQL Server、PostgreSQL。
- 提供前端 UI（Vue），并由后端静态资源托管。
- 支持表设计、连接管理、查询执行、自定义 API、事务逻辑、脚本扩展、模型代码生成、权限与监控。

> 核心理念：**元数据驱动 + 插件式驱动层 + 可观测与可审计**。

---

## 2. 总体架构

```text
┌───────────────────────────────┐
│         Vue Admin UI          │
│ 连接管理/表设计/API配置/监控页面 │
└──────────────┬────────────────┘
               │ HTTPS/REST
┌──────────────▼───────────────────────────────────────────────────────┐
│                         ASP.NET Core Host                            │
│                                                                       │
│  API网关层                                                           │
│  ├─ CRUD Controller                                                   │
│  ├─ Query Controller                                                  │
│  ├─ Dynamic API Controller                                            │
│  └─ Auth Controller                                                   │
│                                                                       │
│  领域服务层                                                           │
│  ├─ Connection Manager (连接池 + 健康检查)                             │
│  ├─ Table Designer (Schema管理 + 迁移)                               │
│  ├─ CRUD Engine (元数据驱动 SQL 构造)                                 │
│  ├─ Query Engine (参数化查询 / SQL白名单)                              │
│  ├─ API Manager (自定义API定义/发布)                                  │
│  ├─ Logic Engine (事务编排)                                            │
│  ├─ Script Engine (脚本执行沙箱)                                       │
│  ├─ Model Generator (多语言代码模板)                                   │
│  ├─ Permission Service (RBAC + ABAC)                                  │
│  └─ Audit/Monitor Service                                              │
│                                                                       │
│  基础设施层                                                           │
│  ├─ Provider Adapters: SQLite/MySQL/SQLServer/PostgreSQL             │
│  ├─ EF Core + Dapper (混合策略)                                        │
│  ├─ Redis缓存(可选)                                                    │
│  ├─ OpenTelemetry + Prometheus + Serilog                              │
│  └─ Metadata DB(存储连接/API定义/脚本/权限/审计日志)                  │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 3. 模块设计（对应你的模块清单）

### 3.1 连接管理（Connection Manager）

**能力**
- 管理多个数据源（dev/test/prod 分组）。
- 连接池参数配置（最小/最大池、超时、重试策略）。
- 连接健康检查与连通性测试。
- 凭据加密存储（建议使用 Data Protection + KMS）。

**实现建议**
- 使用 `DbProviderFactory` + Provider Adapter 抽象不同数据库。
- 每个连接定义 `DataSourceId`，运行时按租户/环境解析。
- 提供连接限流，防止单数据库被压垮。

### 3.2 表管理（Table Designer）

**能力**
- UI 上可视化建表：字段、类型、主键、唯一索引、外键、默认值。
- 版本化 schema（v1/v2）。
- 一键生成迁移 SQL 并执行。

**实现建议**
- 维护统一元模型：`TableDefinition / ColumnDefinition / IndexDefinition`。
- 根据数据库方言生成 DDL（Dialect Translator）。
- 禁止直接拼接 DDL，统一模板 + 参数。

### 3.3 CRUD 引擎（Auto CRUD）

**能力**
- 基于表元数据自动生成增删改查 API。
- 支持分页、排序、过滤、字段选择。
- 支持乐观锁（`rowversion`/`xmin`/时间戳字段）。

**实现建议**
- 对外统一请求 DSL（如 filter/sort/page），内部映射到参数化 SQL。
- 批量写入支持事务与幂等键。

### 3.4 查询引擎（Query Engine）

**能力**
- 表查询构建器（无 SQL 用户）。
- SQL 执行器（高级用户）。
- 执行计划分析、慢查询标记。

**安全建议**
- SQL 执行默认只读；写操作需要高权限策略。
- 必须参数化，禁止多语句执行（防注入）。
- 引入 SQL 白名单/关键词黑名单双层策略。

### 3.5 API 管理（自定义 API）

**能力**
- 在 UI 中配置 API：路径、方法、输入模型、执行动作、返回映射。
- 发布版本（v1/v2）与灰度开关。
- 自动生成 OpenAPI 文档。

**执行模型建议**
- API = `Trigger + Pipeline Steps + Response Mapping`
- Steps 可包含：查询、CRUD、脚本、事务块、外部 HTTP 调用。

### 3.6 逻辑引擎（事务）

**能力**
- 多步骤业务流程编排。
- 单数据源事务（ACID）。
- 分布式场景支持 Saga（补偿事务）。

**实现建议**
- 提供 `Begin/Commit/Rollback` 编排器。
- 在 SQL Server/MySQL/PostgreSQL 场景优先本地事务。
- 跨库跨服务流程使用 Outbox + 事件总线。

### 3.7 脚本引擎（业务脚本）

**能力**
- API 前置/后置脚本，可处理参数、校验、结果加工。
- 支持脚本版本与回滚。

**技术建议**
- .NET 内建议优先 Roslyn C# Script（受限 API）。
- 强沙箱：超时、内存限制、禁 IO/网络（除白名单）。
- 每次脚本执行记录审计日志。

### 3.8 模型生成（代码生成）

**能力**
- 根据表定义生成多语言模型类：C#、Java、TypeScript、Go、Python。
- 可选生成 DTO、Repository 接口、OpenAPI Client。

**实现建议**
- 使用模板引擎（Scriban/Handlebars）。
- 生成策略可配置：命名风格、可空性、注解风格。

### 3.9 权限系统（API 权限）

**能力**
- 用户/角色/权限（RBAC）。
- 行级权限与列级脱敏（ABAC）。
- API token、JWT、OAuth2 对接。

**关键点**
- 权限判定下沉到执行层，不仅在 Controller 层。
- 所有管理操作要求双日志（行为日志 + 审计日志）。

### 3.10 监控日志

**能力**
- 请求链路追踪（TraceId）。
- API 调用次数、耗时、错误率。
- 慢 SQL 与高频表预警。

**实现建议**
- OpenTelemetry + Prometheus + Grafana。
- Serilog 结构化日志 + Elasticsearch（可选）。

---

## 4. 技术栈建议

### 后端（.NET）
- .NET 8 + ASP.NET Core Web API。
- EF Core（Schema/迁移）+ Dapper（高性能查询）。
- FluentValidation、MediatR、Polly（重试/熔断）。
- Hangfire/Quartz（异步任务、定时任务）。

### 前端（Vue）
- Vue 3 + TypeScript + Vite。
- Element Plus / Naive UI。
- Pinia + Vue Router + Axios。
- Monaco Editor（SQL/脚本编辑器）。

### 托管方式
- 前端打包后输出到 `wwwroot`，由 ASP.NET Core 提供静态资源。
- 同域部署，减少跨域与网关复杂度。

---

## 5. 核心数据模型（元数据）

建议使用独立 `metadata_db` 保存系统配置，而不是与业务库耦合。

主要表（示例）：

- `ds_connection`：连接配置、驱动类型、加密凭据、池参数。
- `meta_table` / `meta_column` / `meta_index`：表设计元数据。
- `meta_api` / `meta_api_step`：自定义 API 定义。
- `meta_script`：脚本内容、版本、沙箱策略。
- `meta_permission` / `meta_role` / `meta_user_role`。
- `audit_log` / `api_metrics` / `sql_profile`。

---

## 6. API 设计草案

### 6.1 连接管理
- `POST /api/admin/datasources`
- `GET /api/admin/datasources`
- `POST /api/admin/datasources/{id}/test`

### 6.2 表管理
- `POST /api/admin/tables/design`
- `POST /api/admin/tables/{id}/migrate`
- `GET /api/admin/tables/{id}`

### 6.3 通用 CRUD
- `POST /api/data/{datasource}/{table}`
- `GET /api/data/{datasource}/{table}`
- `PUT /api/data/{datasource}/{table}/{id}`
- `DELETE /api/data/{datasource}/{table}/{id}`

### 6.4 查询执行
- `POST /api/query/{datasource}/builder`
- `POST /api/query/{datasource}/sql`（受限）

### 6.5 API 管理
- `POST /api/admin/custom-apis`
- `POST /api/admin/custom-apis/{id}/publish`
- `POST /api/runtime/{apiPath}`

---

## 7. 安全与治理

- 参数化 SQL + 查询超时 + 行数限制。
- 默认只读连接，写操作使用独立凭据。
- 数据脱敏（手机号/身份证/邮箱）与字段级加密。
- 所有动态配置（API、脚本）启用审批流（草稿→审核→发布）。
- 多租户隔离（租户 ID + 数据源隔离）。

---

## 8. 分阶段落地路线（建议）

### Phase 1（MVP，4~6 周）
- 多数据库连接管理。
- 表管理（基础建表）。
- 通用 CRUD + 查询构建器。
- 基础权限（RBAC）+ API 日志。

### Phase 2（增强，6~8 周）
- 自定义 API 流程编排。
- 事务逻辑引擎。
- 模型生成（C#/TS 先行）。
- 监控面板与慢 SQL 分析。

### Phase 3（企业级，8+ 周）
- 脚本引擎沙箱。
- Saga/Outbox 分布式能力。
- 审批流、灰度发布、全链路审计。

---

## 9. 目录结构建议（Monorepo）

```text
/programmable-db
  /backend
    /src
      ApiHost
      Core
      Infrastructure
      Providers.SqlServer
      Providers.MySql
      Providers.Postgres
      Providers.Sqlite
      CodeGen
      ScriptRuntime
  /frontend
    /src
      pages
      components
      stores
  /docs
```

---

## 10. 风险与规避

- **风险：动态 SQL 与脚本带来安全面扩大** → 通过沙箱、白名单、审批流降低风险。
- **风险：多数据库方言差异** → 通过 Dialect 抽象层 + 自动化方言测试。
- **风险：平台复杂度高** → 按 Phase 分阶段交付，优先高频核心功能。

---

## 11. 你这个需求的推荐“第一版”

建议第一版聚焦：

1. 支持 SQL Server + PostgreSQL（你重点提到 SQL Server，优先保障）。
2. 完成连接管理、表管理、CRUD、查询、权限、日志。
3. 前端实现三个主页面：连接管理、表设计、API 测试。
4. 预留脚本引擎与模型生成接口，不在第一版全部做完。

这样可以在较短周期内上线可用版本，再逐步扩展到 MySQL/SQLite 和高级流程能力。
