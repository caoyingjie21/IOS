using Core.Net.EtherCAT;
using IOS.Base.Extensions;
using IOS.Motion.Configuration;
using IOS.Motion.MessageHandlers;
using IOS.Motion.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Information("正在配置电机模块服务...");

// 添加Web服务
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// 添加基础服务
builder.Services.AddIOSBase(builder.Configuration);
builder.Services.AddIOSBaseServices();
// 注册EtherCAT主站
builder.Services.AddSingleton<EtherCATMaster>();
// 添加 Motion 模块专有配置 - 使用泛型配置方法
builder.Services.AddModuleConfiguration<MotionControlOptions, MotionControlOptionsValidator>(builder.Configuration);

// 注册消息处理器
builder.Services.AddTransient<MotionControlHandler>();
builder.Services.AddTransient<MotionStatusHandler>();
builder.Services.AddTransient<MotionCalibrationHandler>();
builder.Services.AddTransient<DefaultMotionMessageHandler>();

// 注册消息处理器工厂
builder.Services.AddSingleton<MotionMessageHandlerFactory>();

// 注册后台服务
builder.Services.AddHostedService<MotionHostService>();

Log.Information("电机模块服务配置完成!");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 配置默认路由到静态页面
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapControllers();
app.MapRazorPages();

// 显示应用运行信息
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    Log.Information("IOS.Motion 电机模块已启动");
    foreach (var address in addresses)
    {
        Log.Information("访问地址: {Address}", address);
    }
});

await app.RunAsync();
