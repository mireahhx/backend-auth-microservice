using MassTransit;
using Shared.Configuration.Extensions;
using Shared.Interfaces.Users;
using Shared.Results;
using Shared.Utils;

namespace AuthMicroservice.Services {
    public interface IAuthService {
        Task<ServiceResult<string>> Login(string username, string password);
        Task<ServiceResult<string>> Register(string username, string password);
    }

    public class AuthService(IConfiguration configuration, IRequestClient<IGetUserByUsernameRequest> getUserByUsernameClient, IRequestClient<ICreateUserRequest> createUserClient) : IAuthService {
        private readonly IRequestClient<IGetUserByUsernameRequest> _getUserByUsernameClient = getUserByUsernameClient;
        private readonly IRequestClient<ICreateUserRequest> _createUserClient = createUserClient;
        private readonly IConfiguration _configuration = configuration;

        public async Task<ServiceResult<string>> Login(string username, string password) {
            var generator = new ServiceResult<string>.Generator();

            var getUserResponse = await _getUserByUsernameClient.GetResponse<IGetUserByUsernameResponse, ServiceError>(new() {
                Username = username
            }, responseConfig => {
                responseConfig.UseExecute(executeConfig => {
                    executeConfig.SetRoutingKey(_configuration.GetOrThrow("Users:Endpoints:GetUserByUsername:RoutingKey"));
                });
            });

            if (getUserResponse.Is(out Response<ServiceError> error)) {
                return generator.Error(error.Message.Error);
            } else if (getUserResponse.Is(out Response<IGetUserByUsernameResponse> user)) {
                if (Password.Verify(password, user.Message.Password)) {
                    return generator.Success($"ACCESS_TOKEN by id {user.Message.Id}");
                } else {
                    return generator.Error("Wrong password");
                }
            }

            throw new InvalidOperationException();
        }

        public async Task<ServiceResult<string>> Register(string username, string password) {
            var generator = new ServiceResult<string>.Generator();

            var getUserResponse = await _getUserByUsernameClient.GetResponse<IGetUserByUsernameResponse, ServiceError>(new() {
                Username = username
            }, responseConfig => {
                responseConfig.UseExecute(executeConfig => {
                    executeConfig.SetRoutingKey(_configuration.GetOrThrow("Users:Endpoints:GetUserByUsername:RoutingKey"));
                });
            });

            if (getUserResponse.Is(out Response<ServiceError> getUserError)) {
                var createUserResponse = await _createUserClient.GetResponse<ICreateUserResponse, ServiceError>(new() {
                    Username = username,
                    Password = Password.Hash(password)
                });

                if (createUserResponse.Is(out Response<ICreateUserResponse> createUser)) {
                    return generator.Success($"ACCESS_TOKEN by id {createUser.Message.Id}");
                }
            } else if (getUserResponse.Is(out Response<IGetUserByUsernameResponse> getUser)) {
                return generator.Error("User is already registered");
            }

            throw new InvalidOperationException();
        }
    }
}
