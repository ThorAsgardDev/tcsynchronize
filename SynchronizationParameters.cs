using System;
using System.Collections.Generic;
using System.Text;

namespace TCSynchronize
{
    class SynchronizationParameters
    {
        private string srcPath;
        private string destPath;
        private Filter filter;

        public SynchronizationParameters(string srcPath, string destPath, Filter filter)
        {
            this.srcPath = srcPath;
            this.destPath = destPath;
            this.filter = filter;
        }

        public string getSrcPath()
        {
            return srcPath;
        }

        public string getDestPath()
        {
            return destPath;
        }

        public Filter getFilter()
        {
            return filter;
        }
    }
}
