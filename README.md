# 采购审批管理系统

基于 **WorkflowCore** 工作流引擎的采购审批管理系统，已补齐生产向关键安全基线：统一认证授权、主体校验、输入边界校验、结构化日志、请求关联 ID 与敏感操作审计。

## 🚀 快速启动

### 1) 准备环境变量

将项目根目录的 `.env.example` 复制为 `.env`，并填入你自己的安全配置：

```bash
cp .env.example .env
```

必须至少配置：

- `POSTGRES_PASSWORD`
- `JWT_SECRET_KEY`（长度至少 32）
- `SEED_USER_PASSWORD`

### 2) 启动服务

```bash
docker compose up --build -d
```

## 🌐 端口与访问地址（统一口径）

- 容器内部端口（Backend）：`5000`
- 宿主机对外端口（Backend）：`5001`

### 访问地址

| 服务 | 地址 |
|------|------|
| 前端界面 | http://localhost:3000 |
| 后端 API | http://localhost:5001 |
| API 文档 | http://localhost:5001/swagger |

## 🔐 认证与鉴权说明

- 登录接口：`POST /api/auth/login`
- 登录成功后返回 `token`（JWT Bearer）。
- 前端自动在请求头携带 `Authorization: Bearer <token>`。
- 业务接口默认要求已认证（401）；越权操作返回 403。

## 👥 测试账号

系统默认会初始化以下账号（用户名固定）：

- `zhangsan`
- `lisi`
- `wangwu`
- `zhaoliu`
- `admin`

密码不再写死在代码和文档中，统一来自 `.env` 的 `SEED_USER_PASSWORD`。

## 📋 功能特性

- 采购申请全生命周期管理（创建、编辑、提交、撤销）
- 基于 WorkflowCore 的多级审批流程
- 审批规则分级（经理 / 财务 / 总经理）
- 通知中心与仪表盘
- 审计日志与请求关联 ID（`X-Correlation-ID`）

## 🛠️ 技术栈

### 后端

- ASP.NET Core 8
- WorkflowCore 3.9
- Entity Framework Core 8
- PostgreSQL 16
- JWT Bearer Authentication

### 前端

- React 18
- TypeScript 5
- Ant Design 5
- Vite 5
- Zustand

### 部署

- Docker & Docker Compose
- Nginx

## 📁 项目结构

```text
├── backend/
│   ├── Configuration/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Extensions/
│   ├── Middleware/
│   ├── Models/
│   ├── Services/
│   ├── Utils/
│   └── Workflows/
├── frontend/
│   └── src/
├── docs/
├── .env.example
└── docker-compose.yml
```

## 📄 主要接口

- `POST /api/auth/login`：用户登录并获取 JWT
- `GET /api/requests`：获取申请列表（按当前登录用户范围）
- `POST /api/requests`：创建申请
- `PUT /api/requests/{id}`：更新申请（仅申请人本人/管理员）
- `POST /api/requests/{id}/submit`：提交申请（仅申请人本人/管理员）
- `POST /api/requests/{id}/cancel`：撤销申请（仅申请人本人/管理员）
- `GET /api/approvals/pending`：获取待审批列表
- `POST /api/approvals/{id}/approve`：审批通过
- `GET /api/statistics/dashboard`：获取仪表盘

## 🔧 本地开发

### 后端

```bash
cd backend
dotnet restore
dotnet run
```

### 前端

```bash
cd frontend
npm install
npm run dev
```

## 📝 License

MIT
