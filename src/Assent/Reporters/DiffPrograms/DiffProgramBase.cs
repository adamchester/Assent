using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Assent.Reporters.DiffPrograms
{
    public abstract class DiffProgramBase : IDiffProgram
    {
        public IReadOnlyList<string> SearchPaths { get; }

        protected DiffProgramBase(IReadOnlyList<string> searchPaths)
        {
            SearchPaths = searchPaths;
        }

        protected virtual string CreateProcessStartArgs(
            string receivedFile, string approvedFile)
        {
            return $"\"{receivedFile}\" \"{approvedFile}\"";
        }
        
        public virtual bool Launch(string receivedFile, string approvedFile)
        {
            var path = SearchPaths.FirstOrDefault(File.Exists);
            if (path == null)
                return false;

            var process = Process.Start(new ProcessStartInfo(
                path, CreateProcessStartArgs(receivedFile, approvedFile)));
            process.WaitForExit();
            return true;
        }
    }
}