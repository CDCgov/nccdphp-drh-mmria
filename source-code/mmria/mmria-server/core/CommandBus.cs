using System;
namespace cqrs;

public sealed class CommandBus : ICommandBus
{
    private readonly Func<Type, ICommandHandler> _handlersFactory;
    public CommandBus(Func<Type, ICommandHandler> handlersFactory)

    {
        _handlersFactory = handlersFactory;
    }

    public void Send<TCommand>(TCommand command) where TCommand : ICommand
    {

        // log
        // authz
        // tx
        // validate
        // measure time
        // error handling
        // ...


        var handler = (ICommandHandler<TCommand>)_handlersFactory(typeof(TCommand));
        handler.Handle(command);
    }
}

