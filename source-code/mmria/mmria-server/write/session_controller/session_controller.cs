using System;
namespace cqrs.write;

public sealed partial class session_controller
{
    private readonly ICommandBus _command_bus;
    public session_controller(ICommandBus p_command_bus)
    {
        _command_bus = p_command_bus;
    }

    /*
    public void MarkAsInteresting_UsingCommands(MarkAdAsInterestingCommand command)
    {
        _command_bus.Send(command);
    }


    public void MarkAsInteresting(MarkAdAsInterestingCommand command)
    {
        _command_bus.Send(command);
    }
    */

}

