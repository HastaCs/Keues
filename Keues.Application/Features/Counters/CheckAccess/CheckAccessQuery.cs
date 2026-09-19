namespace Keues.Application.Features.Counters.CheckAccess;

public record CheckAccessQuery(Guid CounterId, Guid? UserId) ;