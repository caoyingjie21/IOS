using IOS.Base.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IOS.Scheduler.MessageHandlers;

/// <summary>
/// 调度器消息处理器工厂
/// </summary>
public class SchedulerMessageHandlerFactory : MessageHandlerFactory
{
    public SchedulerMessageHandlerFactory(IServiceProvider serviceProvider, ILogger<SchedulerMessageHandlerFactory> logger) 
        : base(serviceProvider, logger)
    {
    }

    protected override Dictionary<string, Type> InitializeHandlerMappings()
    {
        return new Dictionary<string, Type>
        {
            // 光栅触发消息
            { "*/sensor/grating/trigger", typeof(GratingTriggerHandler) },
            { "+/sensor/grating/trigger", typeof(GratingTriggerHandler) },
            
            // 相机结果消息
            { "*/vision/camera/result", typeof(CameraResultHandler) },
            { "+/vision/camera/result", typeof(CameraResultHandler) },
            
            // 运动完成消息
            { "*/motion/control/complete", typeof(MotionCompleteHandler) },
            { "+/motion/control/complete", typeof(MotionCompleteHandler) },
            
            // 编码器完成消息
            { "*/coder/service/complete", typeof(CoderCompleteHandler) },
            { "+/coder/service/complete", typeof(CoderCompleteHandler) },
            
        };
    }

    protected override Type GetDefaultHandlerType()
    {
        return typeof(DefaultMessageHandler);
    }
} 