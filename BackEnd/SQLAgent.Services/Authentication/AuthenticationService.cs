using BCrypt.Net;
using Serilog;
using SQLAgent.Core.DTOs;
using SQLAgent.Core.Models;
using SQLAgent.Infrastructure.Data.Repositories;

namespace SQLAgent.Services.Authentication;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger _logger;

    public AuthenticationService(IUserRepository userRepository, ITokenService tokenService, ILogger logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.Warning("Login attempt with empty credentials");
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                };
            }

            var user = await _userRepository.GetByUsernameAsync(request.Username);
            
            if (user == null)
            {
                _logger.Warning("Failed login attempt for user: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.Warning("Failed login attempt for user: {Username} - invalid password", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            if (!user.IsActive)
            {
                _logger.Warning("Login attempt for inactive user: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "User account is inactive"
                };
            }

            var token = _tokenService.GenerateToken(user);

            _logger.Information("Successful login for user: {Username}", request.Username);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during login");
            return new LoginResponse
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username, email, and password are required"
                };
            }

            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.Warning("Registration attempt with existing username: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }

            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                _logger.Warning("Registration attempt with existing email: {Email}", request.Email);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.Information("New user registered: {Username}", request.Username);

            var token = _tokenService.GenerateToken(user);

            return new LoginResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during registration");
            return new LoginResponse
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }
}
