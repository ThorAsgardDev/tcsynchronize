using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TCSynchronize
{
    class Filter
    {
        private string rootPath;
        private List<Regex> regexs;

        public Filter(string rootPath, List<string> patterns)
        {
            this.rootPath = rootPath;

            regexs = new List<Regex>();
            if (patterns != null)
            {
                foreach (string pattern in patterns)
                {
                    string regexPattern = "^" + pattern.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".") + "$";
                    regexs.Add(new Regex(regexPattern));
                }
            }
        }

        private bool isNameFiltered(string value)
        {
            foreach (Regex regex in regexs)
            {
                if (regex.IsMatch(value))
                {
                    return true;
                }
            }
            return false;
        }

        public bool isPathFiltered(string fullPath)
        {
            string path = fullPath.Replace(rootPath + Path.DirectorySeparatorChar, "");
            string[] names = path.Split(Path.DirectorySeparatorChar);
            foreach (string name in names)
            {
                if (isNameFiltered(name))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
