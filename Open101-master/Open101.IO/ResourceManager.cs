using System.Collections.Generic;
using System.IO;
using DragonLib.IO;

namespace Open101.IO
{
    public static class ResourceManager
    {
        public const string ROOT_WAD = "Root";
        private const string DATA_DIR = @"Data\GameData";
        
        public static string s_gameDir = @"C:\ProgramData\KingsIsle Entertainment\Wizard101";
        public static string s_gameDataDir = $@"{s_gameDir}\{DATA_DIR}";
        
        private static readonly Dictionary<string, Wad> s_wads = new Dictionary<string, Wad>();
        
        public static void SetGameDir(string dir)
        {
            if (dir == s_gameDir) return;
            
            s_gameDir = dir;
            s_gameDataDir = $@"{s_gameDir}\{DATA_DIR}";
            
            lock (s_wads)
            {
                s_wads.Clear();
            }
        }
        
        public static void SetGameDataDir(string dir)
        {
            // using this method will init data without a real game directory
            
            s_gameDir = null;
            if (dir == s_gameDataDir) return;
            s_gameDataDir = dir;

            lock (s_wads)
            {
                s_wads.Clear();
            }
        }

        public static byte[] GetFileBytes(string file)
        {
            using var stream = OpenFile(file);
            if (stream == null) return null;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public static bool DumpFile(string name)
        {
            var bytes = GetFileBytes(name);
            if (bytes == null) return false;
            
            string wadName = ROOT_WAD;
            var file = ParseFileName(name, ref wadName);
            file = Path.GetFileName(file);
            
            File.WriteAllBytes(file, bytes);

            return true;
        }

        private static string ParseFileName(string file, ref string wadName)
        {
            if (file.StartsWith('|'))
            {
                int lastIndexOfPipe = file.LastIndexOf('|');
                var wadRaw = file.Substring(1, lastIndexOfPipe - 1);
                wadName = wadRaw.Replace('|', '-');

                file = file.Substring(lastIndexOfPipe+1);
            }
            file = file.Replace('\\', '/');
            return file;
        }
        
        public static Stream OpenFile(string file)
        {
            string wadName = ROOT_WAD;
            file = ParseFileName(file, ref wadName);
            var wad = GetWad(wadName);
            if (wad == null)
            {
                Logger.Error("ResourceManager", $"Missing wad {wadName}");
                return null;
            }
            return wad.OpenFile(file);
        }

        public static Wad GetWad(string name)
        {
            lock (s_wads)
            {
                if (s_wads.TryGetValue(name, out var wad)) return wad;
                var newWadPath = Path.Combine(s_gameDataDir, name + ".wad");
                if (!File.Exists(newWadPath)) return null;
                var newWad = new Wad(newWadPath);
                s_wads[name] = newWad;
                return newWad;
            }
        }

        public static Wad AddCustomWad(string fileName)
        {
            lock (s_wads)
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var wad = new Wad(fileName);
                s_wads[name] = wad;
                return wad;
            }
        }
    }
}