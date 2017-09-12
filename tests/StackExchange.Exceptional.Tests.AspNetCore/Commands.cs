using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.AspNetCore
{
    public class Commands : AspNetCoreTest
    {
        public Commands(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AddCommand()
        {
            var settings = new ExceptionalSettings();
            var ex = new Exception();
            var err = new Error(ex, settings);
            err.AddCommand(
                new Command("SQL", "Select * From MyTable")
                    .AddData("Server", "SQL01"));
            Assert.Single(err.Commands);
            Assert.Equal("SQL", err.Commands[0].Type);
            Assert.Equal("Select * From MyTable", err.Commands[0].CommandString);
            Assert.Single(err.Commands[0].Data);
            Assert.Equal("SQL01", err.Commands[0].Data["Server"]);
        }

        [Fact]
        public void Serialization()
        {
            var settings = new ExceptionalSettings();
            var ex = new Exception();
            var err = new Error(ex, settings);
            err.AddCommand(
                new Command("SQL", "Select * From MyTable")
                    .AddData("Server", "SQL01"));

            var json = err.ToJson();
            Assert.Contains("Select * From MyTable", json);

            var derr = Error.FromJson(json);
            Assert.Single(derr.Commands);
            Assert.Equal("SQL", derr.Commands[0].Type);
            Assert.Equal("Select * From MyTable", derr.Commands[0].CommandString);
            Assert.Single(derr.Commands[0].Data);
            Assert.Equal("SQL01", derr.Commands[0].Data["Server"]);
        }

        [Fact]
        public void SQLLegacyCompat()
        {
            const string json = "{\"SQL\": \"Select * From MyTable\"}";

            var err = Error.FromJson(json);
            Assert.Single(err.Commands);
            Assert.Equal("SQL Server Query", err.Commands[0].Type);
            Assert.Equal("Select * From MyTable", err.Commands[0].CommandString);
        }
    }
}
