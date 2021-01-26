
namespace TCSynchronize
{
    class Synchronizer
    {
        private FileTools fileTools;

        public Synchronizer()
        {
            fileTools = new FileTools();
        }

        public void synchronize(string srcPath, string destPath, Filter filter, bool logsOuput)
        {
            if (logsOuput)
            {
                Logger.log(Logger.Level.Info, $"Synchronizing {srcPath} to {destPath}...");
            }
            
            fileTools.copyDirectory(srcPath, destPath, filter, logsOuput);
        }
    }
}
