using System;
namespace cqrs;

public interface IEventBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
}

