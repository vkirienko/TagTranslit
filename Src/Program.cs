using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine;
using CommandLine.Text;
using TagLib;

/*
 * TagLib# (aka taglib-sharp): http://github.com/mono/taglib-sharp
 * CommandLine: http://commandline.codeplex.com
 */

namespace TagTranslit
{
    class Program
    {
        static Options options = new Options();
        static Dictionary<char, string> translitMap = new Dictionary<char, string>();

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Write(options.GetUsage());
                Environment.Exit(1);
            }

            CommandLineParserSettings settings = new CommandLineParserSettings(Console.Error);
            settings.MutuallyExclusive = true;
            settings.CaseSensitive = false;

            ICommandLineParser parser = new CommandLineParser(settings);
            if (!parser.ParseArguments(args, options))
                Environment.Exit(1);

            // load translation map 
            LoadTranslitMap(options.TranslitMap);

            // list of files for processing
            List<string> files = new List<string>();

            foreach(string name in options.Input)
            {
                if (System.IO.File.Exists(name))
                    files.Add(name);
                else
                    GetFiles(name, options.Recursive, files);
            }

            int retCode = 0;
            files.ForEach(f => retCode &= ProcessFile(f));

            Environment.Exit(retCode);
        }

        static int ProcessFile(string srcFile)
        {
            try
            {
                Console.Write(srcFile);

                string destFile = srcFile;

                if (!options.NoTranslitFileName)
                {
                    string file = Path.GetFileName(srcFile);
                    string dir = Path.GetDirectoryName(srcFile);
                    destFile = Path.Combine(dir, Translit(file));
                    System.IO.File.Move(srcFile, destFile);
                    //destFile = Translit(srcFile);
                    //System.IO.File.Move(srcFile, destFile);
                }

                if (!options.NoTranslitMp3Tags)
                {
                    TagLib.File tagFile = TagLib.File.Create(destFile);

                    TagLib.ByteVector.UseBrokenLatin1Behavior = true;

                    tagFile.Tag.Album = Translit(tagFile.Tag.Album);
                    tagFile.Tag.Title = Translit(tagFile.Tag.Title);
                    tagFile.Tag.Comment = Translit(tagFile.Tag.Comment);

                    int len = tagFile.Tag.AlbumArtists.Length;
                    if (len > 0)
                    {
                        string[] arr = new string[len];
                        for (int i = 0; i < len; i++)
                            arr[i] = Translit(tagFile.Tag.AlbumArtists[i]);
                        tagFile.Tag.AlbumArtists = arr;
                    }

                    len = tagFile.Tag.Performers.Length;
                    if (len > 0)
                    {
                        string[] arr = new string[len];
                        for (int i = 0; i < len; i++)
                            arr[i] = Translit(tagFile.Tag.Performers[i]);
                        tagFile.Tag.Performers = arr;
                    }

                    /*
                    TagLib.Id3v1.Tag tagId3v1 = tagFile.GetTag(TagTypes.Id3v1) as TagLib.Id3v1.Tag;
                    if (tagId3v1 != null)
                    {
                        tagId3v1.Performers = new string[] { Translit(tagId3v1.Performers[0]) };
                    }

                    TagLib.Id3v2.Tag tagId3v2 = tagFile.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                    if (tagId3v2 != null)
                    {
                        tagId3v2.AlbumArtists = new string[] { Translit(tagId3v2.AlbumArtists[0]) };
                        tagId3v2.Performers = new string[] { Translit(tagId3v2.Performers[0]) };
                    }
                    */
                    tagFile.Save();

                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" - failed (" + ex.Message + ")");
                return 1;
            }

            return 0;
        }

        static string Translit(string src)
        {
            if (string.IsNullOrEmpty(src))
                return src;

            string temp = src;

            Encoding srcEncodingFormat = Encoding.GetEncoding("windows-1252");
            Encoding destEncodingFormat = Encoding.GetEncoding("windows-1251");
            
            byte[] srcByteString = srcEncodingFormat.GetBytes(src);
            temp = destEncodingFormat.GetString(srcByteString);

            if (IsCorrectEncoding(temp))
                temp = src;

            StringBuilder dest = new StringBuilder();

            for (int i = 0; i < temp.Length; i++)
            {
                char c = temp[i];
                if (translitMap.ContainsKey(c))
                    dest.Append(translitMap[c]);
                else
                    dest.Append(c);
            }

            return dest.ToString();
        }

        static void LoadTranslitMap(string mapSource)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(mapSource);

            foreach (XmlNode node in doc.SelectNodes("//Transliteration/element"))
            {
                char from = node.Attributes["from"].InnerText[0];
                string to = node.Attributes["to"].InnerText;
                translitMap.Add(from, to);
            }
        }

        public static bool IsCorrectEncoding(string input)
        {
            int cnt = input.Where(c => c == '?').Count();
            return (cnt * 100 / input.Length) > 25;
        }

        public static void GetFiles(string sourceDir, bool recursive, List<string> files)
        {
            if (!Directory.Exists(sourceDir))
                return;

            // Process the list of files found in the directory. 
            string[] fileEntries = Directory.GetFiles(sourceDir);
            foreach (string fileName in fileEntries)
                files.Add(fileName);

            if (!recursive)
                return;

            // Recurse into subdirectories of this directory.
            string[] subdirEntries = Directory.GetDirectories(sourceDir);
            foreach (string subdir in subdirEntries)
                // Do not iterate through reparse points
                if ((System.IO.File.GetAttributes(subdir) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    GetFiles(subdir, recursive, files);
        }
    }
}
