using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StackExchange.Exceptional.Internal;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Exceptional.Tests.Storage
{
    public abstract class StoreBaseTest : BaseTest
    {
        protected virtual bool StoreHardDeletes => false;

        protected StoreBaseTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task DeleteErrorAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);
            var error2 = GetBasicError("Test Error2", store);
            store.Log(error);
            store.Log(error2);

            Assert.True(await store.DeleteAsync(error.GUID).ConfigureAwait(false));

            if (StoreHardDeletes)
            {
                Assert.Null(await store.GetAsync(error.GUID).ConfigureAwait(false));
            }
            else
            {
                Assert.NotNull((await store.GetAsync(error.GUID).ConfigureAwait(false))?.DeletionDate);
            }
            Assert.Null((await store.GetAsync(error2.GUID).ConfigureAwait(false)).DeletionDate);
        }

        [Fact]
        public async Task DeleteErrorsAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);
            var error2 = GetBasicError("Test Error2", store);
            var error3 = GetBasicError("Test Error3", store);
            Assert.True(store.Log(error));
            Assert.True(store.Log(error2));
            Assert.True(store.Log(error3));

            Assert.True(await store.DeleteAsync(new[] { error.GUID, error2.GUID }).ConfigureAwait(false));

            if (StoreHardDeletes)
            {
                Assert.Null(await store.GetAsync(error.GUID).ConfigureAwait(false));
                Assert.Null(await store.GetAsync(error2.GUID).ConfigureAwait(false));
            }
            else
            {
                Assert.NotNull((await store.GetAsync(error.GUID).ConfigureAwait(false)).DeletionDate);
                Assert.NotNull((await store.GetAsync(error2.GUID).ConfigureAwait(false)).DeletionDate);
            }
            Assert.Null((await store.GetAsync(error3.GUID).ConfigureAwait(false)).DeletionDate);
        }

        [Fact]
        public async Task DeleteAllErrorsAsync()
        {
            var store = GetStore();
            Assert.True(store.Log(GetBasicError("Test Error", store)));
            Assert.True(store.Log(GetBasicError("Test Error2", store)));
            Assert.True(store.Log(GetBasicError("Test Error3", store)));

            Assert.Equal(3, await store.GetCountAsync().ConfigureAwait(false));
            Assert.True(await store.DeleteAllAsync().ConfigureAwait(false));
            Assert.Equal(0, await store.GetCountAsync().ConfigureAwait(false));
        }

        [Fact]
        public async Task DuplicateCount()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);

            Assert.True(store.Log(error));
            Assert.True(store.Log(GetBasicError("Test Error", store)));
            Assert.True(store.Log(GetBasicError("Test Error", store)));

            var storedError = await store.GetAsync(error.GUID).ConfigureAwait(false);

            Assert.NotNull(storedError);
            Assert.Equal(3, storedError.DuplicateCount);
        }

        [Fact]
        public async Task GetAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);

            Assert.True(await store.LogAsync(error).ConfigureAwait(false));
            var storedError = await store.GetAsync(error.GUID).ConfigureAwait(false);
            Assert.NotNull(storedError);
            Assert.Equal(error.GetHash(true), storedError.GetHash(true));
        }

        [Fact]
        public async Task GetCountAsync()
        {
            var store = GetStore();

            Assert.True(await store.LogAsync(GetBasicError("Test Error", store)).ConfigureAwait(false));
            Assert.True(await store.LogAsync(GetBasicError("Test Error2", store)).ConfigureAwait(false));
            Assert.Equal(2, await store.GetCountAsync().ConfigureAwait(false));
            Assert.Equal(2, await store.GetCountAsync(DateTime.UtcNow.AddMinutes(-10)).ConfigureAwait(false));
            Assert.Equal(0, await store.GetCountAsync(DateTime.UtcNow.AddMinutes(10)).ConfigureAwait(false));
        }

        [Fact]
        public async Task Log()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);

            Assert.True(store.Log(error));
            var storedError = await store.GetAsync(error.GUID).ConfigureAwait(false);

            Assert.NotNull(storedError);
            Assert.Equal(error.GUID, storedError.GUID);
        }

        [Fact]
        public async Task LogAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);

            Assert.True(await store.LogAsync(error).ConfigureAwait(false));
            var storedError = await store.GetAsync(error.GUID).ConfigureAwait(false);

            Assert.NotNull(storedError);
            Assert.Equal(error.GUID, storedError.GUID);
        }

        [Fact]
        public async Task ProtectErrorAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);
            var error2 = GetBasicError("Test Error2", store);
            store.Log(error);
            store.Log(error2);

            Assert.True(await store.ProtectAsync(error.GUID).ConfigureAwait(false));

            Assert.True((await store.GetAsync(error.GUID).ConfigureAwait(false)).IsProtected);
            Assert.False((await store.GetAsync(error2.GUID).ConfigureAwait(false)).IsProtected);
        }

        [Fact]
        public async Task ProtectErrorsAsync()
        {
            var store = GetStore();
            var error = GetBasicError("Test Error", store);
            var error2 = GetBasicError("Test Error2", store);
            var error3 = GetBasicError("Test Error3", store);
            Assert.True(store.Log(error));
            Assert.True(store.Log(error2));
            Assert.True(store.Log(error3));

            Assert.True(await store.ProtectAsync(new[] { error.GUID, error2.GUID }).ConfigureAwait(false));

            Assert.True((await store.GetAsync(error.GUID).ConfigureAwait(false)).IsProtected);
            Assert.True((await store.GetAsync(error2.GUID).ConfigureAwait(false)).IsProtected);
            Assert.False((await store.GetAsync(error3.GUID).ConfigureAwait(false)).IsProtected);
        }

        [Fact]
        public async Task TestAsync()
        {
            var store = GetStore();
            Statics.Settings = new TestSettings(store);
            Assert.True(await store.TestAsync().ConfigureAwait(false));
        }

        protected Error GetBasicError(string message, ErrorStore store) =>
            new Error(new Exception(message), GetSettings(store));

        protected TestSettings GetSettings(ErrorStore store)
        {
            var settings = new TestSettings(store);
            settings.Store.ApplicationName = store.Settings.ApplicationName;
            return settings;
        }

        protected abstract ErrorStore GetStore([CallerMemberName]string appName = null);
    }
}
