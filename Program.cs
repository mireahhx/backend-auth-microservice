using AuthMicroservice.Consumers;
using AuthMicroservice.Services;
using MassTransit;
using Shared.Configuration.Extensions;
using Shared.Interfaces.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFilesFromDirectory(builder.Configuration.GetOrThrow("ConfigsDirectory"));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(massTransitConfig => {
    massTransitConfig.AddConsumer<LoginConsumer>();
    massTransitConfig.AddConsumer<RegisterConsumer>();

    massTransitConfig.AddRequestClient<IGetUserByUsernameRequest>(new Uri(builder.Configuration.GetOrThrow("Users:Endpoints:GetUserById:Uri")));
    massTransitConfig.AddRequestClient<ICreateUserRequest>(new Uri(builder.Configuration.GetOrThrow("Users:Endpoints:CreateUser:Uri")));

    massTransitConfig.UsingRabbitMq((context, rabbitMqConfig) => {
        rabbitMqConfig.Host(builder.Configuration.GetOrThrow("MessageBroker:Host"), hostConfig => {
            hostConfig.Username(builder.Configuration.GetOrThrow("MessageBroker:Username"));
            hostConfig.Password(builder.Configuration.GetOrThrow("MessageBroker:Password"));
        });

        rabbitMqConfig.ReceiveEndpoint(builder.Configuration.GetOrThrow("Auth:Endpoints:Login:Queue"), endpointConfig => {
            endpointConfig.Bind(builder.Configuration.GetOrThrow("Auth:Endpoints:Login:Exchange"), exchangeConfig => {
                exchangeConfig.ExchangeType = "direct";

                if (builder.Environment.IsDevelopment()) {
                    exchangeConfig.AutoDelete = true;
                    exchangeConfig.Durable = false;
                }
            });

            if (builder.Environment.IsDevelopment()) {
                endpointConfig.AutoDelete = true;
                endpointConfig.Durable = false;
            }

            endpointConfig.ConfigureConsumer<LoginConsumer>(context);
        });

        rabbitMqConfig.ReceiveEndpoint(builder.Configuration.GetOrThrow("Auth:Endpoints:Register:Queue"), endpointConfig => {
            endpointConfig.Bind(builder.Configuration.GetOrThrow("Auth:Endpoints:Register:Exchange"), exchangeConfig => {
                exchangeConfig.ExchangeType = "direct";

                if (builder.Environment.IsDevelopment()) {
                    exchangeConfig.AutoDelete = true;
                    exchangeConfig.Durable = false;
                }
            });

            if (builder.Environment.IsDevelopment()) {
                endpointConfig.AutoDelete = true;
                endpointConfig.Durable = false;
            }

            endpointConfig.ConfigureConsumer<RegisterConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
