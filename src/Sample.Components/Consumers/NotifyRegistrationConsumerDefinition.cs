using MassTransit;
namespace Sample.Components.Consumers;

public class NotifyRegistrationConsumerDefinition : ConsumerDefinition<NotifyRegistrationConsumer>
{
    private readonly IServiceProvider _serviceProvider;

    public NotifyRegistrationConsumerDefinition(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<NotifyRegistrationConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<RegistrationDbContext>(_serviceProvider);
    }
}