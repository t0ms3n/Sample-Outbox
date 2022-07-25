using System.Diagnostics;
using MassTransit;
using MassTransit.Metadata;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample.Components;
using Sample.Components.Consumers;
using Sample.Components.StateMachines;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<RegistrationDbContext>(x =>
        {
            var connectionString = hostContext.Configuration.GetConnectionString("Default");

            x.UseNpgsql(connectionString, options =>
            {
                options.MinBatchSize(1);
            });
        });

        services.AddOpenTelemetryTracing(builder =>
        {
            builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("service")
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector())
                .AddSource("MassTransit")
                .AddJaegerExporter(o =>
                {
                    o.AgentHost = HostMetadataCache.IsRunningInContainer ? "jaeger" : "localhost";
                    o.AgentPort = 6831;
                    o.MaxPayloadSizeInBytes = 4096;
                    o.ExportProcessorType = ExportProcessorType.Batch;
                    o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                    {
                        MaxQueueSize = 2048,
                        ScheduledDelayMilliseconds = 5000,
                        ExporterTimeoutMilliseconds = 30000,
                        MaxExportBatchSize = 512,
                    };
                });
        });

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<RegistrationDbContext>(o =>
            {
                o.UsePostgres();

                o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
            });

            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<NotifyRegistrationConsumer, NotifyRegistrationConsumerDefinition>();
            x.AddConsumer<SendRegistrationEmailConsumer>();
            x.AddConsumer<AddEventAttendeeConsumer>();
            x.AddSagaStateMachine<RegistrationStateMachine, RegistrationState, RegistrationStateDefinition>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<RegistrationDbContext>();
                    r.UsePostgres();
                });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(HostMetadataCache.IsRunningInContainer ? "rabbitmq://localhost:5672" : "rabbitmq://localhost:5673" );
                cfg.ConfigureEndpoints(context);
            });
        });
    })
    .UseSerilog()
    .Build();

await host.RunAsync();