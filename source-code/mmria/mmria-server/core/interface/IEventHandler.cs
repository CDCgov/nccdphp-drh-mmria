using System;
namespace cqrs;

public interface IEventHandler { }

public interface IEventHandler<TEvent> : IEventHandler where TEvent : IEvent
{
    void Handle(TEvent @event);
}

