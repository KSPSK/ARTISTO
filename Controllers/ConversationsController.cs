using ARTISTO.Extensions;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ConversationsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    [ProducesResponseType<List<ConversationSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ConversationSummaryResponse>>> GetConversations()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to access conversations."));
        }

        var result = await _chatService.GetUserConversationsAsync(userId);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost]
    [ProducesResponseType<ConversationDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailsResponse>> StartOrGetConversation(StartConversationRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to start a conversation."));
        }

        var result = await _chatService.StartOrGetConversationAsync(request, userId);

        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ConversationDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailsResponse>> GetById(int id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to access conversations."));
        }

        var result = await _chatService.GetConversationByIdAsync(id, userId);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => Forbid(),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:int}/messages")]
    [ProducesResponseType<IReadOnlyList<ChatMessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChatMessageResponse>>> GetMessages(int id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to access messages."));
        }

        var result = await _chatService.GetMessagesAsync(id, userId);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => Forbid(),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/messages")]
    [ProducesResponseType<ChatMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(int id, SendMessageRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to send a message."));
        }

        var result = await _chatService.SendMessageAsync(id, request, userId);

        return result.Status switch
        {
            ServiceStatus.Success => StatusCode(StatusCodes.Status201Created, result.Value),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => Forbid(),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
