// 用户角色
export enum UserRole {
  Employee = 0,
  Manager = 1,
  Finance = 2,
  Director = 3,
  Admin = 4,
}

export const UserRoleLabels: Record<UserRole, string> = {
  [UserRole.Employee]: '普通员工',
  [UserRole.Manager]: '部门经理',
  [UserRole.Finance]: '财务总监',
  [UserRole.Director]: '总经理',
  [UserRole.Admin]: '系统管理员',
}

// 申请状态
export enum RequestStatus {
  Draft = 0,
  Pending = 1,
  ManagerApproved = 2,
  FinanceApproved = 3,
  Approved = 4,
  Rejected = 5,
  Returned = 6,
  Cancelled = 7,
}

export const RequestStatusLabels: Record<RequestStatus, string> = {
  [RequestStatus.Draft]: '草稿',
  [RequestStatus.Pending]: '待审批',
  [RequestStatus.ManagerApproved]: '经理已批',
  [RequestStatus.FinanceApproved]: '财务已批',
  [RequestStatus.Approved]: '已通过',
  [RequestStatus.Rejected]: '已拒绝',
  [RequestStatus.Returned]: '已退回',
  [RequestStatus.Cancelled]: '已撤销',
}

export const RequestStatusColors: Record<RequestStatus, string> = {
  [RequestStatus.Draft]: 'default',
  [RequestStatus.Pending]: 'processing',
  [RequestStatus.ManagerApproved]: 'processing',
  [RequestStatus.FinanceApproved]: 'processing',
  [RequestStatus.Approved]: 'success',
  [RequestStatus.Rejected]: 'error',
  [RequestStatus.Returned]: 'warning',
  [RequestStatus.Cancelled]: 'default',
}

// 紧急程度
export enum Urgency {
  Normal = 0,
  Urgent = 1,
}

export const UrgencyLabels: Record<Urgency, string> = {
  [Urgency.Normal]: '普通',
  [Urgency.Urgent]: '紧急',
}

// 审批操作
export enum ApprovalAction {
  Approve = 0,
  Reject = 1,
  Return = 2,
}

export const ApprovalActionLabels: Record<ApprovalAction, string> = {
  [ApprovalAction.Approve]: '同意',
  [ApprovalAction.Reject]: '拒绝',
  [ApprovalAction.Return]: '退回',
}

export enum DelegationStatus {
  Active = 0,
  Revoked = 1,
  Expired = 2,
}

export const DelegationStatusLabels: Record<DelegationStatus, string> = {
  [DelegationStatus.Active]: '生效中',
  [DelegationStatus.Revoked]: '已撤销',
  [DelegationStatus.Expired]: '已过期',
}

export const DelegationStatusColors: Record<DelegationStatus, string> = {
  [DelegationStatus.Active]: 'success',
  [DelegationStatus.Revoked]: 'default',
  [DelegationStatus.Expired]: 'warning',
}

export interface Delegation {
  id: string
  delegatorId: string
  delegatorName: string
  delegateeId: string
  delegateeName: string
  startDate: string
  endDate: string
  reason: string
  status: string
  statusValue: DelegationStatus
  createdAt: string
  updatedAt?: string
}

export interface CreateDelegationDto {
  delegateeId: string
  startDate: string
  endDate: string
  reason?: string
}

export interface ApproverOption {
  id: string
  displayName: string
  role: string
  department: string
}

// 用户
export interface User {
  id: string
  username: string
  displayName: string
  role: string
  roleValue: UserRole
  department: string
  createdAt?: string
  token?: string
  tokenType?: string
  expiresAt?: string
}

// 采购申请
export interface PurchaseRequest {
  id: string
  requestNumber: string
  applicantId: string
  applicantName: string
  applicantDepartment: string
  itemName: string
  quantity: number
  unitPrice: number
  totalAmount: number
  reason: string
  urgency: string
  urgencyValue: Urgency
  status: string
  statusValue: RequestStatus
  currentApprovalLevel: number
  workflowId?: string
  createdAt: string
  updatedAt: string
  approvalRecords?: ApprovalRecord[]
}

// 审批记录
export interface ApprovalRecord {
  id: string
  approverId: string
  approverName: string
  actualOperatorId?: string
  actualOperatorName?: string
  action: string
  actionValue: ApprovalAction
  comment: string
  approvalLevel: number
  createdAt: string
}

// 通知
export interface Notification {
  id: string
  title: string
  content: string
  requestId?: string
  isRead: boolean
  createdAt: string
}

// 仪表盘数据
export interface DashboardData {
  pendingApprovalCount: number
  myRequestCount: number
  monthlyAmount: number
  approvalRate: number
  statusDistribution: { status: string; count: number }[]
  dailyTrend: { date: string; count: number; amount: number }[]
  todoItems: {
    requestId: string
    requestNumber: string
    itemName: string
    applicantName: string
    totalAmount: number
    createdAt: string
  }[]
}

// 创建申请 DTO
export interface CreateRequestDto {
  itemName: string
  quantity: number
  unitPrice: number
  reason?: string
  urgency: Urgency
}

// 更新申请 DTO
export interface UpdateRequestDto {
  itemName: string
  quantity: number
  unitPrice: number
  reason?: string
  urgency: Urgency
}
