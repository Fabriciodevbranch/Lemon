using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController, Authorize]
[Route("api/books/{bookId:guid}/studio/characters/{characterId:guid}")]
public sealed class CharacterConnectionsController(ICharacterConnectionsService service, ICurrentUserService currentUser,
    IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("media")] public async Task<IActionResult> Media(Guid bookId, Guid characterId, CancellationToken ct) => await Run(bookId, ct, () => service.ListMediaAsync(bookId, characterId, ct));
    [HttpPost("media")] public async Task<IActionResult> AddMedia(Guid bookId, Guid characterId, SaveCharacterMediaReferenceDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveMediaAsync(bookId, characterId, null, request, ct));
    [HttpPut("media/{id:guid}")] public async Task<IActionResult> EditMedia(Guid bookId, Guid characterId, Guid id, SaveCharacterMediaReferenceDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveMediaAsync(bookId, characterId, id, request, ct));
    [HttpDelete("media/{id:guid}")] public async Task<IActionResult> RemoveMedia(Guid bookId, Guid characterId, Guid id, CancellationToken ct) => await Delete(bookId, ct, () => service.DeleteMediaAsync(bookId, characterId, id, ct));
    [HttpGet("timeline")] public async Task<IActionResult> Timeline(Guid bookId, Guid characterId, CancellationToken ct) => await Run(bookId, ct, () => service.ListTimelineAsync(bookId, characterId, ct));
    [HttpPost("timeline")] public async Task<IActionResult> AddTimeline(Guid bookId, Guid characterId, SaveCharacterTimelineReferenceDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveTimelineAsync(bookId, characterId, null, request, ct));
    [HttpPut("timeline/{id:guid}")] public async Task<IActionResult> EditTimeline(Guid bookId, Guid characterId, Guid id, SaveCharacterTimelineReferenceDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveTimelineAsync(bookId, characterId, id, request, ct));
    [HttpDelete("timeline/{id:guid}")] public async Task<IActionResult> RemoveTimeline(Guid bookId, Guid characterId, Guid id, CancellationToken ct) => await Delete(bookId, ct, () => service.DeleteTimelineAsync(bookId, characterId, id, ct));
    [HttpGet("attributes")] public async Task<IActionResult> Attributes(Guid bookId, Guid characterId, CancellationToken ct) => await Run(bookId, ct, () => service.ListAttributesAsync(bookId, characterId, ct));
    [HttpPost("attributes")] public async Task<IActionResult> AddAttribute(Guid bookId, Guid characterId, SaveCharacterAttributeDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveAttributeAsync(bookId, characterId, null, request, ct));
    [HttpPut("attributes/{id:guid}")] public async Task<IActionResult> EditAttribute(Guid bookId, Guid characterId, Guid id, SaveCharacterAttributeDto request, CancellationToken ct) => await Run(bookId, ct, () => service.SaveAttributeAsync(bookId, characterId, id, request, ct));
    [HttpDelete("attributes/{id:guid}")] public async Task<IActionResult> RemoveAttribute(Guid bookId, Guid characterId, Guid id, CancellationToken ct) => await Delete(bookId, ct, () => service.DeleteAttributeAsync(bookId, characterId, id, ct));

    private async Task<IActionResult> Run<T>(Guid bookId, CancellationToken ct, Func<Task<LemonWriter.Application.Common.Errors.Result<T>>> action)
    { if (!await Owns(bookId, ct)) return NotFound(); var result = await action(); return result.IsSuccess ? Ok(result.Value) : result.Error == LemonWriter.Application.Common.Errors.Error.NotFound ? NotFound() : BadRequest(new { message = result.Error!.Message }); }
    private async Task<IActionResult> Delete(Guid bookId, CancellationToken ct, Func<Task<LemonWriter.Application.Common.Errors.Result>> action)
    { if (!await Owns(bookId, ct)) return NotFound(); var result = await action(); return result.IsSuccess ? NoContent() : NotFound(); }
    private Task<bool> Owns(Guid bookId, CancellationToken ct) => currentUser.UserId is Guid userId ? authorization.OwnsBookAsync(userId, bookId, ct) : Task.FromResult(false);
}
