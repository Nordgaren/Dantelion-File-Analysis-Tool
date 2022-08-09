using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace Elden_Ring_File_Analysis_Tool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowUsage();
                return;
            }

            string gamePath = Path.GetDirectoryName(args[0]);

            if (!File.Exists(args[0]) || gamePath == null)
            {
                ShowUsage();
                return;
            }

            if (!File.Exists("oo2core_6_win64.dll"))
                File.Copy($"{gamePath}/oo2core_6_win64.dll", $"{Environment.CurrentDirectory}/oo2core_6_win64.dll");

            switch (args[1])
            {
                case "-p":
                    if (args.Length < 3) { ShowUsage(); return; }
                    PathMatch(gamePath, args[2]);
                    break;
                case "-efl":
                    EntryFileListSearch(gamePath);
                    break;
                case "-m":
                    if (args.Length < 3) { ShowUsage(); return; }
                    FileMagicSearch(gamePath, args[2], false);
                    break;
                case "-mb":
                    if (args.Length < 3) { ShowUsage(); return; }
                    FileMagicSearch(gamePath, args[2], true);
                    break;
                case "-b":
                    if (args.Length < 3) { ShowUsage(); return; }
                    BNDSearch(gamePath, args[2], false);
                    break;
                case "-be":
                    if (args.Length < 3) { ShowUsage(); return; }
                    BNDSearch(gamePath, args[2], true);
                    break;
                default:
                    ShowUsage();
                    break;
            }

            Console.WriteLine("Operation complete.");
            Console.ReadLine();
        }


        private static void ShowUsage()
        {
            Console.WriteLine("Arg1: eldenring.exe path. \n" +
                "Arg2: switch:\n" +
                    "\t-p: Path match: provide a path as Arg3 to be hashed and matched against files in `Game\\_unknown`\n" +
                    "\t-efl: Hash blast the `Game\\map\\entryfilelist`\n" +
                    "\t-m: Search File Magic: compare magic of every file with Arg3 in `Game\\`\n" +
                    "\t    use -mb to search inside BNDs, as well\n" +
                    "\t-b: Search BND Files: compare file name of every file in a BND with Arg3 in `Game\\`.\n" +
                    "\t    -be to search file names without an extension\n" +
                "Arg3: Arguments for the specified switch");
        }

        private static void PathMatch(string gamePath, string path)
        {

            Dictionary<ulong, string> unknowns = Util.GetUnkDictionary(gamePath, "*");
            if (unknowns.Count <= 0)
                throw new Exception($@"No files found in {gamePath}\_unknown");

            ulong hash = Util.ComputeHash(path, true);
            if (unknowns.ContainsKey(hash))
                Console.WriteLine($"Matched: {hash} -> {unknowns[hash]}");
            else
                Console.WriteLine($"No match: {path} {hash}");
        }

        private static void EntryFileListSearch(string gamePath)
        {
            Dictionary<ulong, string> entryfilelists = Util.GetUnkDictionary(gamePath, "*.entryfilelist");

            if (entryfilelists.Count <= 0)
                throw new Exception($@"No '.entryfilelist' files found in {gamePath}\_unknown");

            string[] folders = Directory.GetDirectories($@"{gamePath}\map\entryfilelist");

            List<string> list = new();
            Console.WriteLine($@"Searching .entryfilelist files in {gamePath}\_unknown matching {entryfilelists.Count} files");
            for (int i = 0; i < folders.Length; i++)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(new string(' ', 255));
                Console.WriteLine(new string(' ', 255));
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"{i}/{folders.Length}");
                Console.WriteLine(folders[i]);
                Console.WriteLine(string.Join("\n", list));
                string folderName = Path.GetFileName(folders[i]);
                int num = 0;
                while (num < 10000)
                {
                    string hashPathi = $@"/map/entryfilelist/{folderName}/{folderName.Replace("e", "i")}{num:D4}.entryfilelist";
                    string hashPathe = $@"/map/entryfilelist/{folderName}/{folderName}{num:D4}.entryfilelist";

                    if (File.Exists(@$"{gamePath}/{hashPathi}") && File.Exists(@$"{gamePath}/{hashPathe}"))
                        continue;

                    ulong hash = Util.ComputeHash(hashPathi, true);
                    if (entryfilelists.ContainsKey(hash))
                        list.Add($"Match Found: {entryfilelists[hash]} -> {hashPathi}");

                    hash = Util.ComputeHash(hashPathe, true);
                    if (entryfilelists.ContainsKey(hash))
                        list.Add($"Match Found: {entryfilelists[hash]} -> {hashPathe}");

                    num++;
                }

            }

        }
        private static void FileMagicSearch(string gamePath, string magic, bool bndSearch)
        {
            string[] files = Directory.GetFiles(gamePath, "*", SearchOption.AllDirectories);

            Console.WriteLine($"Searching {files.Length} for magic {magic}");

            List<string> list = new();
            Console.CursorVisible = false;
            for (int i = 0; i < files.Length; i++)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(new string(' ', 255));
                Console.WriteLine(new string(' ', 255));
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"{i}/{files.Length}");
                Console.WriteLine(files[i]);
                Console.WriteLine(string.Join("\n", list));
                try
                {
                    byte[] bytes = new byte[magic.Length];
                    File.OpenRead(files[i]).Read(bytes);
                    string magicString = Encoding.UTF8.GetString(bytes, 0, magic.Length);
                    if (magicString.Contains("DCX\0"))
                        magicString = Encoding.UTF8.GetString(DCX.Decompress(File.ReadAllBytes(files[i])), 0, magic.Length);

                    if (magicString == magic)
                        list.Add($"Matched: {files[i]} with magic {magic}");

                    if (bndSearch && magicString == "BND4")
                    {
                        BND4 bnd4 = BND4.Read(bytes);
                        foreach (BinderFile file in bnd4.Files)
                        {
                            magicString = Encoding.UTF8.GetString(file.Bytes, 0, magic.Length);

                            if (magicString == magic)
                                list.Add($"Matched: {files[i]} with magic {magic}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    list.Add($"Error in: {files[i]}");
                    list.Add(ex.Message);
                }


            }

        }

        private static void BNDSearch(string gamePath, string fileName, bool extensionless)
        {
            string[] files = Directory.GetFiles(gamePath, "*", SearchOption.AllDirectories);


            Console.WriteLine($"Searching {files.Length} for file {fileName}");

            List<string> list = new();
            Console.CursorVisible = false;
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(new string(' ', 255));
                    Console.WriteLine(new string(' ', 255));
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine($"{i}/{files.Length}");
                    Console.WriteLine(files[i]);
                    Console.WriteLine(string.Join("\n", list));

                    if (!BND4.IsRead(files[i], out BND4 bnd))
                        continue;

                    foreach (BinderFile file in bnd.Files)
                    {
                        string bndFileName = extensionless ? Path.GetFileNameWithoutExtension(file.Name) : Path.GetFileName(file.Name);

                        if (bndFileName == fileName)
                            list.Add($"Matched: {files[i]} with path {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    list.Add($"Error in: {files[i]}");
                    list.Add(ex.Message);
                }
           
            }
        }
    }
}