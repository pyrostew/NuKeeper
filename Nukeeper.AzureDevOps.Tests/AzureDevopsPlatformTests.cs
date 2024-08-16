using NSubstitute;

using NuKeeper.Abstractions.Configuration;
using NuKeeper.Abstractions.Logging;
using NuKeeper.AzureDevOps;

using NUnit.Framework;

using System;
using System.Net.Http;

namespace Nukeeper.AzureDevOps.Tests
{
    public class AzureDevOpsPlatformTests
    {
        [Test]
        public void Initialise()
        {
            IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
            _ = httpClientFactory.CreateClient().Returns(new HttpClient());

            AzureDevOpsPlatform platform = new(Substitute.For<INuKeeperLogger>(), httpClientFactory);
            platform.Initialise(new AuthSettings(new Uri("https://uri.com"), "token"));
        }
    }
}
