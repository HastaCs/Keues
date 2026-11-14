using Keues.Application.Common;
using Keues.Infrastructure.Authorization;
using Keues.Infrastructure.Qr;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keues.Tests.UseCases;

public class QrTokenServiceTests
{
  private static readonly Guid FlowId = Guid.Parse("33333333-3333-3333-3333-333333333333");
  private static readonly Guid OtherFlowId = Guid.Parse("44444444-4444-4444-4444-444444444444");
  private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

  private readonly QrTokenService _service = new(
    Options.Create(new JwtOptions { Key = "test-qr-key-that-is-long-enough-0123456789" }),
    Options.Create(new QrOptions { SlotSeconds = 300 }));

  [Fact]
  public void GetPeriod_aligns_to_five_minute_boundaries()
  {
    var beforeBoundary = _service.GetPeriod(new DateTimeOffset(2026, 1, 1, 12, 4, 59, TimeSpan.Zero));
    var atBoundary = _service.GetPeriod(new DateTimeOffset(2026, 1, 1, 12, 5, 0, TimeSpan.Zero));

    Assert.Equal(BaseTime, beforeBoundary.Start);
    Assert.Equal(TimeSpan.FromMinutes(5), beforeBoundary.Duration);
    Assert.Equal(BaseTime.AddMinutes(5), atBoundary.Start);
  }

  [Fact]
  public void Create_returns_a_url_safe_token()
  {
    var token = _service.Create(FlowId, BaseTime);

    Assert.False(string.IsNullOrWhiteSpace(token));
    Assert.DoesNotContain('+', token);
    Assert.DoesNotContain('/', token);
    Assert.DoesNotContain('=', token);
  }

  [Fact]
  public void Create_is_deterministic_for_the_same_period()
  {
    var first = _service.Create(FlowId, BaseTime);
    var second = _service.Create(FlowId, BaseTime.AddMinutes(4).AddSeconds(59));

    Assert.Equal(first, second);
  }

  [Fact]
  public void Create_changes_between_periods()
  {
    var first = _service.Create(FlowId, BaseTime);
    var second = _service.Create(FlowId, BaseTime.AddMinutes(5));

    Assert.NotEqual(first, second);
  }

  [Fact]
  public void Create_changes_between_flows()
  {
    var first = _service.Create(FlowId, BaseTime);
    var second = _service.Create(OtherFlowId, BaseTime);

    Assert.NotEqual(first, second);
  }

  [Fact]
  public void Validate_accepts_the_current_period_token()
  {
    var token = _service.Create(FlowId, BaseTime.AddMinutes(1));

    Assert.True(_service.Validate(FlowId, token, BaseTime.AddMinutes(2)));
  }

  [Fact]
  public void Validate_accepts_the_previous_period_token()
  {
    var token = _service.Create(FlowId, BaseTime);

    Assert.True(_service.Validate(FlowId, token, BaseTime.AddMinutes(7)));
  }

  [Fact]
  public void Validate_rejects_a_token_from_two_periods_ago()
  {
    var token = _service.Create(FlowId, BaseTime);

    Assert.False(_service.Validate(FlowId, token, BaseTime.AddMinutes(10)));
  }

  [Fact]
  public void Validate_rejects_a_future_token()
  {
    var token = _service.Create(FlowId, BaseTime.AddMinutes(10));

    Assert.False(_service.Validate(FlowId, token, BaseTime.AddMinutes(5)));
  }

  [Fact]
  public void Validate_rejects_a_token_for_another_flow()
  {
    var token = _service.Create(OtherFlowId, BaseTime);

    Assert.False(_service.Validate(FlowId, token, BaseTime));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not-a-token")]
  [InlineData("AAAA")]
  public void Validate_rejects_missing_or_malformed_tokens(string? token)
  {
    Assert.False(_service.Validate(FlowId, token, BaseTime));
  }

  [Fact]
  public void Validate_accepts_the_token_until_the_previous_period_ends()
  {
    var token = _service.Create(FlowId, BaseTime);

    Assert.True(_service.Validate(FlowId, token, BaseTime.AddMinutes(9).AddSeconds(59)));
    Assert.False(_service.Validate(FlowId, token, BaseTime.AddMinutes(10)));
  }
}
