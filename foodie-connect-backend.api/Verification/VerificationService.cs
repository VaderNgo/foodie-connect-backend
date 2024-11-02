using FluentEmail.Core;
using foodie_connect_backend.Data;
using foodie_connect_backend.Shared.Classes;
using foodie_connect_backend.Shared.Classes.Errors;
using Microsoft.AspNetCore.Identity;

namespace foodie_connect_backend.Verification;

public class VerificationService(UserManager<User> userManager, IFluentEmail fluentEmail)
{
    public async Task<Result<bool>> SendConfirmationEmail(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Result<bool>.Failure(AppError.RecordNotFound("User not found"));
        if (user.EmailConfirmed) return Result<bool>.Failure(AppError.ValidationError("Email is already confirmed"));
        
        // Send verification email
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var email = await fluentEmail
            .To(user.Email)
            .SetFrom("verify@account.foodie.town", "The Foodie team")
            .Subject("Verify your foodie town account")
            .Body($"Your verification token: {token}")
            .SendAsync();
        
        return Result<bool>.Success(true);
    }
    
    public async Task<Result<bool>> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Result<bool>.Failure(AppError.RecordNotFound("No user found"));
        
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return Result<bool>.Failure(AppError.BadToken("Token is not valid"));
        
        return Result<bool>.Success(result.Succeeded);
    }
}