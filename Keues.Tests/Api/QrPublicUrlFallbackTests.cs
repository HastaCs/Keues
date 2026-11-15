using Keues.API.Hubs;
using Keues.Application.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Keues.Tests.Api;

public class QrPublicUrlFallbackTests
{
  [Fact]
  public void ApplyFromRequest_sets_the_request_host_when_no_base_url_is_configured()
  {
    var options = new QrOptions();
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("192.168.1.50", 8080);

    QrPublicUrlFallback.ApplyFromRequest(options, context.Request);

    Assert.Equal("http://192.168.1.50:8081", options.PublicBaseUrl);
  }

  [Fact]
  public void ApplyFromRequest_keeps_a_configured_base_url()
  {
    var options = new QrOptions { PublicBaseUrl = "https://qr.example.com" };
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("192.168.1.50", 8080);

    QrPublicUrlFallback.ApplyFromRequest(options, context.Request);

    Assert.Equal("https://qr.example.com", options.PublicBaseUrl);
  }

  [Theory]
  [InlineData("localhost")]
  [InlineData("127.0.0.1")]
  public void ApplyFromRequest_ignores_loopback_hosts(string host)
  {
    var options = new QrOptions();
    var context = new DefaultHttpContext();
    context.Request.Scheme = "http";
    context.Request.Host = new HostString(host, 8080);

    QrPublicUrlFallback.ApplyFromRequest(options, context.Request);

    Assert.Equal(string.Empty, options.PublicBaseUrl);
  }
}
