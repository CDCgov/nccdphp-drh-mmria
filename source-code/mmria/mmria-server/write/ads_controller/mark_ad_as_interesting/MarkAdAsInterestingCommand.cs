using System;
namespace cqrs.write;

public sealed class MarkAdAsInterestingCommand : ICommand
{
    public int UserId { get; set; }
    public int AdId { get; set; }
}

