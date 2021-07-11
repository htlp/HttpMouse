# HttpMouse
基于yarp的http公网反向代理到内网的服务端与客户端库

### Nuget

| 包名 | 描述 | Nuget |
---|---|--|
| HttpMouse | 服务端用 | [![NuGet](https://buildstats.info/nuget/HttpMouse)](https://www.nuget.org/packages/HttpMouse) |
| HttpMouse.Client | 客户端用 | [![NuGet](https://buildstats.info/nuget/HttpMouse.Client )](https://www.nuget.org/packages/HttpMouse.Client ) | 

### 原理图
![image](https://raw.githubusercontent.com/xljiulang/HttpMouse/master/HttpMouse.png)

### 服务端开发
#### 基础入门
```
/// <summary>
/// 配置服务
/// </summary>
/// <param name="services"></param>
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpMouse(options =>
    {
        options.DefaultKey = "客户端连接秘钥";
    });
}

/// <summary>
/// 配置中间件
/// </summary>
/// <param name="app"></param>
/// <param name="hostEnvironment"></param>
public void Configure(IApplicationBuilder app, IHostEnvironment hostEnvironment)
{
    app.UseHttpMouse();
    app.UseRouting();

    // 其它中间件

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapReverseProxy();
    });
} 
```

#### 自定义客户端认证

```
/// <summary>
/// 配置服务
/// </summary>
/// <param name="services"></param>
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpMouse(options =>
    {
        options.DefaultKey = "客户端连接秘钥";
    });

    services.AddSingleton<IHttpMouseClientVerifier, ComstomClientVerifier>();
}
```

```
class ComstomClientVerifier : IHttpMouseClientVerifier
{
    public ValueTask<bool> VerifyAsync(IHttpMouseClient httpMouseClient)
    {
        var key = httpMouseClient.Key;
        var domain = httpMouseClient.Domain;

        var result = false;
        if (domain == "b.xx.com")
        {
            result = key == "123456";
        }

        return ValueTask.FromResult(result);
    }
}
```

#### YARP功能
services.AddHttpMouse()返回YARP的IReverseProxyBuilder对象，此对象还有其它比较重要的功能。有关YARP的完整功能介绍，可以阅读[YARP文档](https://microsoft.github.io/reverse-proxy/articles/getting-started.html)

##### AddConfigFilter
AddConfigFilter()实际注册了一个IProxyConfigFilter服务，服务用于修改既有的路由与集群配置。

#### AddTransforms
AddTransforms()提供多个重载方法，最终注册ITransformProvider服务，服务用于变换http请求或响应内容。

#### ConfigureHttpClient
用于配置SocketsHttpHandler的配置

#### ~~LoadFromConfig~~
 ~~LoadFromConfig()实际是注册了基于配置文件的IProxyConfigProvider，HttpMouse实现了基于内存的IProxyConfigProvider，功能冲突，不能调用此方法~~

