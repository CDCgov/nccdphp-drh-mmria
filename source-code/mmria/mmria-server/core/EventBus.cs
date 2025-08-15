using System;
using System.Collections.Generic;
using System.Linq;

namespace cqrs;

public sealed class EventBus : IEventBus
{
    private readonly Func<Type, IEnumerable<IEventHandler>> _handlersFactory;
    public EventBus(Func<Type, IEnumerable<IEventHandler>> handlersFactory)
    {
        _handlersFactory = handlersFactory;
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var handlers = _handlersFactory(typeof(TEvent))
            .Cast<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            handler.Handle(@event);
        }
    }
}


