using Keues.Application.Features.WebhooksConfig;
using Keues.Application.Features.WebhooksConfig.UpdateWebhooksConfig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class WebhooksConfigController : ControllerBase
{
    private readonly WebhooksConfigUseCases _webhooksConfigUseCases;

    public WebhooksConfigController(WebhooksConfigUseCases webhooksConfigUseCases)
    {
      _webhooksConfigUseCases = webhooksConfigUseCases;
    }

    [HttpGet]
    public async Task<IActionResult> GetWebhooksConfig()
    {
      var config = await _webhooksConfigUseCases.GetWebhooksConfigHandler.Handle();
      return Ok(config);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateWebhooksConfig(UpdateWebhooksCommand command)
    {
      await _webhooksConfigUseCases.UpdateWebhooksConfigHandler.Handle(command);
      return NoContent();
    }

    [HttpGet("events")]
    public IActionResult GetEvents()
    {
      var events = _webhooksConfigUseCases.GetEventsHandler.Handle();
      return Ok(events);
    }
    
  }
  
