using AuthMicroservice.Services;
using MassTransit;
using Shared.Interfaces.Auth;

namespace AuthMicroservice.Consumers {
    public class VerifyConsumer(IAuthService authService) : IConsumer<IVerifyRequest> {
        private readonly IAuthService _authService = authService;

        public async Task Consume(ConsumeContext<IVerifyRequest> context) {

        }
    }
}
