using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class LogFilters : AspNetCoreTest
    {
        public LogFilters(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task LogFiltersAsync()
        {
            Error error = null;
            using (var server = GetServer(async context =>
            {
                error = await new Exception("Log!").LogAsync(context, "TestCategoy").ConfigureAwait(false);
                await context.Response.WriteAsync("Hey.").ConfigureAwait(false);
            }))
            {
                CurrentSettings.LogFilters.Cookie["CookieSecret"] = "***1";
                CurrentSettings.LogFilters.Header["HeaderSecret"] = "***2";
                CurrentSettings.LogFilters.Form["FormSecret"] = "***3";
                CurrentSettings.LogFilters.Header["HeaderSecret-CaseTest"] = "***4";
                CurrentSettings.LogFilters.QueryString["QuerySECRET"] = "***5"; // testing insensitivity too

                var request = server.CreateRequest("/?QueryNotSecret=QueryNotSecretValue&QuerySecret=dontlogme");
                request.AddHeader("Cookie", "CookieNotSecret=CookieNotSecretValue; CookieSecret=secretcookie!;");
                request.AddHeader("HeaderNotSecret", "HeaderNotSecretValue");
                request.AddHeader("HeaderSecret", "secret header!");
                request.AddHeader("HeaderSecret-CaseTest", "secret header!");
                request.And(rm => rm.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    ["FormNotSecret"] = "FormNotSecretValue",
                    ["FormSecret"] = "secrets!"
                }));

                using (var response = await request.PostAsync().ConfigureAwait(false))
                {
                    Assert.Equal("Hey.", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }

                Assert.Equal(2, error.Cookies.Count);
                Assert.Equal("CookieNotSecretValue", error.Cookies["CookieNotSecret"]);
                Assert.Equal("***1", error.Cookies["CookieSecret"]);

                Assert.Equal(5, error.RequestHeaders.Count); // Host and Content-Type
                Assert.Equal("HeaderNotSecretValue", error.RequestHeaders["HeaderNotSecret"]);
                Assert.Equal("***2", error.RequestHeaders["HeaderSecret"]);
                Assert.Equal("***4", error.RequestHeaders["headersecret-CASETest"]); // case insensitive

                Assert.Equal(2, error.Form.Count);
                Assert.Equal("FormNotSecretValue", error.Form["FormNotSecret"]);
                Assert.Equal("***3", error.Form["FormSecret"]);

                Assert.Equal(2, error.QueryString.Count);
                Assert.EndsWith("?QueryNotSecret=QueryNotSecretValue&QuerySecret=***5", error.FullUrl);
                Assert.Equal("QueryNotSecretValue", error.QueryString["QueryNotSecret"]);
                Assert.Equal("***5", error.QueryString["QuerySecret"]);
            }
        }
    }
}
