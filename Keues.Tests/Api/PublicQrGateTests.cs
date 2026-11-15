using Keues.API.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Keues.Tests.Api;

public class PublicQrGateTests
{
  [Theory]
  [InlineData("GET", "/turno", true)]
  [InlineData("GET", "/turno/flow/token", true)]
  [InlineData("GET", "/assets/app.js", true)]
  [InlineData("GET", "/favicon.ico", true)]
  [InlineData("GET", "/api/flows/36ba437b-bc9d-462c-922a-fb66c9dc5b3d", true)]
  [InlineData("GET", "/api/tickets/11111111-1111-1111-1111-111111111111", true)]
  [InlineData("POST", "/api/queues/c1543a44-a69a-486f-8109-44f2fed5707e/qr-new-ticket", true)]
  [InlineData("GET", "/locations", false)]
  [InlineData("GET", "/", false)]
  [InlineData("GET", "/api/locations", false)]
  [InlineData("GET", "/api/queues", false)]
  [InlineData("GET", "/api/queues/c1543a44-a69a-486f-8109-44f2fed5707e/new-ticket", false)]
  [InlineData("GET", "/api/tickets", false)]
  [InlineData("GET", "/api/users/me", false)]
  [InlineData("GET", "/devices", false)]
  [InlineData("POST", "/api/queues/c1543a44-a69a-486f-8109-44f2fed5707e/new-ticket", false)]
  [InlineData("GET", "/api/queues/c1543a44-a69a-486f-8109-44f2fed5707e/qr-new-ticket", false)]
  public void IsAllowed_only_opens_qr_routes(string method, string path, bool expected)
  {
    var context = new DefaultHttpContext();
    context.Request.Method = method;
    context.Request.Path = path;

    Assert.Equal(expected, PublicQrGate.IsAllowed(context.Request));
  }
}
