# 采购审批管理系统 - 需求规格说明书

## 1. 项目概述

### 1.1 项目背景
企业采购流程需要规范化管理，通过工作流引擎实现多级审批、流程追踪和自动化处理。

### 1.2 项目目标
基于 **WorkflowCore** 框架构建一套完整的采购审批管理系统，实现：
- 采购申请的全生命周期管理
- 灵活的多级审批工作流
- 实时的审批状态追踪
- 直观的数据统计与展示

### 1.3 技术栈
| 层级 | 技术选型 |
|------|---------|
| 后端框架 | ASP.NET Core 8 |
| 工作流引擎 | WorkflowCore |
| ORM | Entity Framework Core |
| 数据库 | PostgreSQL |
| 前端框架 | React 18 |
| UI 组件库 | Ant Design 5 |
| 容器化 | Docker Compose |

---

## 2. 功能需求

### 2.1 用户管理模块

#### 2.1.1 角色定义
| 角色 | 权限描述 |
|------|---------|
| 普通员工 (Employee) | 创建采购申请、查看自己的申请 |
| 部门经理 (Manager) | 审批部门内申请 (≤5000元) |
| 财务总监 (Finance) | 审批中等金额申请 (≤20000元) |
| 总经理 (Director) | 审批所有金额申请 |
| 系统管理员 (Admin) | 用户管理、系统配置 |

#### 2.1.2 用户功能
- 用户登录/登出（简化版，基于用户名）
- 查看个人信息
- 角色权限控制

### 2.2 采购申请模块

#### 2.2.1 申请创建
- 填写采购物品名称
- 填写采购数量
- 填写预估单价
- 系统自动计算总金额
- 填写采购原因说明
- 选择紧急程度（普通/紧急）

#### 2.2.2 申请管理
- 查看申请列表（支持筛选：状态、日期、金额范围）
- 查看申请详情
- 撤回待审批的申请
- 重新提交被拒绝的申请

### 2.3 审批工作流模块 (WorkflowCore)

#### 2.3.1 审批流程规则
```
金额 ≤ 5,000 元:
  └─> 部门经理审批 ──> 完成

金额 5,001 ~ 20,000 元:
  └─> 部门经理审批 ──> 财务总监审批 ──> 完成

金额 > 20,000 元:
  └─> 部门经理审批 ──> 财务总监审批 ──> 总经理审批 ──> 完成
```

#### 2.3.2 审批操作
- 同意 (Approve)：进入下一审批节点或完成
- 拒绝 (Reject)：流程终止，申请被拒绝
- 退回修改 (Return)：退回申请人修改后重新提交

#### 2.3.3 工作流特性
- 审批超时提醒（可配置）
- 审批记录留痕
- 支持审批意见填写

### 2.4 通知模块

#### 2.4.1 系统内通知
- 待审批提醒
- 审批结果通知
- 申请状态变更通知

### 2.5 统计报表模块

#### 2.5.1 仪表盘展示
- 待办事项数量
- 本月采购总额
- 申请状态分布（饼图）
- 近期采购趋势（折线图）

#### 2.5.2 数据统计
- 按部门统计采购金额
- 按状态统计申请数量
- 审批效率统计（平均审批时长）

---

## 3. 非功能需求

### 3.1 性能要求
- 页面加载时间 < 2秒
- API 响应时间 < 500ms

### 3.2 安全要求
- 接口权限校验
- 敏感操作日志记录

### 3.3 可用性要求
- 响应式设计，支持桌面端浏览器
- 清晰的操作反馈

---

## 4. 数据模型

### 4.1 核心实体

```
User (用户)
├── Id: GUID
├── Username: string
├── DisplayName: string
├── Role: enum
├── Department: string
└── CreatedAt: DateTime

PurchaseRequest (采购申请)
├── Id: GUID
├── RequestNumber: string (自动生成)
├── ApplicantId: GUID (FK -> User)
├── ItemName: string
├── Quantity: int
├── UnitPrice: decimal
├── TotalAmount: decimal (计算字段)
├── Reason: string
├── Urgency: enum (Normal/Urgent)
├── Status: enum
├── WorkflowId: string
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

ApprovalRecord (审批记录)
├── Id: GUID
├── RequestId: GUID (FK -> PurchaseRequest)
├── ApproverId: GUID (FK -> User)
├── Action: enum (Approve/Reject/Return)
├── Comment: string
├── ApprovalLevel: int
└── CreatedAt: DateTime

Notification (通知)
├── Id: GUID
├── UserId: GUID (FK -> User)
├── Title: string
├── Content: string
├── IsRead: bool
└── CreatedAt: DateTime
```

### 4.2 状态枚举

```
RequestStatus:
├── Draft (草稿)
├── Pending (待审批)
├── ManagerApproved (经理已批)
├── FinanceApproved (财务已批)
├── Approved (已通过)
├── Rejected (已拒绝)
├── Returned (已退回)
└── Cancelled (已撤销)
```

---

## 5. 接口设计

### 5.1 API 端点

| 方法 | 端点 | 描述 |
|------|------|------|
| POST | /api/auth/login | 用户登录 |
| GET | /api/users/me | 获取当前用户信息 |
| GET | /api/users | 获取用户列表 (Admin) |
| GET | /api/requests | 获取采购申请列表 |
| POST | /api/requests | 创建采购申请 |
| GET | /api/requests/{id} | 获取申请详情 |
| PUT | /api/requests/{id} | 更新申请 |
| POST | /api/requests/{id}/submit | 提交申请 |
| POST | /api/requests/{id}/cancel | 撤销申请 |
| GET | /api/approvals/pending | 获取待审批列表 |
| POST | /api/approvals/{id}/approve | 同意审批 |
| POST | /api/approvals/{id}/reject | 拒绝审批 |
| POST | /api/approvals/{id}/return | 退回修改 |
| GET | /api/notifications | 获取通知列表 |
| PUT | /api/notifications/{id}/read | 标记已读 |
| GET | /api/statistics/dashboard | 获取仪表盘数据 |

---

## 6. 界面原型

### 6.1 页面清单
1. **登录页** - 用户选择/登录
2. **仪表盘** - 数据概览、待办事项
3. **采购申请列表** - 申请管理
4. **创建/编辑申请** - 表单页面
5. **申请详情** - 详情与审批历史
6. **审批工作台** - 待审批列表
7. **通知中心** - 消息列表
8. **用户管理** - (Admin) 用户列表

---

## 7. 交付标准

### 7.1 Docker 交付
```bash
docker compose up
```
- 前端访问: http://localhost:3000
- API 访问: http://localhost:5000
- 数据库: PostgreSQL (内部)

### 7.2 预置数据
系统启动时自动初始化：
- 5个测试用户（每种角色各一个）
- 若干示例采购申请

---

## 8. 确认清单

- [ ] 技术栈确认
- [ ] 功能范围确认
- [ ] 审批流程规则确认
- [ ] UI 设计风格确认

---

**请确认以上需求是否符合预期，确认后将进入 `/plan` 阶段。**
