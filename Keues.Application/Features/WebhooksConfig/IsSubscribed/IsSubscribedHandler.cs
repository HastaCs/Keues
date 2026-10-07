using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.WebhooksConfig.IsSubscribed;

public class IsSubscribedHandler
{
  private readonly IApplicationDbContext _context;

  public IsSubscribedHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<bool> Handle(string eventType)
  {
    var config = await _context.WebhooksConfigs.FirstOrDefaultAsync();
    return config is { Enabled: true } && config.Events.Contains(eventType);
  }
}