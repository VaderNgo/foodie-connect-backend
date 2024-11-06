using System.Security.Claims;
using foodie_connect_backend.Data;
using foodie_connect_backend.Modules.Users.Dtos;
using foodie_connect_backend.Shared.Classes.Errors;
using foodie_connect_backend.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace foodie_connect_backend.Modules.Users
{
    [Route("v1/users")]
    [ApiController]
    [Produces("application/json")]
    public class UsersController(UsersService usersService) : ControllerBase
    {
        /// <summary>
        /// Create a USER account
        /// </summary>
        /// <param name="user"></param>
        /// <returns>The newly created USER account</returns>
        /// <response code="201">Returns the newly created USER account</response>
        /// <response code="400">Request body does not meet specified requirements</response>
        /// <response code="409">Username or Email is taken</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<User>> CreateUser(CreateUserDto user)
        {
            var result = await usersService.CreateUser(user);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    UserError.DuplicateUsernameCode => Conflict(result.Error),
                    UserError.DuplicateEmailCode => Conflict(result.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                };
            }

            var responseDto = new UserResponseDto()
            {
                Id = result.Value.Id,
                UserName = result.Value.UserName!,
                DisplayName = result.Value.DisplayName,
            };

            return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, responseDto);
        }



        /// <summary>
        /// Query basic information about a USER account
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Information about the USER account without sensitive information</returns>
        /// <response code="200">Returns the USER account information</response>
        /// <response code="404">USER account not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var result = await usersService.GetUserById(id);
            if (result.IsFailure) return NotFound(result.Error);

            var responseDto = new UserResponseDto()
            {
                Id = result.Value.Id,
                UserName = result.Value.UserName!,
                DisplayName = result.Value.DisplayName,
                Avatar = result.Value.AvatarId,
            };
            return Ok(responseDto);
        }



        /// <summary>
        /// Change the logged-in user avatar
        /// </summary>
        /// <param name="id"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <response code="200">Successfully uploaded avatar</response>
        /// <response code="400">Invalid image. Allowed extensions are png, jpg, jpeg, and webp. Image must be less than 5MB</response>
        /// <response code="401">Not logged in</response>
        /// <response code="403">Not authorized to change this user's avatar</response>
        /// <response code="404">No user with this id was found</response>
        [HttpPatch("{id}/avatar")]
        [ProducesResponseType(typeof(GenericResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<GenericResponse>> UploadAvatar(string id, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || userId != id)
                return Unauthorized(AuthError.NotAuthorized());

            var result = await usersService.UploadAvatar(id, file);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    UserError.UserNotFoundCode => NotFound(result.Error),
                    UploadError.ExceedMaxSizeCode => BadRequest(result.Error),
                    UploadError.TypeNotAllowedCode => BadRequest(result.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                };
            }

            return Ok(new GenericResponse() { Message = "Avatar updated successfully" });
        }



        /// <summary>
        /// Change a USER account password
        /// </summary>
        /// <param name="id"></param>
        /// <param name="changePasswordDto"></param>
        /// <returns>Password change result</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid request body or new password does not meet requirements</response>
        /// <response code="401">Not authorized to change another user's password or old password is incorrect</response>
        /// <response code="404">No USER account found</response>
        [HttpPatch("{id}/password")]
        [ProducesResponseType(typeof(GenericResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<ActionResult<GenericResponse>> ChangePassword(string id, ChangePasswordDto changePasswordDto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || userId != id) return Unauthorized(AuthError.NotAuthorized());

            var result = await usersService.ChangePassword(id, changePasswordDto);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    UserError.UserNotFoundCode => NotFound(result.Error),
                    UserError.PasswordMismatchCode => Unauthorized(result.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                };
            }
            
            return Ok(new GenericResponse { Message = "Password changed successfully" });
        }



        /// <summary>
        /// Upgrade a USER to a HEAD account. This action is not reversible. This will destroy the user's current session
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Successfully upgraded the user account. REQUIRES USER TO RE-LOGIN</response>
        /// <response code="400">Something unexpected happened</response>
        /// <response code="401">Not authorized to perform this action. Possible invalid id provided</response>
        /// <response code="403">User is already a HEAD account</response>
        [HttpPatch("{id}/type")]
        [ProducesResponseType(typeof(GenericResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<GenericResponse>> UpgradeToHead(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || userId != id) return Unauthorized(AuthError.NotAuthorized());

            var upgradeResult = await usersService.UpgradeToHead(userId);
            if (upgradeResult.IsFailure)
                return upgradeResult.Error.Code switch
                {
                    UserError.UserNotFoundCode => NotFound(upgradeResult.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, upgradeResult.Error)
                };

            return Ok(new GenericResponse { Message = "Upgraded user to head successfully" });
        }
    }
}
