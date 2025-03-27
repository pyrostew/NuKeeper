using System.Text;

using NuKeeper.Inspection.Report;

namespace NuKeeper.Inspection.Tests.Report
{
   public sealed class TestReportWriter : IReportWriter
   {
      private readonly StringBuilder _data = new();

      public void WriteLine(string value = "")
      {
         _ = _data.AppendLine(value);
      }

      public string Data()
      {
         return _data.ToString().TrimEnd();
      }

      public void Dispose()
      {
      }
   }
}
