using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context, ILogger logger)
    {
        // 确保数据库已创建
        context.Database.EnsureCreated();

        // 如果已有用户数据，跳过初始化
        if (context.Users.Any())
        {
            return;
        }

        // ========== 创建测试用户 ==========
        var zhangsan = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username = "zhangsan",
            Password = HashPassword(),
            DisplayName = "张三",
            Role = UserRole.Employee,
            Department = "研发部"
        };
        var lisi = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Username = "lisi",
            Password = HashPassword(),
            DisplayName = "李四",
            Role = UserRole.Manager,
            Department = "研发部"
        };
        var wangwu = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Username = "wangwu",
            Password = HashPassword(),
            DisplayName = "王五",
            Role = UserRole.Finance,
            Department = "财务部"
        };
        var zhaoliu = new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Username = "zhaoliu",
            Password = HashPassword(),
            DisplayName = "赵六",
            Role = UserRole.Director,
            Department = "总经办"
        };
        var admin = new User
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Username = "admin",
            Password = HashPassword(),
            DisplayName = "系统管理员",
            Role = UserRole.Admin,
            Department = "信息部"
        };

        context.Users.AddRange(zhangsan, lisi, wangwu, zhaoliu, admin);
        context.SaveChanges();

        var now = DateTimeHelper.GetBeijingTime();

        // ========== 场景1: 草稿状态 - 可编辑提交 ==========
        var req1_draft = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0001-0000-0000-0000-000000000001"),
            RequestNumber = "PR20240001",
            ApplicantId = zhangsan.Id,
            ItemName = "办公文具套装",
            Quantity = 20,
            UnitPrice = 50,
            Reason = "部门日常办公消耗品补充",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Draft,
            CurrentApprovalLevel = 0,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-5)
        };

        // ========== 场景2: 小额申请待审批(≤5000) - 仅需经理审批 ==========
        var req2_pending_small = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0002-0000-0000-0000-000000000002"),
            RequestNumber = "PR20240002",
            ApplicantId = zhangsan.Id,
            ItemName = "机械键盘",
            Quantity = 5,
            UnitPrice = 500,
            Reason = "研发团队需要更换老旧键盘，提高工作效率",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Pending,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3)
        };

        // ========== 场景3: 中额申请待审批(5001-20000) - 需经理+财务审批 ==========
        var req3_pending_medium = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0003-0000-0000-0000-000000000003"),
            RequestNumber = "PR20240003",
            ApplicantId = zhangsan.Id,
            ItemName = "专业显示器",
            Quantity = 5,
            UnitPrice = 2500,
            Reason = "设计团队需要4K专业显示器进行UI设计工作",
            Urgency = Urgency.Urgent,
            Status = RequestStatus.Pending,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2)
        };

        // ========== 场景4: 大额申请待审批(>20000) - 需三级审批 ==========
        var req4_pending_large = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0004-0000-0000-0000-000000000004"),
            RequestNumber = "PR20240004",
            ApplicantId = zhangsan.Id,
            ItemName = "高性能工作站",
            Quantity = 3,
            UnitPrice = 15000,
            Reason = "AI研发团队需要高性能GPU工作站进行模型训练",
            Urgency = Urgency.Urgent,
            Status = RequestStatus.Pending,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        // ========== 场景5: 经理已批准,等待财务审批 ==========
        var req5_manager_approved = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0005-0000-0000-0000-000000000005"),
            RequestNumber = "PR20240005",
            ApplicantId = zhangsan.Id,
            ItemName = "团队服务器",
            Quantity = 1,
            UnitPrice = 18000,
            Reason = "搭建内部开发测试服务器",
            Urgency = Urgency.Normal,
            Status = RequestStatus.ManagerApproved,
            CurrentApprovalLevel = 2,
            CreatedAt = now.AddDays(-4),
            UpdatedAt = now.AddDays(-2)
        };

        // ========== 场景6: 财务已批准,等待总经理审批 ==========
        var req6_finance_approved = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0006-0000-0000-0000-000000000006"),
            RequestNumber = "PR20240006",
            ApplicantId = zhangsan.Id,
            ItemName = "会议室投影设备",
            Quantity = 2,
            UnitPrice = 25000,
            Reason = "新会议室需要配备专业投影设备",
            Urgency = Urgency.Normal,
            Status = RequestStatus.FinanceApproved,
            CurrentApprovalLevel = 3,
            CreatedAt = now.AddDays(-6),
            UpdatedAt = now.AddDays(-1)
        };

        // ========== 场景7: 小额已完全通过 (本月) ==========
        var req7_approved_small = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0007-0000-0000-0000-000000000007"),
            RequestNumber = "PR20240007",
            ApplicantId = zhangsan.Id,
            ItemName = "打印纸",
            Quantity = 100,
            UnitPrice = 30,
            Reason = "办公室日常打印用纸",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Approved,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-4)  // 本月通过
        };

        // ========== 场景8: 中额已完全通过(经过两级审批, 本月) ==========
        var req8_approved_medium = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0008-0000-0000-0000-000000000008"),
            RequestNumber = "PR20240008",
            ApplicantId = zhangsan.Id,
            ItemName = "办公桌椅套装",
            Quantity = 10,
            UnitPrice = 1500,
            Reason = "新员工工位配置",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Approved,
            CurrentApprovalLevel = 2,
            CreatedAt = now.AddDays(-6),
            UpdatedAt = now.AddDays(-3)  // 本月通过
        };

        // ========== 场景9: 大额已完全通过(经过三级审批, 本月) ==========
        var req9_approved_large = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0009-0000-0000-0000-000000000009"),
            RequestNumber = "PR20240009",
            ApplicantId = zhangsan.Id,
            ItemName = "企业级存储设备",
            Quantity = 1,
            UnitPrice = 80000,
            Reason = "数据中心扩容需求",
            Urgency = Urgency.Urgent,
            Status = RequestStatus.Approved,
            CurrentApprovalLevel = 3,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-2)  // 本月通过
        };

        // ========== 场景10: 被拒绝的申请 ==========
        var req10_rejected = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0010-0000-0000-0000-000000000010"),
            RequestNumber = "PR20240010",
            ApplicantId = zhangsan.Id,
            ItemName = "游戏外设套装",
            Quantity = 5,
            UnitPrice = 2000,
            Reason = "团队建设活动使用",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Rejected,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-7)
        };

        // ========== 场景11: 被退回修改的申请 ==========
        var req11_returned = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0011-0000-0000-0000-000000000011"),
            RequestNumber = "PR20240011",
            ApplicantId = zhangsan.Id,
            ItemName = "办公软件许可",
            Quantity = 20,
            UnitPrice = 800,
            Reason = "采购正版办公软件",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Returned,
            CurrentApprovalLevel = 0,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-4)
        };

        // ========== 场景12: 已撤销的申请 ==========
        var req12_cancelled = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0012-0000-0000-0000-000000000012"),
            RequestNumber = "PR20240012",
            ApplicantId = zhangsan.Id,
            ItemName = "临时设备租赁",
            Quantity = 1,
            UnitPrice = 5000,
            Reason = "活动临时使用",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Cancelled,
            CurrentApprovalLevel = 1,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-6)
        };

        // ========== 场景13: 经理提交的大额申请 - 直接从财务审批开始 ==========
        var req13_manager_submit = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0013-0000-0000-0000-000000000013"),
            RequestNumber = "PR20240013",
            ApplicantId = lisi.Id,  // 经理李四提交
            ItemName = "部门团建活动",
            Quantity = 1,
            UnitPrice = 15000,
            Reason = "研发部年度团建活动经费",
            Urgency = Urgency.Normal,
            Status = RequestStatus.ManagerApproved,  // 经理提交>5000，跳过经理审批，从财务开始
            CurrentApprovalLevel = 2,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2)
        };

        // ========== 场景14: 财务提交的大额申请 - 直接从总经理审批开始 ==========
        var req14_finance_submit = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0014-0000-0000-0000-000000000014"),
            RequestNumber = "PR20240014",
            ApplicantId = wangwu.Id,  // 财务王五提交
            ItemName = "财务系统升级",
            Quantity = 1,
            UnitPrice = 50000,
            Reason = "财务管理系统需要升级到新版本",
            Urgency = Urgency.Urgent,
            Status = RequestStatus.FinanceApproved,  // 财务提交>20000，跳过财务审批，从总经理开始
            CurrentApprovalLevel = 3,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        // ========== 场景15: 总经理提交的申请 - 直接通过(本月) ==========
        var req15_director_submit = new PurchaseRequest
        {
            Id = Guid.Parse("aaaa0015-0000-0000-0000-000000000015"),
            RequestNumber = "PR20240015",
            ApplicantId = zhaoliu.Id,  // 总经理赵六提交
            ItemName = "公司战略咨询",
            Quantity = 1,
            UnitPrice = 100000,
            Reason = "聘请外部顾问进行战略规划咨询",
            Urgency = Urgency.Normal,
            Status = RequestStatus.Approved,  // 总经理提交直接通过
            CurrentApprovalLevel = 3,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3)  // 本月通过
        };

        context.PurchaseRequests.AddRange(
            req1_draft, req2_pending_small, req3_pending_medium, req4_pending_large,
            req5_manager_approved, req6_finance_approved,
            req7_approved_small, req8_approved_medium, req9_approved_large,
            req10_rejected, req11_returned, req12_cancelled,
            req13_manager_submit, req14_finance_submit, req15_director_submit
        );
        context.SaveChanges();

        // ========== 创建审批记录 ==========
        var approvalRecords = new List<ApprovalRecord>
        {
            // req5: 经理已批准
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req5_manager_approved.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Approve,
                Comment = "同意采购，对团队开发很有帮助",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-2)
            },

            // req6: 经理和财务都已批准
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req6_finance_approved.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Approve,
                Comment = "同意，会议室设备确实需要升级",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-4)
            },
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req6_finance_approved.Id,
                ApproverId = wangwu.Id,
                Action = ApprovalAction.Approve,
                Comment = "预算充足，同意采购",
                ApprovalLevel = 2,
                CreatedAt = now.AddDays(-2)
            },

            // req7: 小额已通过 - 经理审批
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req7_approved_small.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Approve,
                Comment = "日常消耗品，同意",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-9)
            },

            // req8: 中额已通过 - 经理+财务审批
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req8_approved_medium.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Approve,
                Comment = "新员工入职必需",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-14)
            },
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req8_approved_medium.Id,
                ApproverId = wangwu.Id,
                Action = ApprovalAction.Approve,
                Comment = "同意，价格合理",
                ApprovalLevel = 2,
                CreatedAt = now.AddDays(-13)
            },

            // req9: 大额已通过 - 三级审批完整流程
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req9_approved_large.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Approve,
                Comment = "技术需求合理",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-18)
            },
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req9_approved_large.Id,
                ApproverId = wangwu.Id,
                Action = ApprovalAction.Approve,
                Comment = "Q4预算内，同意采购",
                ApprovalLevel = 2,
                CreatedAt = now.AddDays(-17)
            },
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req9_approved_large.Id,
                ApproverId = zhaoliu.Id,
                Action = ApprovalAction.Approve,
                Comment = "同意，这是公司战略发展需要",
                ApprovalLevel = 3,
                CreatedAt = now.AddDays(-16)
            },

            // req10: 被拒绝
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req10_rejected.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Reject,
                Comment = "与工作无关的设备不予采购，请提交正规办公设备申请",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-7)
            },

            // req11: 被退回
            new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                RequestId = req11_returned.Id,
                ApproverId = lisi.Id,
                Action = ApprovalAction.Return,
                Comment = "请补充软件版本信息和授权方式说明",
                ApprovalLevel = 1,
                CreatedAt = now.AddDays(-4)
            }
        };

        context.ApprovalRecords.AddRange(approvalRecords);
        context.SaveChanges();

        // ========== 创建通知 ==========
        var notifications = new List<Notification>
        {
            // 待审批通知 - 给经理
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = lisi.Id,
                Title = "新的待审批申请",
                Content = "张三提交了一笔采购申请 [PR20240002] 机械键盘，金额 ¥2,500，请及时审批",
                RequestId = req2_pending_small.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-3)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = lisi.Id,
                Title = "紧急待审批申请",
                Content = "张三提交了一笔紧急采购申请 [PR20240003] 专业显示器，金额 ¥12,500，请优先处理",
                RequestId = req3_pending_medium.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-2)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = lisi.Id,
                Title = "紧急待审批申请",
                Content = "张三提交了一笔紧急采购申请 [PR20240004] 高性能工作站，金额 ¥45,000，请优先处理",
                RequestId = req4_pending_large.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-1)
            },

            // 待审批通知 - 给财务
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = wangwu.Id,
                Title = "新的待审批申请",
                Content = "采购申请 [PR20240005] 团队服务器 已通过部门经理审批，等待您的财务审批",
                RequestId = req5_manager_approved.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-2)
            },

            // 待审批通知 - 给总经理
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhaoliu.Id,
                Title = "新的待审批申请",
                Content = "采购申请 [PR20240006] 会议室投影设备 已通过财务审批，等待您的最终审批",
                RequestId = req6_finance_approved.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-1)
            },

            // 审批结果通知 - 给申请人张三
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhangsan.Id,
                Title = "采购申请已通过",
                Content = "您的采购申请 [PR20240007] 打印纸 已被 李四 审批通过",
                RequestId = req7_approved_small.Id,
                IsRead = true,
                CreatedAt = now.AddDays(-9)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhangsan.Id,
                Title = "采购申请已通过",
                Content = "您的采购申请 [PR20240008] 办公桌椅套装 已完成全部审批流程，可以进行采购",
                RequestId = req8_approved_medium.Id,
                IsRead = true,
                CreatedAt = now.AddDays(-12)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhangsan.Id,
                Title = "采购申请已通过",
                Content = "您的采购申请 [PR20240009] 企业级存储设备 已获得总经理批准，可以进行采购",
                RequestId = req9_approved_large.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-15)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhangsan.Id,
                Title = "采购申请已拒绝",
                Content = "您的采购申请 [PR20240010] 游戏外设套装 已被 李四 拒绝，原因：与工作无关的设备不予采购",
                RequestId = req10_rejected.Id,
                IsRead = true,
                CreatedAt = now.AddDays(-7)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhangsan.Id,
                Title = "采购申请已退回",
                Content = "您的采购申请 [PR20240011] 办公软件许可 已被 李四 退回，请补充信息后重新提交",
                RequestId = req11_returned.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-4)
            },

            // 经理的历史通知
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = lisi.Id,
                Title = "审批完成提醒",
                Content = "您已完成采购申请 [PR20240007] 的审批",
                RequestId = req7_approved_small.Id,
                IsRead = true,
                CreatedAt = now.AddDays(-9)
            },

            // 经理提交的申请通知 - 给财务
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = wangwu.Id,
                Title = "新的待审批申请",
                Content = "李四（部门经理）提交了采购申请 [PR20240013] 部门团建活动，金额 ¥15,000，请审批",
                RequestId = req13_manager_submit.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-2)
            },

            // 财务提交的申请通知 - 给总经理
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhaoliu.Id,
                Title = "紧急待审批申请",
                Content = "王五（财务总监）提交了紧急采购申请 [PR20240014] 财务系统升级，金额 ¥50,000，请审批",
                RequestId = req14_finance_submit.Id,
                IsRead = false,
                CreatedAt = now.AddDays(-1)
            },

            // 总经理提交直接通过的通知
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = zhaoliu.Id,
                Title = "采购申请已通过",
                Content = "您的采购申请 [PR20240015] 公司战略咨询 已自动通过（总经理权限）",
                RequestId = req15_director_submit.Id,
                IsRead = true,
                CreatedAt = now.AddDays(-3)
            }
        };

        context.Notifications.AddRange(notifications);
        context.SaveChanges();

        logger.LogInformation("数据库初始化完成: users={UserCount}, requests={RequestCount}, approvalRecords={ApprovalRecordCount}, notifications={NotificationCount}",
            context.Users.Count(),
            context.PurchaseRequests.Count(),
            context.ApprovalRecords.Count(),
            context.Notifications.Count());
    }

    private static string HashPassword()
    {
        var seedPassword = Environment.GetEnvironmentVariable("SEED_USER_PASSWORD");
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            throw new InvalidOperationException("缺少 SEED_USER_PASSWORD 环境变量，无法初始化测试用户密码");
        }

        return BCrypt.Net.BCrypt.HashPassword(seedPassword);
    }
}
