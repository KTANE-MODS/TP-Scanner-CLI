using System.Collections.Generic;
using System.IO;

namespace TP_Scanner
{

    internal class RepoEntry
    {
        public string Name { get; private set; }
        public string SteamID { get; private set; }
        public string Type { get; private set; }

        public enum TPStatus
        { 
            Unknown,
            False,
            True
        }
        public TPStatus HasTPSupport { get; set; }
        public TPStatus HasAutoSolver { get; set; }


        public static string ParentPath;
        public string DirectoryPath { get { return ParentPath + SteamID; } }

        private string[] dllPaths;

        public RepoEntry(Dictionary<string, object> Data)
        {
            Name = (string)Data["Name"];
            SteamID = (string)Data["SteamID"];
            Type = (string)Data["Type"];
        }

        public string[] GetDLLPaths()
        {
            if (dllPaths != null)
            {
                return dllPaths;
            }

            //Get all the dlls within the directory
            dllPaths = Directory.GetFiles(DirectoryPath, "*.dll", SearchOption.TopDirectoryOnly);
            return dllPaths;
        }

        public string ToString()
        {
            string newName = Name;
            if (Name.Contains(",") || Name.Contains("\n"))
            {
                newName = Name.Replace("\"", "\"\"");
                newName = $"\"{newName}\"";
            }

            return $"{newName},{HasTPSupport},{HasAutoSolver}";
        }
    }
}
