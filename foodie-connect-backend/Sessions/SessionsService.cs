using System.Security.Claims;
using foodie_connect_backend.Data;
using foodie_connect_backend.Sessions.Dtos;
using foodie_connect_backend.Shared.Classes;
using Microsoft.AspNetCore.Identity;

namespace foodie_connect_backend.Sessions;

public class SessionsService(
    SignInManager<User> signInManager,
    UserManager<User> userManager)
{
    public async Task<Result<bool>> LoginHead(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null || await userManager.IsInRoleAsync(user, "User"))
            return Result<bool>.Failure(new AppError("InvalidCredentials", "Incorrect username or password"));

        var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Result<bool>.Failure(new AppError("InvalidCredentials", "Incorrect username or password"));

        return Result<bool>.Success(true);
    }
    
    public async Task<Result<bool>> LoginUser(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null || await userManager.IsInRoleAsync(user, "Head"))
            return Result<bool>.Failure(new AppError("InvalidCredentials", "Incorrect username or password"));

        var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Result<bool>.Failure(new AppError("InvalidCredentials", "Incorrect username or password"));

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Logout()
    {
        await signInManager.SignOutAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<SessionInfo>> GetUserSession(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Result<SessionInfo>.Failure(new AppError("InvalidSession", "This session is not valid"));
        var roles = await userManager.GetRolesAsync(user);
        var sessionResponse = new SessionInfo
        {
            Type = roles.FirstOrDefault()!,
            Id = user.Id,
            UserName = user.UserName!,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber!,
        };
        return Result<SessionInfo>.Success(sessionResponse);
    }
}