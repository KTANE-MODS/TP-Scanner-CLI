using Newtonsoft.Json;
using System.Data;
using System.Reflection;

namespace TP_Scanner
{
    internal class Program
    {
        #region Configuration Variables
        static bool useLocalJSON = true; //If true, will use JSON from local path
        static string localJSONPath = "modules.json"; //The path of the JSON file
        static string jsonUrl = "https://ktane.timwi.de/json/raw"; //the url to download the json
        static string moduleInstalledPath = @"C:\Program Files (x86)\Steam\steamapps\workshop\content\341800\"; //Path where modules are installed
        static string unityDllPath = @"C:\Program Files\Unity\Hub\Editor\2017.4.22f1\Editor\Data\Managed\UnityEngine.dll"; //Path to KTANE UnityEngine.dll
        static bool warnLogs = false; //if warning logs should appears
        static bool moduleLogs = false; //if the list of moduels should be printed
        #endregion

        static List<RepoEntry> mods;
        static bool sucessJSON;

        static async Task Main(string[] args)
        {
            string jsonStr = "";

            //Load the JSON string
            if (useLocalJSON)
            {
                Console.WriteLine($"Loading JSON from local path \"{localJSONPath}\"...");
                try
                {
                    jsonStr = File.ReadAllText(localJSONPath);
                    sucessJSON = true;
                }
                catch (IOException e)
                {
                    sucessJSON = false;
                    Console.WriteLine($"Failed to load JSON: {e.Message}");
                }
            }

            else
            {
                jsonStr = await LoadJSON();
            }

            if (!sucessJSON)
            {
                Console.WriteLine("Failed to get JSON. Run the program to try again");
                Environment.Exit(0);
            }

            //parse the json into modules
            mods = ProcessJson(jsonStr).Where(x => x.SteamID != null && (x.Type == "Regular" || x.Type == "Needy")).OrderBy(m => m.Name).ToList();
            Console.WriteLine($"Loaded {mods.Count} modules from json");

            //check if player is missing any modules on their device
            RepoEntry.ParentPath = moduleInstalledPath;
            List<RepoEntry> installedModules = mods.Where(mod => Directory.Exists(mod.DirectoryPath)).ToList();
            List<RepoEntry> missingModules = mods.Except(installedModules).ToList();

            Console.WriteLine($"Found {installedModules.Count} installed modules");
            if (moduleLogs)
            {
                Console.WriteLine($"Installed modules: {GetRepoNameList(installedModules)}");
                Console.WriteLine($"Modules not installed from the repo: {GetRepoNameList(missingModules)}");
            }

            
            foreach (RepoEntry mod in missingModules)
            {
                mod.HasTPSupport = RepoEntry.TPStatus.Unknown;
                mod.HasAutoSolver = RepoEntry.TPStatus.Unknown;
            }

            // Load UnityEngine from KTANE path
            var unityAssembly = Assembly.LoadFrom(unityDllPath);
            var monoBehaviourType = unityAssembly.GetType("UnityEngine.MonoBehaviour");

            //Check with modules have TP Support
            for (int i = 0; i < installedModules.Count; i++)
            {
                UpdateProgress("Checking for TP support:", i + 1, installedModules.Count);
                CheckForSupport(installedModules[i], monoBehaviourType);
            }
            Console.WriteLine($"{installedModules.Count(m => m.HasTPSupport == RepoEntry.TPStatus.True)} modules have TP support");
            Console.WriteLine($"{installedModules.Count(m => m.HasAutoSolver == RepoEntry.TPStatus.True)} modules have auto solvers");
        }

        async static Task<string> LoadJSON()
        {
            Console.WriteLine($"Loading JSON from \"{jsonUrl}\"...");
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(jsonUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successsfully fetched JSON");
                sucessJSON = true;
                return await response.Content.ReadAsStringAsync();

            }
            else
            {
                Console.WriteLine($"Failed to fetch JSON: {response.StatusCode}");
                sucessJSON = false;
                return response.StatusCode.ToString();
            }
        }

        //Converts JsON into C# objects
        static List<RepoEntry> ProcessJson(string content)
        {
            KtaneData Deserialized = JsonConvert.DeserializeObject<KtaneData>(content);
            List<RepoEntry> RepoEntries = new List<RepoEntry>();
            foreach (var item in Deserialized.KtaneModules)
                RepoEntries.Add(new RepoEntry(item));
            return RepoEntries;
        }

        //Checks to see if a module have TP support and an autosolver at once to reduce load dlls
        static void CheckForSupport(RepoEntry mod, Type monoBehaviourType)
        {
            
            if (!File.Exists(unityDllPath))
            {
                WarnLog("UnityEngine.dll not found");
                return;
            }

            if (monoBehaviourType == null)
            {
                WarnLog("MonoBehaviour type not found");
                return;
            }

            foreach (string dll in mod.GetDLLPaths())
            {
                try
                {
                    var modAssembly = Assembly.LoadFrom(dll);
                    var types = modAssembly.GetTypes();

                    // Find all MonoBehaviour derived types
                    var validTypes = modAssembly.GetTypes()
                        .Where(t =>
                            t != null &&
                            monoBehaviourType.IsAssignableFrom(t) &&
                            !t.IsAbstract)
                        .ToList();

                    bool hasTPSupport = validTypes.Any(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                    .Any(m => m.Name == "ProcessTwitchCommand"));
                    bool hasAutoSolver = validTypes.Any(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                    .Any(m => m.Name == "TwitchHandleForcedSolve"));

                    mod.HasTPSupport = hasTPSupport ? RepoEntry.TPStatus.True : RepoEntry.TPStatus.False;
                    mod.HasAutoSolver = hasAutoSolver ? RepoEntry.TPStatus.True : RepoEntry.TPStatus.False;
                }
                catch
                {
                    WarnLog($"Failed to load: {dll}");
                }
            }
        }

        static string GetRepoNameList(List<RepoEntry> list)
        {
            return String.Join(", ", list.Select(m => m.Name));
        }

        static void UpdateProgress(string header, int current, int total)
        {
            string text = $"{header} {current}/{total}";
            Console.Write("\r" + text.PadRight(Console.WindowWidth - 1));
            if (current == total)
            {
                Console.WriteLine();
            }
        }

        static void WarnLog(string str)
        {
            if (warnLogs)
            { 
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(str);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }
    }
}
