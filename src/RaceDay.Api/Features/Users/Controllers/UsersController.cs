using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

/// <summary>User profiles. Requires a logged-in user.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IFileStorageService _storage;

    public UsersController(RaceDayDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        return user is null ? NotFound() : Ok(user.ToDto());
    }

    /// <summary>Updates the logged-in user's own profile (full name and phone number).</summary>
    /// <remarks>Expects { fullName, phoneNumber }. Email and role cannot be changed here.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return NotFound();

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        await _db.SaveChangesAsync();

        return Ok(user.ToDto());
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile picture in Azure Blob Storage.</summary>
    /// <param name="request">multipart/form-data with a single image field named "file" (JPG, PNG, WEBP or GIF, max 5 MB).</param>
    /// <response code="200">The updated profile, including profilePictureUrl.</response>
    /// <response code="400">No file, wrong type or too large.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("me/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UploadPhoto([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return NotFound();

        var error = ImageUploadRules.Validate(request.File, out var extension, out var contentType);
        if (error is not null)
        {
            ModelState.AddModelError("File", error);
            return ValidationProblem(ModelState);
        }

        var oldUrl = user.ProfilePictureUrl;
        await using (var stream = request.File!.OpenReadStream())
        {
            user.ProfilePictureUrl = await _storage.UploadAsync(stream, contentType, "profile-pictures", extension, ct);
        }
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(oldUrl, ct);

        return Ok(user.ToDto());
    }

    /// <summary>Returns a user's public profile. Organisers only (e.g. to view a Participant who enrolled).</summary>
    /// <param name="id">The user's id.</param>
    /// <response code="200">The public profile (id, name, role).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">No such user.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(PublicUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicUserDto>> GetUser(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        return user is null ? NotFound() : Ok(new PublicUserDto(user.UserId, user.FullName, user.Role));
    }
}
