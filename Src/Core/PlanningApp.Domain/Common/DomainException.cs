using System;

namespace PlanningApp.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string? message) : base(message)
    {
    }
}
