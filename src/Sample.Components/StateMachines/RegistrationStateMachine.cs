namespace Sample.Components.StateMachines;

using Contracts;
using MassTransit;

public class RegistrationStateMachine :
    MassTransitStateMachine<RegistrationState>
{
    public RegistrationStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => RegistrationSubmitted, x => x.CorrelateById(m => m.Message.RegistrationId));

        Schedule(() => RegistrationTimeout, instance => instance.TimeoutId,
            x => { x.Received = r => r.CorrelateById(context => context.Message.CorrelationId); });

        Initially(
            When(RegistrationSubmitted)
                .Then(context =>
                {
                    context.Saga.RegistrationDate = context.Message.RegistrationDate;
                    context.Saga.EventId = context.Message.EventId;
                    context.Saga.MemberId = context.Message.MemberId;
                    context.Saga.Payment = context.Message.Payment;
                })
                .TransitionTo(Registered)
                .Publish(context => new SendRegistrationEmail
                {
                    RegistrationId = context.Saga.CorrelationId,
                    RegistrationDate = context.Saga.RegistrationDate,
                    EventId = context.Saga.EventId,
                    MemberId = context.Saga.MemberId
                })
                //.Schedule(RegistrationTimeout,
                //    context => { return context.Init<RegistrationTimeout>(new { context.Saga.CorrelationId }); },
                //    context => {
                //        return TimeSpan.FromSeconds(30);
                //    })
                .If(context => context.Saga.Payment < 50m && context.GetRetryAttempt() == 0,
                    fail => fail.Then(_ => throw new ApplicationException("Totally random, but you didn't pay enough for quality service")))
                .Publish(context => new AddEventAttendee
                {
                    RegistrationId = context.Saga.CorrelationId,
                    EventId = context.Saga.EventId,
                    MemberId = context.Saga.MemberId
                })
               );

        During(Registered,
            When(RegistrationTimeout.Received)
                .If(x => x.Saga.TimeoutId.HasValue, then =>
                    then.Then(x =>
                            {
                                Console.WriteLine($"Timeout received");
                            }
                        )
                        .Schedule(RegistrationTimeout,
                            context =>
                            {
                                return context.Init<RegistrationTimeout>(new {context.Saga.CorrelationId});
                            },
                            context =>
                            {
                                return TimeSpan.FromSeconds(30);
                            })));

    }

    //
    // ReSharper disable MemberCanBePrivate.Global
    public State Registered { get; } = null!;
    public Event<RegistrationSubmitted> RegistrationSubmitted { get; } = null!;

    public Schedule<RegistrationState, RegistrationTimeout> RegistrationTimeout { get; } = null!;
}