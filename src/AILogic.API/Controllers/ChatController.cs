using AILogic.API.Models;
using AILogic.Application.Models;
using AILogic.Application.Services;
using AILogic.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AILogic.API.Controllers;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("chat")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ChatReply>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatReply>> Send(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString("N")
            : request.ConversationId.Trim();

        var reply = await chatService.ReplyAsync(
            Channel.Web,
            conversationId,
            request.Message,
            cancellationToken);

        return Ok(reply);
    }
}
