using foodie_connect_backend.Data;
using foodie_connect_backend.Modules.Heads.Dtos;
using foodie_connect_backend.Modules.Uploader;
using foodie_connect_backend.Modules.Verification;
using foodie_connect_backend.Shared.Classes;
using foodie_connect_backend.Shared.Classes.Errors;
using foodie_connect_backend.Shared.Dtos;
using Microsoft.AspNetCore.Identity;

namespace foodie_connect_backend.Modules.Heads;

public class HeadsService(
    UserManager<User> userManager,
    VerificationService verificationService,
    IUploaderService uploaderService)
{
    public async Task<Result<User>> CreateHead(CreateHeadDto head)
    {
        var newHead = new User
        {
            DisplayName = head.DisplayName,
            PhoneNumber = head.PhoneNumber,
            UserName = head.UserName,
            Email = head.Email
        };

        var result = await userManager.CreateAsync(newHead, head.Password);
        await userManager.AddToRoleAsync(newHead, "Head");
        
        if (!result.Succeeded)
        {
            return result.Errors.FirstOrDefault()?.Code switch
            {
                "DuplicateUserName" => Result<User>.Failure(UserError.DuplicateUsername(head.UserName)),
                "DuplicateEmail" => Result<User>.Failure(UserError.DuplicateEmail(head.Email)),
                _ => Result<User>.Failure(AppError.UnexpectedError(result.Errors.First().Description))
            };
        }

        // Send verification email
        // This introduces tight-coupling between Heads and Verification service
        // TODO: Implement a pub/sub that invokes and consumes UserRegistered event
        await verificationService.SendConfirmationEmail(newHead.Id);

        return Result<User>.Success(newHead);
    }

    public async Task<Result<User>> GetHeadById(string id)
    {
        var head = await userManager.FindByIdAsync(id);
        if (head == null || !await userManager.IsInRoleAsync(head, "Head"))
            return Result<User>.Failure(UserError.UserNotFound(id));

        return Result<User>.Success(head);
    }

    public async Task<Result<bool>> ChangePassword(string userId, ChangePasswordDto changePasswordDto)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null || !await userManager.IsInRoleAsync(user, "Head"))
            return Result<bool>.Failure(UserError.UserNotFound(userId));
        
        var result = await userManager.ChangePasswordAsync(user, changePasswordDto.OldPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            return result.Errors.First().Code switch
            {
                "PasswordMismatch" => Result<bool>.Failure(UserError.PasswordMismatch()),
                _ => Result<bool>.Failure(AppError.UnexpectedError(result.Errors.First().Description))
            };
        }
        
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UploadAvatar(string userId, IFormFile file)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null || !await userManager.IsInRoleAsync(user, "Head"))
            return Result<bool>.Failure(UserError.UserNotFound(userId));

        var uploadParams = new ImageFileOptions()
        {
            Format = "webp",
            PublicId = userId,
            Folder = "foodie/user_avatars"
        };
        var uploadResult = await uploaderService.UploadImageAsync(file, uploadParams);

        if (uploadResult.IsSuccess)
        {
            user.AvatarId = uploadResult.Value;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Result<bool>.Failure(AppError.InternalError());
            
            return Result<bool>.Success(true);
        }

        return Result<bool>.Failure(uploadResult.Error);
    }
}