---
title: Calling services with .NET gRPC client
author: jamesnk
description: Learn how to call gRPC services with the .NET gRPC client.
monikerRange: '>= aspnetcore-3.0'
ms.author: jamesnk
ms.date: 08/06/2019
uid: grpc/client
---
# Calling services with .NET gRPC client

A .NET gRPC client library is available in the [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client) NuGet package. This document shows how to:

* Configure .NET gRPC client to call gRPC services
* Make gRPC calls to unary, server streaming, client streaming and bi-direction streaming methods

If you are calling gRPC service from an ASP.NET Core app, consider [gRPC client factory integration in .NET Core](xref:grpc/clientfactory>). gRPC integration with HttpClientFactory offers a centralized alternative to creating gRPC clients.

## Configure gRPC client

gRPC clients are strongly typed .NET classes that are generated from `*.proto` files. When making a gRPC call the client will handle serialize messages to Protobuf and invoking the correct service.

To configure a gRPC client to call a service, start by creating a channel with the service address, and then use it to create a gRPC client:

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };
var channelBuilder = ChannelBuilder.ForHttpClient(httpclient);
var channel = channelBuilder.Build();

var client = new Greet.GreeterClient(channel);
```

A channel represents a long-lived connection to a gRPC service. When a channel is created it can be configured with options related to calling a service. For example, the `HttpClient` used to make calls, the maximum send and receive message size, and logging. Visit [Client Configuration]() for a complete list of channel configuration options.

Multiple gRPC clients can be created from a channel, including different types of clients. Reusing a channel is recommended for best performance. Strongly typed gRPC clients are light-weight objects and can be created when needed.

```csharp
var channel = CreateChannel("https://localhost:5001");

var greeterClient = new Greet.GreeterClient(channel);
var counterClient = new Count.CounterClient(channel);

// Use clients to call gRPC services
```

## Create gRPC clients by integrating with HttpClientFactory

gRPC integration with HttpClientFactory offers another way to create gRPC clients. Factory integration is available in the [Grpc.Net.ClientFactory](https://www.nuget.org/packages/Grpc.Net.ClientFactory) NuGet package. The factory offers the following benefits:

* Provides a central location for configuring logical gRPC client instances
* Manages the lifetime of the underlying `HttpClientMessageHandler`
* Automatic propagation of deadline and cancellation in a ASP.NET Core gRPC service

To register a gRPC client, the generic `AddGrpcClient` extension method can be used within Startup.ConfigureServices, specifying the gRPC typed client class and service address:

```csharp
services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
    o.BaseAddress = new Uri("http://localhost:5001");
});
```

The gRPC client type is registered as transient with DI. The client can now be injected and consumed directly in types create by DI, such as gRPC services.

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

### Configure HttpClient

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

### Configure Channel and Interceptors

gRPC specific configuration is available gRPC client's underlying channel and adding `Interceptor` instances to calls.

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

### Propagating deadline and cancellation

sdfsdf

## Making gRPC calls

Making a gRPC call depends on the type of method you are calling. The four gRPC method types are:

* Unary
* Server streaming
* Client streaming
* Bi-directional streaming

### Unary call

A unary call starts with the client sending a message. The response is returned when the server finishes.

```csharp
var client = new Greet.GreeterClient(channel);
var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });

Console.WriteLine("Greeting: " + response.Message);
// Greeting: Hello World
```

Generated gRPC clients have a blocking version of unary calls. For example, on `GreeterClient` there is `SayHellosAsync` which is async and `SayHellos` which is blocking. Alway use the async unary method in async code to avoid thread blocking.

### Server streaming call

A server streaming call starts with the client sending a request message. `ResponseStream.MoveNext()` reads messages return from the service until it finishes the call.

```csharp
var client = new Greet.GreeterClient(channel);
using (var call = client.SayHellos(new HelloRequest { Name = "World" }))
{
    while (await call.ResponseStream.MoveNext())
    {
        Console.WriteLine("Greeting: " + call.ResponseStream.Current.Message);
        // "Greeting: Hello World" is streamed multiple times
    }
}
```

### Client streaming call

A client streaming call starts without the client sending a message. The client can choose to send sends messages with `RequestStream.WriteAsync`. When the client has finished sending message `ResponseStream.CompleteAsync` should be called to notify the service that the client is finished. When the call finishes on the service it will return a response message.

```csharp
var client = new Counter.CounterClient(channel);
using (var call = client.AccumulateCount())
{
    for (var i = 0; i < 3; i++)
    {
        await call.RequestStream.WriteAsync(new CounterRequest { Count = 1 });
    }
    await call.RequestStream.CompleteAsync();

    var response = await call;
    Console.WriteLine($"Count: {response.Count}");
    // Count: 3
}

```

### Bi-directional streaming call

A bi-directional streaming call starts without the client sending a message. The client can choose to send messages with `RequestStream.WriteAsync`. Messages from the service are accessible with `ResponseStream.MoveNext()` and the call is complete when `ResponseStream` is finished.

```csharp
using (var call = client.Echo())
{
    Console.WriteLine("Starting background task to receive messages");
    var readTask = Task.Run(async () =>
    {
        while (await call.ResponseStream.MoveNext())
        {
            Console.WriteLine(call.ResponseStream.Current.Message);
            // Echo messages sent to the service
        }
    });

    Console.WriteLine("Starting to send messages");
    Console.WriteLine("Type a message to echo then press enter.");
    while (true)
    {
        var result = Console.ReadLine();
        if (string.IsNullOrEmpty(result))
        {
            break;
        }

        await call.RequestStream.WriteAsync(new EchoMessage { Message = result });
    }

    Console.WriteLine("Disconnecting");
    await call.RequestStream.CompleteAsync();
    await readTask;
}

```

During the course of a bi-directional streaming call the client and service can send messages to each other at any time and in any order. The best client logic for interacting with a bi-directional call varies depending on the service logic.

## Additional resources

* <xref:grpc/index>
* <xref:tutorials/grpc/grpc-start>
* <xref:grpc/clientfactory>
