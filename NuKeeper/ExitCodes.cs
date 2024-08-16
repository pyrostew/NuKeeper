using System;

namespace NuKeeper
{
    [Flags]
    internal enum ExitCodes
    {
        Success = 0,
        UnknownError = 1 << 0,
        InvalidArguments = 1 << 1,
    }
}
