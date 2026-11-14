using Keues.Application.Features.Flows;
using Xunit;

namespace Keues.Tests.UseCases;

public class QrFlowJsonParserTests
{
  private static readonly Guid FirstQueueId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid SecondQueueId = Guid.Parse("22222222-2222-2222-2222-222222222222");

  [Fact]
  public void TryGetFirstQueueId_returns_queue_from_root_array()
  {
    var flowJson = $$"""
      [
        {
          "nodeType": "ticket",
          "queueId": "{{FirstQueueId}}"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(FirstQueueId, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_returns_queue_from_menu_items_object()
  {
    var flowJson = $$"""
      {
        "menuItems": [
          {
            "nodeType": "ticket",
            "queueId": "{{FirstQueueId}}"
          }
        ]
      }
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(FirstQueueId, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_returns_first_valid_queue_and_ignores_the_rest()
  {
    var flowJson = $$"""
      [
        {
          "nodeType": "ticket",
          "queueId": "{{FirstQueueId}}"
        },
        {
          "nodeType": "ticket",
          "queueId": "{{SecondQueueId}}"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(FirstQueueId, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_skips_non_ticket_nodes()
  {
    var flowJson = $$"""
      [
        {
          "nodeType": "menu",
          "queueId": "{{FirstQueueId}}"
        },
        {
          "nodeType": "ticket",
          "queueId": "{{SecondQueueId}}"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(SecondQueueId, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_skips_invalid_and_empty_queue_ids()
  {
    var flowJson = $$"""
      [
        {
          "nodeType": "ticket",
          "queueId": ""
        },
        {
          "nodeType": "ticket",
          "queueId": "not-a-guid"
        },
        {
          "nodeType": "ticket",
          "queueId": "{{FirstQueueId}}"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(FirstQueueId, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_accepts_ticket_without_node_type()
  {
    var flowJson = $$"""
      [
        {
          "queueId": "{{FirstQueueId}}"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.True(result);
    Assert.Equal(FirstQueueId, queueId);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void TryGetFirstQueueId_returns_false_for_empty_json(string? flowJson)
  {
    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.False(result);
    Assert.Equal(Guid.Empty, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_returns_false_for_malformed_json()
  {
    var result = QrFlowJsonParser.TryGetFirstQueueId("[{ \"queueId\":", out var queueId);

    Assert.False(result);
    Assert.Equal(Guid.Empty, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_returns_false_when_no_queue_id_is_present()
  {
    var flowJson = $$"""
      [
        {
          "nodeType": "ticket",
          "queueId": null
        },
        {
          "nodeType": "ticket"
        }
      ]
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.False(result);
    Assert.Equal(Guid.Empty, queueId);
  }

  [Fact]
  public void TryGetFirstQueueId_returns_false_when_root_is_not_supported()
  {
    var flowJson = $$"""
      {
        "queueId": "{{FirstQueueId}}"
      }
      """;

    var result = QrFlowJsonParser.TryGetFirstQueueId(flowJson, out var queueId);

    Assert.False(result);
    Assert.Equal(Guid.Empty, queueId);
  }
}
