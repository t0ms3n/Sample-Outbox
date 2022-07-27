namespace Sample.Components.Contracts;

public record RegistrationTimeout
{
    public Guid CorrelationId { get; init; }
}