using NuKeeper.Abstractions.CollaborationModels;

using NUnit.Framework;

namespace NuKeeper.Abstractions.Tests.CollaborationModels
{
    [TestFixture]
    public class PullRequestRequestTests
    {
        [Test]
        public void ReplacesRemotesWhenCreatingPullRequestRequestObject()
        {
            PullRequestRequest pr = new("head", "title", "origin/master", true, true);
            PullRequestRequest pr2 = new("head", "title", "master", true, true);

            Assert.That(pr.BaseRef, Is.EqualTo("master"));
            Assert.That(pr2.BaseRef, Is.EqualTo("master"));
        }

    }
}
