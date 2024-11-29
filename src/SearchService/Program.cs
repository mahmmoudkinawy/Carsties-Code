using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddMassTransit(x =>
{
	x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

	x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

	x.UsingRabbitMq(
		(context, cfg) =>
		{
			cfg.UseMessageRetry(r =>
			{
				r.Handle<RabbitMqConnectionException>();
				r.Interval(5, TimeSpan.FromSeconds(10));
			});

			cfg.ReceiveEndpoint(
				"search-auction-created",
				e =>
				{
					e.UseMessageRetry(r => r.Interval(5, 5));

					e.ConfigureConsumer<AuctionCreatedConsumer>(context);
				}
			);

			cfg.Host(
				builder.Configuration["RabbitMq:Host"],
				"/",
				host =>
				{
					host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
					host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
				}
			);

			cfg.ConfigureEndpoints(context);
		}
	);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
	await Policy
		.Handle<TimeoutException>()
		.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(5))
		.ExecuteAndCaptureAsync(async () => await DbInitializer.InitDb(app));
});

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy() =>
	HttpPolicyExtensions
		.HandleTransientHttpError()
		.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
		.WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
