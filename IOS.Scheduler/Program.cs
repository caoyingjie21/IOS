using Serilog;
using IOS.Base.Extensions;
using IOS.Scheduler.Services;
using IOS.Scheduler.MessageHandlers;

var builder = WebApplication.CreateBuilder(args);
// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Information("正在配置服务...");
builder.Services.AddIOSBase(builder.Configuration);
builder.Services.AddIOSBaseServices();

// 注册消息处理器
builder.Services.AddTransient<GratingTriggerHandler>();
builder.Services.AddTransient<CameraResultHandler>();
builder.Services.AddTransient<MotionCompleteHandler>();
builder.Services.AddTransient<CoderCompleteHandler>();
builder.Services.AddTransient<DefaultMessageHandler>();
builder.Services.AddTransient<HeightResultHandler>();

// 注册消息处理器工厂
builder.Services.AddSingleton<SchedulerMessageHandlerFactory>();

Log.Information("服务配置完成!");

builder.Services.AddHostedService<SchedulerHostService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

await app.RunAsync();