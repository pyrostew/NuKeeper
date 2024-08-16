using NuKeeper.Abstractions.Git;

using System;
using System.IO;
using System.Threading.Tasks;

namespace NuKeeper.Abstractions.Formats
{
    public static class UriFormats
    {
        public static Uri EnsureTrailingSlash(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            string path = uri.ToString();

            return path.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? uri : new Uri(path + "/");
        }

        public static async Task<Uri> GetRemoteUriFromLocalRepo(this Uri repositoryUri, IGitDiscoveryDriver discoveryDriver, string shouldMatchTo)
        {
            if (discoveryDriver == null)
            {
                throw new ArgumentNullException(nameof(discoveryDriver));
            }

            if (await discoveryDriver.IsGitRepo(repositoryUri))
            {
                // Check the origin remotes
                GitRemote origin = await discoveryDriver.GetRemoteForPlatform(repositoryUri, shouldMatchTo);

                if (origin != null)
                {
                    return origin.Url;
                }
            }

            return repositoryUri;
        }

        public static Uri ToUri(this string repositoryString)
        {
            Uri repositoryUri = new(repositoryString, UriKind.RelativeOrAbsolute);
            if (repositoryUri.IsAbsoluteUri)
            {
                return repositoryUri;
            }

            string absoluteUri = Path.Combine(Environment.CurrentDirectory, repositoryUri.OriginalString);
            return !Directory.Exists(absoluteUri)
                ? throw new NuKeeperException($"Local uri doesn't exist: {absoluteUri}")
                : new Uri(absoluteUri);
        }
    }
}
