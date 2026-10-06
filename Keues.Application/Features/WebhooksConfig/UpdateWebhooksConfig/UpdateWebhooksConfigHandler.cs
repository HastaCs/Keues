using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.WebhooksConfig.UpdateWebhooksConfig;

public class UpdateWebhooksConfigHandler
{
  private readonly IApplicationDbContext _context;

  public UpdateWebhooksConfigHandler(IApplicationDbContext context)
  {
    _context = context;
  }

  public async Task Handle(UpdateWebhooksCommand command)
  {
    var config = await _context.WebhooksConfigs.FirstOrDefaultAsync();
    if (config == null)
    {
      throw new Exception("Webhooks config not found");
    }

    config.Url = command.Url;
    config.Events = command.Events;
    config.Enabled = command.Enabled;
    config.Key = command.Key;

    await _context.SaveChangesAsync();
  }
}