# 采购审批管理系统

基于 **WorkflowCore** 工作流引擎的企业级采购审批管理系统。

## 🚀 快速启动

```bash
docker compose up -d
```

### 启动成功标志

后端启动成功后，日志会显示：

```
╔═══════════════════════════════════════════════════════════╗
║          采购审批管理系统 - Backend API                    ║
╠═══════════════════════════════════════════════════════════╣
║  ✅ Startup Success                                        ║
║  🌐 API: http://localhost:5000                             ║
║  📚 Swagger: http://localhost:5000/swagger                 ║
╚═══════════════════════════════════════════════════════════╝
```

### 访问地址

| 服务 | 地址 |
|------|------|
| **前端界面** | http://localhost:3000 |
| **后端 API** | http://localhost:5001 |
| **API 文档** | http://localhost:5001/swagger |

## 📋 功能特性

### 核心功能
- ✅ 采购申请全生命周期管理（创建、编辑、提交、撤回）
- ✅ 基于 WorkflowCore 的多级审批工作流
- ✅ 灵活的金额分级审批规则
- ✅ 实时审批状态追踪
- ✅ 系统内通知中心
- ✅ 数据统计仪表盘

### 审批规则
| 金额范围 | 审批流程 |
|---------|---------|
| ≤ 5,000 元 | 部门经理 |
| 5,001 ~ 20,000 元 | 部门经理 → 财务总监 |
| > 20,000 元 | 部门经理 → 财务总监 → 总经理 |

## 👥 测试账号

登录方式为账号密码登录，测试环境默认密码均为 `123456`。

| 用户名 | 密码 | 姓名 | 角色 | 权限说明 |
|-------|------|------|------|---------|
| zhangsan | 123456 | 张三 | 普通员工 | 创建和管理采购申请 |
| lisi | 123456 | 李四 | 部门经理 | 审批第1级（≤5000元） |
| wangwu | 123456 | 王五 | 财务总监 | 审批第2级（≤20000元） |
| zhaoliu | 123456 | 赵六 | 总经理 | 审批第3级（所有金额） |
| admin | 123456 | 系统管理员 | 管理员 | 用户管理 |

## 🛠️ 技术栈

### 后端
- ASP.NET Core 8
- WorkflowCore 3.10 (工作流引擎)
- Entity Framework Core 8
- PostgreSQL 16

### 前端
- React 18
- TypeScript 5
- Ant Design 5
- @ant-design/charts (数据可视化)
- Vite 5
- Zustand (状态管理)

### 部署
- Docker & Docker Compose
- Nginx (前端代理)

## 📁 项目结构

```
├── backend/                # 后端项目
│   ├── Controllers/        # API 控制器
│   ├── Models/            # 数据模型
│   ├── Services/          # 业务服务
│   ├── Workflows/         # 工作流定义
│   ├── Data/              # 数据库上下文
│   └── DTOs/              # 数据传输对象
├── frontend/              # 前端项目
│   ├── src/
│   │   ├── api/          # API 接口
│   │   ├── pages/        # 页面组件
│   │   ├── layouts/      # 布局组件
│   │   ├── store/        # 状态管理
│   │   ├── theme/        # 主题配置
│   │   └── types/        # 类型定义
│   └── public/           # 静态资源
├── docs/                  # 项目文档
│   ├── Requirements.md   # 需求规格
│   ├── Roadmap.md        # 开发路线图
│   ├── DesignSpec.md     # 设计规范
│   └── SelfTestReport.md # 自测报告
└── docker-compose.yml    # Docker 编排
```

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

## 📄 API 文档

启动后访问 Swagger UI: http://localhost:5001/swagger

主要接口：
- `POST /api/auth/login` - 用户登录
- `GET /api/requests` - 获取申请列表
- `POST /api/requests` - 创建申请
- `POST /api/requests/{id}/submit` - 提交申请
- `GET /api/approvals/pending` - 获取待审批列表
- `POST /api/approvals/{id}/approve` - 同意审批
- `GET /api/statistics/dashboard` - 获取仪表盘数据

## 📝 License

MIT
