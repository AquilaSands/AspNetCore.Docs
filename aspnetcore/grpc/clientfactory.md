---
title: gRPC client factory integration in .NET Core
author: jamesnk
description: Learn how to create gRPC clients using the client factory.
monikerRange: '>= aspnetcore-3.0'
ms.author: jamesnk
ms.date: 08/06/2019
uid: grpc/clientfactory
---
# gRPC client factory integration in .NET Core

.NET gRPC clients can be [created as stand-alone instances](xref:grpc/client). gRPC integration with HttpClientFactory offers a centralized alternative to creating gRPC clients. Factory integration is available in the [Grpc.Net.ClientFactory](https://www.nuget.org/packages/Grpc.Net.ClientFactory) NuGet package.

The factory offers the following benefits:

* Provides a central location for configuring logical gRPC client instances
* Manages the lifetime of the underlying `HttpClientMessageHandler`
* Automatic propagation of deadline and cancellation in a ASP.NET Core gRPC service

## Register gRPC clients

To register a gRPC client, the generic `AddGrpcClient` extension method can be used within `Startup.ConfigureServices`, specifying the gRPC typed client class and service address:

```csharp
services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
    o.BaseAddress = new Uri("http://localhost:5001");
});
```

The gRPC client type is registered as transient with DI. The client can now be injected and consumed directly in types create by DI throughout the app. ASP.NET Core MVC controllers, SignalR hubs and gRPC services are a couple of examples:

```csharp
public class AggregatorService : Aggregator.AggregatorBase
{
    private readonly Greeter.GreeterClient _client;

    public AggregatorService(Greeter.GreeterClient client)
    {
        _client = client;
    }

    public override async Task SayHellos(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        // Forward the call on to the greeter service
        using (var call = _client.SayHellos(request))
        {
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                await responseStream.WriteAsync(call.ResponseStream.Current);
            }
        }
    }
}
```

## Configure HttpClient

HttpClientFactory creates the HttpClient used by the gRPC client. Standard HttpClientFactory methods can be used to add outgoing request middleware or configure the underlying HttpClientHandler:

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>(o =>
    {
        o.BaseAddress = new Uri("http://localhost:5001");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(LoadCertificate());
        return handler;
    });
```

## Configure Channel and Interceptors

gRPC specific methods are available to configure gRPC client's underlying channel, and add `Interceptor` instances to the client.

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>(o =>
    {
        o.BaseAddress = new Uri("http://localhost:5001");
    })
    .AddInterceptor(() => new LoggingInterceptor())
    .ConfigureChannel(o =>
    {
        o.Credentials = new CustomCredentials();
    });
```

## Deadline and cancellation propagation

gRPC clients created by the factory in a gRPC service can be configured with `EnableCallContextPropagation()` to automatically propagate the deadline and cancellation token to child calls.

Call context propagation works by reading those details from the current gRPC request context and automatically propagating them to outgoing calls made by the gRPC client. Call context propagation is an excellent way of ensuring complex, nested gRPC scenarios always propagate the deadline and cancellation through multiple calls.

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>(o =>
    {
        o.BaseAddress = new Uri("http://localhost:5001");
    })
    .EnableCallContextPropagation();
```

## Additional resources

* <xref:grpc/client>
* <xref:tutorials/grpc/grpc-start>
* <xref:fundamentals/http-requests>
