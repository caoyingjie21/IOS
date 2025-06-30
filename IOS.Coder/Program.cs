using IOS.Base.Configuration;
using IOS.Base.Mqtt;
using IOS.Base.Services;
using IOS.Coder.Configuration;
using IOS.Coder.MessageHandlers;
using IOS.Coder.Services;
using Microsoft.Extensions.Options;
using Serilog;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("启动读码器微服务");

    var builder = WebApplication.CreateBuilder(args);

    // 添加 Serilog
    builder.Host.UseSerilog();

    // 添加配置
    builder.Services.Configure<StandardMqttOptions>(
        builder.Configuration.GetSection(StandardMqttOptions.SectionName));
    builder.Services.Configure<CoderControlOptions>(
        builder.Configuration.GetSection(CoderControlOptions.SectionName));
    builder.Services.Configure<MqttOptions>(
        builder.Configuration.GetSection(MqttOptions.SectionName));

    // 添加基础服务
    builder.Services.AddSingleton<SharedDataService>();
    builder.Services.AddSingleton<IMqttService, MqttService>();

    // 添加读码器服务
    builder.Services.AddSingleton<CoderService>();
    builder.Services.AddSingleton<CoderMessageHandlerFactory>();

    // 添加消息处理器
    builder.Services.AddTransient<CoderServiceHandler>();
    builder.Services.AddTransient<CoderConfigHandler>();
    builder.Services.AddTransient<DefaultCoderMessageHandler>();

    // 添加主机服务
    builder.Services.AddHostedService<CoderHostService>();

    // 添加Web服务
    builder.Services.AddControllers();

    var app = builder.Build();

    // 配置HTTP请求管道
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();
    app.UseStaticFiles();

    app.MapControllers();

    // 添加默认路由
    app.MapGet("/", () => "读码器微服务运行中");

    // 添加健康检查端点
    app.MapGet("/health", (IServiceProvider services) =>
    {
        var sharedDataService = services.GetRequiredService<SharedDataService>();
        var coderOptions = services.GetRequiredService<IOptions<CoderControlOptions>>();
        
        return new
        {
            Status = "Healthy",
            Service = "IOS.Coder",
            Timestamp = DateTime.UtcNow,
            Configuration = coderOptions.Value.GetSummary(),
            LastData = sharedDataService.GetData<string>("LastCoderData"),
            LastDataTime = sharedDataService.GetData<DateTime?>("LastCoderDataTime")
        };
    });

    // 添加配置信息端点
    app.MapGet("/config", (IOptions<CoderControlOptions> coderOptions, 
                          IOptions<StandardMqttOptions> mqttOptions,
                          IOptions<MqttOptions> baseMqttOptions) =>
    {
        return new
        {
            CoderConfig = coderOptions.Value,
            StandardMqttConfig = new
            {
                Topics = mqttOptions.Value.Topics,
                Messages = mqttOptions.Value.Messages
            },
            MqttConfig = new
            {
                baseMqttOptions.Value.Broker,
                baseMqttOptions.Value.Port,
                baseMqttOptions.Value.ClientId,
                baseMqttOptions.Value.Username,
                baseMqttOptions.Value.KeepAlivePeriod
            }
        };
    });

    Log.Information("读码器微服务配置完成，开始运行");
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "读码器微服务启动失败");
}
finally
{
    Log.CloseAndFlush();
}
