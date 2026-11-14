using Keues.Application.Common;
using Keues.Application.DeviceRegistry.Messages;
using Keues.Application.Features.Qr;
using Keues.Domain.Enums;
using Keues.Infrastructure.Authorization;
using Keues.Infrastructure.Persistence;
using Keues.Infrastructure.Qr;
using Keues.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keues.Tests.UseCases;

public class QrTokenRotationServiceTests : IDisposable
{
  private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task BuildPayloads_returns_a_payload_for_a_qr_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var (service, tokenService) = CreateService(context);

    var payloads = await service.BuildPayloadsAsync(BaseTime);

    var payload = Assert.Single(payloads);
    Assert.Equal(flow.Id, payload.FlowId);
    Assert.Equal(location.Id, payload.LocationId);
    Assert.Equal(tokenService.Create(flow.Id, BaseTime), payload.Token);
    Assert.Equal(BaseTime.AddMinutes(5), payload.RotatesAt);
    Assert.Equal(BaseTime.AddMinutes(10), payload.ExpiresAt);
    Assert.Equal(
      $"https://keues.test/qr/{flow.Id}?token={Uri.EscapeDataString(payload.Token)}",
      payload.Url);
  }

  [Fact]
  public async Task BuildPayloads_ignores_non_qr_flows()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.TicketMachine, "Ticket machine");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var (service, _) = CreateService(context);

    Assert.Empty(await service.BuildPayloadsAsync(BaseTime));
  }

  [Fact]
  public async Task BuildPayloads_ignores_qr_flows_without_a_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = "[]";
    await context.SaveChangesAsync();
    var (service, _) = CreateService(context);

    Assert.Empty(await service.BuildPayloadsAsync(BaseTime));
  }

  [Fact]
  public async Task BuildPayloads_returns_all_valid_qr_flows()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var firstQueue = await Seed.QueueAsync(context, location.Id, code: "A");
    var secondQueue = await Seed.QueueAsync(context, location.Id, code: "B");
    var firstFlow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR A");
    firstFlow.FlowJson = FlowJsonWithQueue(firstQueue.Id);
    var secondFlow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR B");
    secondFlow.FlowJson = FlowJsonWithQueue(secondQueue.Id);
    await context.SaveChangesAsync();
    var (service, _) = CreateService(context);

    var payloads = await service.BuildPayloadsAsync(BaseTime);

    Assert.Equal(2, payloads.Count);
    Assert.Contains(payloads, payload => payload.FlowId == firstFlow.Id);
    Assert.Contains(payloads, payload => payload.FlowId == secondFlow.Id);
  }

  private static (QrTokenRotationService Service, IQrTokenService TokenService) CreateService(
    AppDbContext context)
  {
    var options = new QrOptions { SlotSeconds = 300, PublicBaseUrl = "https://keues.test" };
    var tokenService = new QrTokenService(
      Options.Create(new JwtOptions { Key = "test-qr-key-that-is-long-enough-0123456789" }),
      Options.Create(options));
    var factory = new QrTokenPayloadFactory(
      new QrFlowResolver(context),
      tokenService,
      Options.Create(options));
    var service = new QrTokenRotationService(
      context,
      factory,
      NullLogger<QrTokenRotationService>.Instance);

    return (service, tokenService);
  }

  private static string FlowJsonWithQueue(Guid queueId) => $$"""
    [
      {
        "nodeType": "ticket",
        "queueId": "{{queueId}}"
      }
    ]
    """;
}
