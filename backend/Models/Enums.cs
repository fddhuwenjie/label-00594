namespace PurchaseApproval.Models;

/// <summary>
/// 用户角色
/// </summary>
public enum UserRole
{
    /// <summary>普通员工</summary>
    Employee = 0,
    /// <summary>部门经理</summary>
    Manager = 1,
    /// <summary>财务总监</summary>
    Finance = 2,
    /// <summary>总经理</summary>
    Director = 3,
    /// <summary>系统管理员</summary>
    Admin = 4
}

/// <summary>
/// 采购申请状态
/// </summary>
public enum RequestStatus
{
    /// <summary>草稿</summary>
    Draft = 0,
    /// <summary>待审批</summary>
    Pending = 1,
    /// <summary>经理已批</summary>
    ManagerApproved = 2,
    /// <summary>财务已批</summary>
    FinanceApproved = 3,
    /// <summary>已通过</summary>
    Approved = 4,
    /// <summary>已拒绝</summary>
    Rejected = 5,
    /// <summary>已退回</summary>
    Returned = 6,
    /// <summary>已撤销</summary>
    Cancelled = 7
}

/// <summary>
/// 紧急程度
/// </summary>
public enum Urgency
{
    /// <summary>普通</summary>
    Normal = 0,
    /// <summary>紧急</summary>
    Urgent = 1
}

/// <summary>
/// 审批操作类型
/// </summary>
public enum ApprovalAction
{
    /// <summary>同意</summary>
    Approve = 0,
    /// <summary>拒绝</summary>
    Reject = 1,
    /// <summary>退回修改</summary>
    Return = 2
}
