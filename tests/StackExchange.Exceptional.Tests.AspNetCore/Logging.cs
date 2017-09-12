using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Logging : AspNetCoreTest
    {
        public Logging(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Log()
        {
            Error error = null;
            using (var server = GetServer(context =>
            {
                error = new Exception("Log!").Log(context, "TestCategoy");
                return context.Response.WriteAsync("Hey.");
            }))
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    ["FormKey"] = "FormValue",
                });
                using (var response = await server.CreateClient().PostAsync("?QueryKey=QueryValue", content).ConfigureAwait(false))
                {
                    Assert.Equal("Hey.", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }

                Assert.Equal("Log!", error.Message);
                Assert.Equal("System.Exception", error.Type);
                Assert.Equal(Environment.MachineName, error.MachineName);
                Assert.Equal("TestCategoy", error.Category);
                Assert.Equal("localhost", error.Host);
                Assert.Equal("http://localhost/?QueryKey=QueryValue", error.FullUrl);
                Assert.Equal("/", error.UrlPath);

                Assert.NotEmpty(error.RequestHeaders);
                Assert.Equal("localhost", error.RequestHeaders["Host"]);
                Assert.Equal("application/x-www-form-urlencoded", error.RequestHeaders["Content-Type"]);

                Assert.Single(error.Form);
                Assert.Equal("FormValue", error.Form["FormKey"]);

                Assert.Single(error.QueryString);
                Assert.Equal("QueryValue", error.QueryString["QueryKey"]);

                Assert.NotEmpty(error.ServerVariables);
                Assert.Equal("localhost", error.ServerVariables["Host"]);
                Assert.Equal("/", error.ServerVariables["Path"]);
                Assert.Equal("POST", error.ServerVariables["Request Method"]);
                Assert.Equal("http", error.ServerVariables["Scheme"]);
                Assert.Equal("http://localhost/?QueryKey=QueryValue", error.ServerVariables["Url"]);
            }
        }

        [Fact]
        public async Task LogAsync()
        {
            Error error = null;
            using (var server = GetServer(async context =>
            {
                error = await new Exception("Log!").LogAsync(context, "TestCategoy").ConfigureAwait(false);
                await context.Response.WriteAsync("Hey.").ConfigureAwait(false);
            }))
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    ["FormKey"] = "FormValue",
                });
                using (var response = await server.CreateClient().PostAsync("?QueryKey=QueryValue", content).ConfigureAwait(false))
                {
                    Assert.Equal("Hey.", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }

                Assert.Equal("Log!", error.Message);
                Assert.Equal("System.Exception", error.Type);
                Assert.Equal(Environment.MachineName, error.MachineName);
                Assert.Equal("TestCategoy", error.Category);
                Assert.Equal("localhost", error.Host);
                Assert.Equal("http://localhost/?QueryKey=QueryValue", error.FullUrl);
                Assert.Equal("/", error.UrlPath);

                Assert.NotEmpty(error.RequestHeaders);
                Assert.Equal("localhost", error.RequestHeaders["Host"]);
                Assert.Equal("application/x-www-form-urlencoded", error.RequestHeaders["Content-Type"]);

                Assert.Single(error.Form);
                Assert.Equal("FormValue", error.Form["FormKey"]);

                Assert.Single(error.QueryString);
                Assert.Equal("QueryValue", error.QueryString["QueryKey"]);

                Assert.NotEmpty(error.ServerVariables);
                Assert.Equal("localhost", error.ServerVariables["Host"]);
                Assert.Equal("/", error.ServerVariables["Path"]);
                Assert.Equal("POST", error.ServerVariables["Request Method"]);
                Assert.Equal("http", error.ServerVariables["Scheme"]);
                Assert.Equal("http://localhost/?QueryKey=QueryValue", error.ServerVariables["Url"]);
            }
        }

        [Fact]
        public async Task LogNoContext()
        {
            Error error = null;
            using (var server = GetServer(context =>
            {
                error = new Exception("Log!").LogNoContext("TestCategoy");
                return context.Response.WriteAsync("Hey.");
            }))
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    ["FormKey"] = "FormValue",
                });
                using (var response = await server.CreateClient().PostAsync("?QueryKey=QueryValue", content).ConfigureAwait(false))
                {
                    Assert.Equal("Hey.", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }

                Assert.Equal("Log!", error.Message);
                Assert.Equal("System.Exception", error.Type);
                Assert.Equal(Environment.MachineName, error.MachineName);
                Assert.Equal("TestCategoy", error.Category);
                Assert.Null(error.Host);
                Assert.Null(error.FullUrl);
                Assert.Null(error.UrlPath);
                Assert.Null(error.IPAddress);

                Assert.Null(error.RequestHeaders);
                Assert.Null(error.Form);
                Assert.Null(error.QueryString);
                Assert.Null(error.ServerVariables);
            }
        }
    }
}
