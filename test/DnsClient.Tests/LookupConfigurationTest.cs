﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupConfigurationTest
    {
        [Fact]
        public void LookupClientOptions_Defaults()
        {
            var options = new LookupClientOptions();

            Assert.True(options.NameServers.Count > 0);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
        }

        [Fact]
        public void LookupClientOptions_DefaultsNoResolve()
        {
            var options = new LookupClientOptions(resolveNameServers: false);

            Assert.Equal(0, options.NameServers.Count);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
        }

        [Fact]
        public void LookupClient_SettingsValid()
        {
            var defaultOptions = new LookupClientOptions(resolveNameServers: true);

            var options = new LookupClientOptions(resolveNameServers: true)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                MinimumCacheTimeout = TimeSpan.FromMinutes(1),
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var client = new LookupClient(options);

            Assert.Equal(defaultOptions.NameServers, client.NameServers);
            Assert.Equal(!defaultOptions.ContinueOnDnsError, client.Settings.ContinueOnDnsError);
            Assert.Equal(!defaultOptions.EnableAuditTrail, client.Settings.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Settings.MinimumCacheTimeout);
            Assert.Equal(!defaultOptions.Recursion, client.Settings.Recursion);
            Assert.Equal(defaultOptions.Retries * 2, client.Settings.Retries);
            Assert.Equal(!defaultOptions.ThrowDnsErrors, client.Settings.ThrowDnsErrors);
            Assert.Equal(TimeSpan.FromMinutes(10), client.Settings.Timeout);
            Assert.Equal(!defaultOptions.UseCache, client.Settings.UseCache);
            Assert.Equal(!defaultOptions.UseRandomNameServer, client.Settings.UseRandomNameServer);
            Assert.Equal(!defaultOptions.UseTcpFallback, client.Settings.UseTcpFallback);
            Assert.Equal(!defaultOptions.UseTcpOnly, client.Settings.UseTcpOnly);
        }

        [Fact]
        public void Lookup_Query_InvalidTimeout()
        {
            var options = new LookupClientOptions();

            Action act = () => options.Timeout = TimeSpan.FromMilliseconds(0);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        public static IEnumerable<object[]> All
        {
            get
            {
                // question doesn't matter
                var question = new DnsQuestion("something.com", QueryType.A);

                // standard
                yield return new object[] { new TestMatrixItem("Query(q)", (client) => client.Query(question)) };
                yield return new object[] { new TestMatrixItem("Query(n,t,c)", (client) => client.Query(question.QueryName, question.QuestionType, question.QuestionClass)) };
                yield return new object[] { new TestMatrixItem("QueryAsync(q)", (client) => client.QueryAsync(question).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryAsync(n,t,c)", (client) => client.QueryAsync(question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryReverse(ip)", (client) => client.QueryReverse(IPAddress.Any)) };
                yield return new object[] { new TestMatrixItem("QueryReverseAsync(ip)", (client) => client.QueryReverseAsync(IPAddress.Any).GetAwaiter().GetResult()) };

                // by server
                yield return new object[] { new TestMatrixItem("QueryServer(s,n,t,c)", (client, servers) => client.QueryServer(servers, question.QueryName, question.QuestionType, question.QuestionClass)) };
                yield return new object[] { new TestMatrixItem("QueryServerAsync(s,n,t,c)", (client, servers) => client.QueryServerAsync(servers, question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryServerReverse(s,ip)", (client, servers) => client.QueryServerReverse(servers, IPAddress.Any)) };
                yield return new object[] { new TestMatrixItem("QueryServerReverseAsync(s,ip)", (client, servers) => client.QueryServerReverseAsync(servers, IPAddress.Any).GetAwaiter().GetResult()) };

                // with query options
                yield return new object[] { new TestMatrixItem("Query(q,o)", (client, options) => client.Query(question, options)) };
                yield return new object[] { new TestMatrixItem("Query(n,t,c,o)", (client, options) => client.Query(question.QueryName, question.QuestionType, question.QuestionClass, options)) };
                yield return new object[] { new TestMatrixItem("QueryAsync(q,o)", (client, options) => client.QueryAsync(question, options).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryAsync(n,t,c,o)", (client, options) => client.QueryAsync(question.QueryName, question.QuestionType, question.QuestionClass, options).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryReverse(ip,o)", (client, options) => client.QueryReverse(IPAddress.Any, options)) };
                yield return new object[] { new TestMatrixItem("QueryReverseAsync(ip,o)", (client, options) => client.QueryReverseAsync(IPAddress.Any, options).GetAwaiter().GetResult()) };
            }
        }

        public static IEnumerable<object[]> AllWithoutServerQueries
            => All.Where(p => !p.Any(a => a is TestMatrixItem m && m.UsesServers));

        public static IEnumerable<object[]> AllWithServers
            => All.Where(p => p.Any(a => a is TestMatrixItem m && m.UsesServers));

        public static IEnumerable<object[]> AllWithQueryOptions
            => All.Where(p => p.Any(a => a is TestMatrixItem m && m.UsesQueryOptions));

        public static IEnumerable<object[]> AllWithoutQueryOptionsOrServerQueries
            => All.Where(p => !p.Any(a => a is TestMatrixItem m && (m.UsesQueryOptions || m.UsesServers)));

        [Theory]
        [MemberData(nameof(AllWithoutServerQueries))]
        public void ConfigMatrix_NoServersConfiguredThrows(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: false);
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersQueriesExpectServers(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: false);
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions, useServers: new NameServer[0]));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersCannotBeNull(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: true);
            Assert.Throws<ArgumentNullException>("servers", () => test.InvokeNoDefaults(lookupClientOptions: unresolvedOptions, useOptions: null, useServers: null));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_ValidSettingsResponse(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(NameServer.GooglePublicDns);
            var queryOptions = new DnsQueryOptions(NameServer.GooglePublicDns2);
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };

            var result = test.Invoke(lookupClientOptions: unresolvedOptions, useOptions: queryOptions, useServers: servers);

            Assert.Null(result.TestClient.TcpHandler.LastRequest);
            Assert.NotNull(result.TestClient.UdpHandler.LastRequest);
            Assert.Equal(new LookupClientSettings(unresolvedOptions), result.TestClient.Client.Settings);

            if (test.UsesQueryOptions)
            {
                Assert.Equal(NameServer.GooglePublicDns2, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2, result.Response.NameServer);
                Assert.Equal(new[] { NameServer.GooglePublicDns2 }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(queryOptions), result.Response.Settings);
            }
            else if (test.UsesServers)
            {
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
                // by server overrules settings, but doesn't change the servers collection in settings..
                Assert.Equal(new[] { NameServer.GooglePublicDns }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(unresolvedOptions), result.Response.Settings);
            }
            else
            {
                Assert.Equal(NameServer.GooglePublicDns, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns, result.Response.NameServer);
                Assert.Equal(new[] { NameServer.GooglePublicDns }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(unresolvedOptions), result.Response.Settings);
            }
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverride(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(resolveNameServers: false);
            var queryOptions = new DnsQueryOptions(NameServer.GooglePublicDns2IPv6)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            Assert.Null(result.TestClient.UdpHandler.LastRequest);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            Assert.Equal(new LookupClientSettings(defaultOptions), result.TestClient.Client.Settings);
            Assert.Equal(new DnsQuerySettings(queryOptions), result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverrideWithServerFallback(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6);
            var queryOptions = new DnsQueryOptions(resolveNameServers: false)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            //var cacheKey = ResponseCache.GetCacheKey(result.TestClient.TcpHandler.LastRequest.Question, NameServer.GooglePublicDns2IPv6);
            //Assert.Null(result.TestClient.Client.ResponseCache.Get(cacheKey));

            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            // make sure we don't alter the original object
            Assert.Empty(queryOptions.NameServers);
            Assert.Equal(new[] { NameServer.GooglePublicDns2IPv6 }, result.Response.Settings.NameServers);
            Assert.Equal(new DnsQuerySettings(queryOptions, defaultOptions.NameServers.ToArray()), result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyCacheUsed(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false
            };
            var queryOptions = new DnsQueryOptions(resolveNameServers: false)
            {
                UseCache = true
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            //var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question, NameServer.GooglePublicDns2IPv6);
            //Assert.NotNull(result.TestClient.Client.ResponseCache.Get(cacheKey));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_VerifyCacheNotUsed(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false,
                MinimumCacheTimeout = TimeSpan.FromMilliseconds(1000)
            };
            var queryOptions = new DnsQueryOptions(resolveNameServers: false)
            {
                UseCache = false
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            //var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question, NameServer.GooglePublicDns2IPv6);
            //Assert.Null(result.TestClient.Client.ResponseCache.Get(cacheKey));
        }

        public class TestMatrixItem
        {
            public bool UsesServers { get; }

            public Func<ILookupClient, IDnsQueryResponse> ResolverSimple { get; }

            public Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> ResolverServers { get; }

            public string Name { get; }

            public Func<ILookupClient, DnsQueryOptions, IDnsQueryResponse> ResolverQueryOptions { get; }

            public bool UsesQueryOptions { get; }

            private TestMatrixItem(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public TestMatrixItem(string name, Func<ILookupClient, DnsQueryOptions, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverQueryOptions = resolver;
                UsesQueryOptions = true;
                UsesServers = false;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverServers = resolver;
                UsesQueryOptions = false;
                UsesServers = true;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverSimple = resolver;
                UsesQueryOptions = false;
                UsesServers = false;
            }

            public TestResponse Invoke(LookupClientOptions lookupClientOptions = null, DnsQueryOptions useOptions = null, IReadOnlyCollection<NameServer> useServers = null)
            {
                var testClient = new TestClient(lookupClientOptions);
                var servers = useServers ?? new NameServer[] { IPAddress.Loopback };
                var queryOptions = useOptions ?? new DnsQueryOptions();

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, servers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, queryOptions);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public TestResponse InvokeNoDefaults(LookupClientOptions lookupClientOptions, DnsQueryOptions useOptions, IReadOnlyCollection<NameServer> useServers)
            {
                var testClient = new TestClient(lookupClientOptions);

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, useServers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, useOptions);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public override string ToString()
            {
                return $"{Name} => s:{UsesServers} q:{UsesQueryOptions}";
            }
        }

        public class TestResponse
        {
            public TestResponse(TestClient testClient, IDnsQueryResponse response)
            {
                TestClient = testClient;
                Response = response;
            }

            public TestClient TestClient { get; }

            public IDnsQueryResponse Response { get; }
        }

        public class TestClient
        {
            public TestClient(LookupClientOptions options)
            {
                UdpHandler = new ConfigurationTrackingMessageHandler(false);
                TcpHandler = new ConfigurationTrackingMessageHandler(true);
                Client = new LookupClient(options, UdpHandler, TcpHandler);
            }

            internal ConfigurationTrackingMessageHandler UdpHandler { get; }

            internal ConfigurationTrackingMessageHandler TcpHandler { get; }

            public LookupClient Client { get; }
        }

        internal class ConfigurationTrackingMessageHandler : DnsMessageHandler
        {
            // raw bytes from mcnet.com
            private static readonly byte[] ZoneData = new byte[]
            {
               95, 207, 129, 128, 0, 1, 0, 11, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 192, 12, 0, 1, 0, 1, 0, 0, 1, 8, 0, 4, 172, 217, 17, 238, 192, 12, 0, 28, 0, 1, 0, 0, 0, 71, 0, 16, 42, 0, 20, 80, 64, 22, 8, 13, 0, 0, 0, 0, 0, 0, 32, 14, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 17, 0, 50, 4, 97, 108, 116, 52, 5, 97, 115, 112, 109, 120, 1, 108, 192, 12, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 4, 0, 10, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 30, 4, 97, 108, 116, 50, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 20, 4, 97, 108, 116, 49, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 40, 4, 97, 108, 116, 51, 192, 91, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 51, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 50, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 52, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 49, 192, 12, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0
            };

            public IReadOnlyList<DnsQuerySettings> UsedSettings { get; }

            public bool IsTcp { get; }

            public IPEndPoint LastServer { get; private set; }

            public DnsRequestMessage LastRequest { get; private set; }

            public ConfigurationTrackingMessageHandler(bool isTcp)
            {
                IsTcp = isTcp;
            }

            public override bool IsTransientException<T>(T exception)
            {
                return false;
            }

            public override DnsResponseMessage Query(
                IPEndPoint server,
                DnsRequestMessage request,
                TimeSpan timeout)
            {
                LastServer = server;
                LastRequest = request;
                var response = GetResponseMessage(new ArraySegment<byte>(ZoneData, 0, ZoneData.Length));

                return response;
            }

            public override Task<DnsResponseMessage> QueryAsync(
                IPEndPoint server,
                DnsRequestMessage request,
                CancellationToken cancellationToken,
                Action<Action> cancelationCallback)
            {
                LastServer = server;
                LastRequest = request;
                // no need to run async here as we don't do any IO
                return Task.FromResult(Query(server, request, Timeout.InfiniteTimeSpan));
            }
        }
    }
}