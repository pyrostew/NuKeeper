using Newtonsoft.Json.Serialization;

using System;
using System.Globalization;

namespace NuKeeper
{
    public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName == null
                ? throw new ArgumentNullException(nameof(propertyName))
                : propertyName.ToLower(CultureInfo.InvariantCulture);
        }
    }
}
