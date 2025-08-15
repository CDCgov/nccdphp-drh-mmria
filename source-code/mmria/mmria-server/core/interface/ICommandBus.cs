using System;
namespace cqrs;

public interface ICommandBus
{
    void Send<TCommand>(TCommand command) where TCommand : ICommand;
}

