﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOS.Base.Messaging;
using IOS.Base.Enums;

namespace IOS.Base.Mqtt
{
    public interface IMqttService
    {
        /// <summary>
        /// 消息接收事件
        /// </summary>
        event Func<string, string, Task>? OnMessageReceived;

        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        event Func<bool, Task>? OnConnectionChanged;

        /// <summary>
        /// 启动MQTT服务
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止MQTT服务
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 发布消息
        /// </summary>
        Task PublishAsync<T>(string topic, StandardMessage<T> message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发布原始消息
        /// </summary>
        Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发布标准消息，自动生成StandardMessage包装
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="topic">发布主题</param>
        /// <param name="message">消息数据</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        Task PublishStandardMessageAsync<T>(string topic, T message, MessageType messageType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 订阅主题
        /// </summary>
        Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消订阅主题
        /// </summary>
        Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 健康检查
        /// </summary>
        Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
    }
}
