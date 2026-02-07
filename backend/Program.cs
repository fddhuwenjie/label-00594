using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Services;
using PurchaseApproval.Workflows;
using WorkflowCore.Interface;

var builder = WebApplication.CreateBuilder(args);

// ========== Services Configuration ==========

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// WorkflowCore
builder.Services.AddWorkflow(cfg =>
{
    cfg.UsePostgreSQL(builder.Configuration.GetConnectionString("DefaultConnection")!, true, true);
});

// Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "采购审批管理系统 API", Version = "v1" });
});

var app = builder.Build();

// ========== Pipeline Configuration ==========

// Swagger (always enabled for demo)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// ========== Database & Workflow Initialization ==========

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // 等待数据库就绪 (Docker 环境)
    var maxRetries = 10;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            context.Database.EnsureCreated();
            break;
        }
        catch (Exception)
        {
            Console.WriteLine($"⏳ 等待数据库就绪... ({i + 1}/{maxRetries})");
            if (i == maxRetries - 1) throw;
            Thread.Sleep(3000);
        }
    }
    
    DbInitializer.Initialize(context);
}

// 启动 WorkflowCore 引擎
var host = app.Services.GetRequiredService<IWorkflowHost>();
host.RegisterWorkflow<PurchaseApprovalWorkflow, PurchaseWorkflowData>();
host.Start();

// ========== Startup Banner ==========
Console.WriteLine();
Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║          采购审批管理系统 - Backend API                    ║");
Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
Console.WriteLine("║  ✅ Startup Success                                        ║");
Console.WriteLine("║  🌐 API: http://localhost:5000                             ║");
Console.WriteLine("║  📚 Swagger: http://localhost:5000/swagger                 ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

app.Run();
