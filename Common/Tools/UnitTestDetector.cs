namespace Skyline.DataMiner.MediaOps.Plan.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class UnitTestDetector
    {
        static UnitTestDetector()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting") ||
                    assembly.FullName.StartsWith("Microsoft.TestPlatform") ||
                    assembly.FullName.StartsWith("NUnit.Framework"))
                {
                    IsInUnitTest = true;
                    return;
                }
            }

            IsInUnitTest = false;
        }

        public static bool IsInUnitTest { get; private set; }
    }
}
