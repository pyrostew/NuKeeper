using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

using System.Collections.Concurrent;

namespace NuKeeper.Inspection.NuGetApi
{
    public class ConcurrentSourceRepositoryCache
    {
        private readonly ConcurrentDictionary<PackageSource, SourceRepository> _packageSources
            = new();

        public SourceRepository Get(PackageSource source)
        {
            return _packageSources.GetOrAdd(source, CreateSourceRepository);
        }

        private static SourceRepository CreateSourceRepository(PackageSource packageSource)
        {
            return new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        }
    }
}
