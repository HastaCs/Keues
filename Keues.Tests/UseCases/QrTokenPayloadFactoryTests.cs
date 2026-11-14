using Keues.Application.Common;
using Keues.Application.Features.Qr;
using Keues.Domain.Enums;
using Keues.Infrastructure.Authorization;
using Keues.Infrastructure.Persistence;
using Keues.Infrastructure.Qr;
using Keues.Tests.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keues.Tests.UseCases;

public class QrTokenPayloadFactoryTests : IDisposable
{
  private static readonly Guid OtherFlowId = Guid.Parse("55555555-5555-5555-5555-555555555555");
  private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

  private readonly TestDatabaseFactory _db = new();

  public void Dispose() => _db.Dispose();

  [Fact]
  public async Task Build_returns_a_payload_for_a_qr_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var (factory, tokenService) = CreateFactory(context);

    var payload = await factory.BuildAsync(flow.Id, BaseTime);

    Assert.NotNull(payload);
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
  public async Task Build_returns_a_relative_url_when_no_public_base_url_is_configured()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var (factory, _) = CreateFactory(context, publicBaseUrl: "");

    var payload = await factory.BuildAsync(flow.Id, BaseTime);

    Assert.NotNull(payload);
    Assert.StartsWith($"/qr/{flow.Id}?token=", payload.Url);
  }

  [Fact]
  public async Task Build_returns_null_for_a_non_qr_flow()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.TicketMachine, "Ticket machine");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    await context.SaveChangesAsync();
    var (factory, _) = CreateFactory(context);

    Assert.Null(await factory.BuildAsync(flow.Id, BaseTime));
  }

  [Fact]
  public async Task Build_returns_null_for_a_qr_flow_without_a_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = "[]";
    await context.SaveChangesAsync();
    var (factory, _) = CreateFactory(context);

    Assert.Null(await factory.BuildAsync(flow.Id, BaseTime));
  }

  [Fact]
  public async Task Build_returns_null_for_a_queue_from_another_location()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context, "Origen");
    var otherLocation = await Seed.LocationAsync(context, "Otra");
    var otherQueue = await Seed.QueueAsync(context, otherLocation.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = FlowJsonWithQueue(otherQueue.Id);
    await context.SaveChangesAsync();
    var (factory, _) = CreateFactory(context);

    Assert.Null(await factory.BuildAsync(flow.Id, BaseTime));
  }

  [Fact]
  public async Task Build_returns_null_for_a_soft_deleted_queue()
  {
    await using var context = _db.CreateContext();
    var location = await Seed.LocationAsync(context);
    var queue = await Seed.QueueAsync(context, location.Id);
    var flow = await Seed.FlowAsync(context, location.Id, FlowType.QrVirtual, "QR virtual");
    flow.FlowJson = FlowJsonWithQueue(queue.Id);
    queue.RemovedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();
    var (factory, _) = CreateFactory(context);

    Assert.Null(await factory.BuildAsync(flow.Id, BaseTime));
  }

  [Fact]
  public async Task Build_returns_null_for_an_unknown_flow()
  {
    await using var context = _db.CreateContext();
    var (factory, _) = CreateFactory(context);

    Assert.Null(await factory.BuildAsync(OtherFlowId, BaseTime));
  }

  [Fact]
  public async Task Resolve_throws_when_the_qr_flow_is_not_available()
  {
    await using var context = _db.CreateContext();
    var resolver = new QrFlowResolver(context);

    await Assert.ThrowsAsync<Exception>(() => resolver.ResolveAsync(OtherFlowId));
  }

  private static (QrTokenPayloadFactory Factory, IQrTokenService TokenService) CreateFactory(
    AppDbContext context,
    string publicBaseUrl = "https://keues.test")
  {
    var options = new QrOptions { SlotSeconds = 300, PublicBaseUrl = publicBaseUrl };
    var tokenService = new QrTokenService(
      Options.Create(new JwtOptions { Key = "test-qr-key-that-is-long-enough-0123456789" }),
      Options.Create(options));
    var factory = new QrTokenPayloadFactory(
      new QrFlowResolver(context),
      tokenService,
      Options.Create(options));

    return (factory, tokenService);
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
