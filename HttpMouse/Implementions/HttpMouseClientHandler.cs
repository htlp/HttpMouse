﻿using HttpMouse.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 客户端处理者
    /// </summary> 
    sealed class HttpMouseClientHandler : IHttpMouseClientHandler
    {
        private const string SERVER_KEY = "ServerKey";
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";

        private readonly IHttpMouseClientVerifier verifier;
        private readonly ILogger<HttpMouseClientHandler> logger;
        private readonly ConcurrentDictionary<string, IHttpMouseClient> clients = new();


        /// <summary>
        /// 客户端变化后事件
        /// </summary>
        public event Action<IHttpMouseClient[]>? ClientsChanged;

        /// <summary>
        /// 客户端处理者
        /// </summary>
        /// <param name="verifier"></param> 
        /// <param name="logger"></param>
        public HttpMouseClientHandler(
            IHttpMouseClientVerifier verifier,
            ILogger<HttpMouseClientHandler> logger)
        {
            this.verifier = verifier;
            this.logger = logger;
        }

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task HandleConnectionAsync(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest == false ||
                context.Request.Headers.TryGetValue(SERVER_KEY, out var keyValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_DOMAIN, out var domainValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_UP_STREAM, out var upSteramValues) == false ||
                Uri.TryCreate(upSteramValues.ToString(), UriKind.Absolute, out var clientUpstream) == false)
            {
                await next();
                return;
            }

            var clientDomain = domainValues.ToString();
            var clientKey = keyValues.ToString();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var client = new HttpMouseClient(clientDomain, clientUpstream, clientKey, webSocket);

            // 验证客户端
            if (await this.verifier.VerifyAsync(client) == false)
            {
                await client.CloseAsync("Key不正确");
                return;
            }

            // 验证连接唯一
            if (this.clients.TryAdd(clientDomain, client) == false)
            {
                await client.CloseAsync($"已在其它地方存在{clientDomain}的客户端实例");
                return;
            }

            this.logger.LogInformation($"{client}连接过来");
            this.ClientsChanged?.Invoke(this.clients.Values.ToArray());

            await client.WaitingCloseAsync();

            this.logger.LogInformation($"{client}断开连接");
            this.clients.TryRemove(clientDomain, out _);
            this.ClientsChanged?.Invoke(this.clients.Values.ToArray());
        }

        /// <summary>
        /// 尝试获取连接 
        /// </summary>
        /// <param name="clientDomain"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string clientDomain, [MaybeNullWhen(false)] out IHttpMouseClient value)
        {
            return this.clients.TryGetValue(clientDomain, out value);
        }
    }
}
