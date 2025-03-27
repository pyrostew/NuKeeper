using System;
using System.Globalization;

using Newtonsoft.Json.Serialization;

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
