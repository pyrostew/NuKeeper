using NuKeeper.Abstractions.CollaborationModels;

using System;

namespace NuKeeper.GitHub.Tests
{
    public static class RepositoryBuilder
    {
        public const string ParentCloneUrl = "http://repos.com/org/parent.git";
        public const string ParentCloneBareUrl = "http://repos.com/org/parent/";

        public const string ForkCloneUrl = "http://repos.com/org/repo.git";
        public const string ForkCloneBareUrl = "http://repos.com/org/repo/";
        public const string NoMatchUrl = "http://repos.com/org/nomatch";

        public static Repository MakeRepository(bool canPull, bool canPush)
        {
            return MakeRepository(ForkCloneUrl, canPull, canPush);
        }

        public static Repository MakeRepository(
            string forkCloneUrl = ForkCloneUrl,
            bool canPull = true,
            bool canPush = true,
            string name = "repoName",
            Repository parent = null)
        {
            return new Repository(
                name,
                false,
                new UserPermissions(false, canPush, canPull),
                new Uri(forkCloneUrl),
                MakeUser(),
                true,
                parent ?? MakeParentRepo());
        }

        public static Repository MakeParentRepo(
            string cloneUrl = ParentCloneUrl)
        {
            return new Repository(
                "repoName",
                false,
                new UserPermissions(false, true, true),
                new Uri(cloneUrl),
                MakeUser(),
                true,
                null
            );
        }

        public static User MakeUser()
        {
            return new User("testUser", "Testy", "testuser@test.com");
        }
    }
}
