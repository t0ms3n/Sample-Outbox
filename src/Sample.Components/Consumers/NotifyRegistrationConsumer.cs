namespace Sample.Components.Consumers;

using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;


public class NotifyRegistrationConsumer :
    IConsumer<RegistrationSubmitted>
{
    readonly ILogger<NotifyRegistrationConsumer> _logger;

    public NotifyRegistrationConsumer(ILogger<NotifyRegistrationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RegistrationSubmitted> context)
    {
        _logger.LogInformation("Member {MemberId} registered for event {EventId} on {RegistrationDate}", context.Message.MemberId, context.Message.EventId,
            context.Message.RegistrationDate);

        await context.ScheduleSend(TimeSpan.FromSeconds(30),
            new RegistrationSubmitted() {EventId = Guid.NewGuid().ToString(), MemberId = Guid.NewGuid().ToString(), Payment = 50,
                RegistrationId = NewId.NextGuid(),
                RegistrationDate = DateTime.UtcNow,
            }, context.CancellationToken);

    }
}