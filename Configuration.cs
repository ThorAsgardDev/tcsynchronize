using System.Collections.Generic;

namespace TCSynchronize
{
    class Configuration
    {
        public class Synchronization
        {
            public string srcPath { get; set; }
            public string destPath { get; set; }
            public List<string> filterPatterns { get; set; }
        }
        public string logLevel { get; set; }
        public List<Synchronization> synchronizations { get; set; }
        public List<string> globalFilterPatterns { get; set; }
    }
}
