using Keues.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.WebhooksConfig.GetWebhooksConfig;

public class GetWebhooksConfigHandler
{
  private readonly IApplicationDbContext _context;

  public GetWebhooksConfigHandler(IApplicationDbContext context)
  {
    _context = context;
  }
  
  public async Task<Domain.Entities.WebhooksConfig> Handle()
  {
    var config = await _context.WebhooksConfigs.FirstOrDefaultAsync();
    if (config == null)
    {
      throw new Exception("Webhooks config not found");
    }

    return config;
  }
}