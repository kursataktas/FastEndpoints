﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FastEndpoints;

/// <summary>
/// handler registration options
/// </summary>
public class HandlerOptions<TStorageRecord, TStorageProvider>
    where TStorageRecord : IEventStorageRecord, new()
    where TStorageProvider : class, IEventHubStorageProvider<TStorageRecord>
{
    private readonly IEndpointRouteBuilder routeBuilder;
    private static readonly MethodInfo _mapGrpcMethodInfo
        = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))!;

    internal HandlerOptions(IEndpointRouteBuilder builder)
    {
        routeBuilder = builder;
    }

    /// <summary>
    /// registers a "void" command handler this server is hosting.
    /// </summary>
    /// <typeparam name="TCommand">the type of the incoming command</typeparam>
    /// <typeparam name="THandler">the type of the handler for the incoming command</typeparam>
    public GrpcServiceEndpointConventionBuilder Register<TCommand, THandler>()
        where TCommand : class, ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        var tHandler = routeBuilder.ServiceProvider.GetService<ICommandHandler<TCommand>>();
        if (tHandler is not null)
        {
            var tService = typeof(VoidHandlerExecutor<,>).MakeGenericType(typeof(TCommand), tHandler.GetType());
            var mapMethod = _mapGrpcMethodInfo.MakeGenericMethod(tService);
            return (GrpcServiceEndpointConventionBuilder)mapMethod.Invoke(null, new[] { routeBuilder })!;
        }
        return routeBuilder.MapGrpcService<VoidHandlerExecutor<TCommand, THandler>>();
    }

    /// <summary>
    /// registers a "unary" command handler this server is hosting.
    /// </summary>
    /// <typeparam name="TCommand">the type of the incoming command</typeparam>
    /// <typeparam name="THandler">the type of the handler for the incoming command</typeparam>
    /// <typeparam name="TResult">the type of the result that will be returned from the handler</typeparam>
    public GrpcServiceEndpointConventionBuilder Register<TCommand, THandler, TResult>()
        where TCommand : class, ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TResult : class
            => routeBuilder.MapGrpcService<UnaryHandlerExecutor<TCommand, THandler, TResult>>();

    /// <summary>
    /// registers a "server stream" command handler this server is hosting.
    /// </summary>
    /// <typeparam name="TCommand">the type of the incoming command</typeparam>
    /// <typeparam name="THandler">the type of the handler for the incoming command</typeparam>
    /// <typeparam name="TResult">the type of the result stream that will be returned from the handler</typeparam>
    public GrpcServiceEndpointConventionBuilder RegisterServerStream<TCommand, THandler, TResult>()
        where TCommand : class, IServerStreamCommand<TResult>
        where THandler : class, IServerStreamCommandHandler<TCommand, TResult>
        where TResult : class
            => routeBuilder.MapGrpcService<ServerStreamHandlerExecutor<TCommand, THandler, TResult>>();

    /// <summary>
    /// registers a "client stream" command handler this server is hosting.
    /// </summary>
    /// <typeparam name="T">the type of the incoming item stream</typeparam>
    /// <typeparam name="THandler">the type of the handler for the incoming stream</typeparam>
    /// <typeparam name="TResult">the type of the result that will be returned from the handler when the stream ends</typeparam>
    public GrpcServiceEndpointConventionBuilder RegisterClientStream<T, THandler, TResult>()
        where T : class
        where THandler : class, IClientStreamCommandHandler<T, TResult>
        where TResult : class
            => routeBuilder.MapGrpcService<ClientStreamHandlerExecutor<T, THandler, TResult>>();

    /// <summary>
    /// registers an "event hub" that broadcasts events of the given type to all remote subscribers in an asynchronous manner
    /// </summary>
    /// <param name="mode">the operation mode of this event hub</param>
    /// <typeparam name="TEvent">the type of event this hub deals with</typeparam>
    public GrpcServiceEndpointConventionBuilder RegisterEventHub<TEvent>(HubMode mode = HubMode.EventPublisher)
        where TEvent : class, IEvent
    {
        EventHub<TEvent, TStorageRecord, TStorageProvider>.Mode = mode;
        return routeBuilder.MapGrpcService<EventHub<TEvent, TStorageRecord, TStorageProvider>>();
    }
}