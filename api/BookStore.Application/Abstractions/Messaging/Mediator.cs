namespace BookStore.Application.Abstractions.Messaging;

public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(
            request.GetType(),
            typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler is registered for {request.GetType().Name}.");
        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))
            ?? throw new InvalidOperationException(
                $"The handler for {request.GetType().Name} has no Handle method.");

        return (Task<TResponse>)handleMethod.Invoke(
            handler,
            [request, cancellationToken])!;
    }
}
