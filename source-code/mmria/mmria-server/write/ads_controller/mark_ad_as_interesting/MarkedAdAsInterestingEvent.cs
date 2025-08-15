using System;
namespace cqrs.write;

public sealed class MarkedAdAsInterestingEvent : IEvent
{
    public int UserId { get; set; }
    public int AdId { get; set; }
}

