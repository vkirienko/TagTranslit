using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace TagTranslit
{
    class Program
    {
        static readonly Options options = new Options();
        static readonly Dictionary<char, string> translitMap = new Dictionary<char, string>();

        static void Main(string[] args)
        {
            using (var parser = new Parser(with => with.HelpWriter = null))
            {
                var parserResult = parser.ParseArguments<Options>(args);
                parserResult
                  .WithParsed<Options>(options => Run(options))
                  .WithNotParsed(errs => DisplayHelp(parserResult, errs));
            }
        }

        static int Run(Options options)
        {
            // load translation map 
            LoadTranslitMap(options.TranslitMap);

            // list of files for processing
            var files = new List<string>();

            foreach (var name in options.Input)
            {
                if (File.Exists(name))
                    files.Add(name);
                else
                    GetFiles(name, options.Recursive, files);
            }

            int retCode = 0;
            files.ForEach(f => retCode &= ProcessFile(f));

            return retCode;
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> _)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddPreOptionsLine(" ");
                h.AddPreOptionsLine("This is free software. You may redistribute copies of it under the terms of");
                h.AddPreOptionsLine("the MIT License <http://www.opensource.org/licenses/mit-license.php>.");
                h.AddPreOptionsLine(" ");
                h.AddPreOptionsLine("Usage: TagTranslit [-n] [-t] [-r] [-m MappingFile] file(s) | folder(s)");
                h.AddPreOptionsLine("       TagTranslit Mp3File.mp3");
                h.AddPreOptionsLine("       TagTranslit \"C:\\My Mp3 Files\\Rock\"");
                h.AddPreOptionsLine("       TagTranslit -n -r -m MyTranslitMap.xml Mp3File.mp3 \"C:\\MyMp3Files\"");

                h.AdditionalNewLineAfterOption = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            Console.WriteLine(helpText);
        }

        static int ProcessFile(string srcFile)
        {
            try
            {
                Console.Write(srcFile);

                var destFile = srcFile;

                if (!options.NoTranslitFileName)
                {
                    var file = Path.GetFileName(srcFile);
                    var dir = Path.GetDirectoryName(srcFile);
                    destFile = Path.Combine(dir, Translit(file));
                    File.Move(srcFile, destFile);
                }

                if (!options.NoTranslitMp3Tags)
                {
                    using (var tagFile = TagLib.File.Create(destFile))
                    { 
                        TagLib.ByteVector.UseBrokenLatin1Behavior = true;

                        tagFile.Tag.Album = Translit(tagFile.Tag.Album);
                        tagFile.Tag.Title = Translit(tagFile.Tag.Title);
                        tagFile.Tag.Comment = Translit(tagFile.Tag.Comment);

                        var len = tagFile.Tag.AlbumArtists.Length;
                        if (len > 0)
                        {
                            var arr = new string[len];
                            for (var i = 0; i < len; i++)
                                arr[i] = Translit(tagFile.Tag.AlbumArtists[i]);
                            tagFile.Tag.AlbumArtists = arr;
                        }

                        len = tagFile.Tag.Performers.Length;
                        if (len > 0)
                        {
                            var arr = new string[len];
                            for (var i = 0; i < len; i++)
                                arr[i] = Translit(tagFile.Tag.Performers[i]);
                            tagFile.Tag.Performers = arr;
                        }

                        /*
                        var tagId3v1 = tagFile.GetTag(TagTypes.Id3v1) as TagLib.Id3v1.Tag;
                        if (tagId3v1 != null)
                        {
                            tagId3v1.Performers = new string[] { Translit(tagId3v1.Performers[0]) };
                        }

                        var tagId3v2 = tagFile.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
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
            }
            catch
            {
                Console.WriteLine(" - failed");
                throw;
            }

            return 0;
        }

        static string Translit(string src)
        {
            if (string.IsNullOrEmpty(src))
                return src;

            var srcEncodingFormat = Encoding.GetEncoding("windows-1252");
            var destEncodingFormat = Encoding.GetEncoding("windows-1251");

            var srcByteString = srcEncodingFormat.GetBytes(src);
            var temp = destEncodingFormat.GetString(srcByteString);

            if (IsCorrectEncoding(temp))
                temp = src;

            var dest = new StringBuilder();

            for (var i = 0; i < temp.Length; i++)
            {
                var c = temp[i];
                if (translitMap.ContainsKey(c))
                    dest.Append(translitMap[c]);
                else
                    dest.Append(c);
            }

            return dest.ToString();
        }

        static void LoadTranslitMap(string mapSource)
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.Load(mapSource);

            foreach (XmlNode node in doc.SelectNodes("//Transliteration/element"))
            {
                var from = node.Attributes["from"].InnerText[0];
                var to = node.Attributes["to"].InnerText;
                translitMap.Add(from, to);
            }
        }

        public static bool IsCorrectEncoding(string input)
        {
            var cnt = input.Where(c => c == '?').Count();
            return (cnt * 100 / input.Length) > 25;
        }

        public static void GetFiles(string sourceDir, bool recursive, List<string> files)
        {
            if (!Directory.Exists(sourceDir))
                return;

            // Process the list of files found in the directory. 
            var fileEntries = Directory.GetFiles(sourceDir);
            foreach (string fileName in fileEntries)
                files.Add(fileName);

            if (!recursive)
                return;

            // Recurse into subdirectories of this directory.
            var subdirEntries = Directory.GetDirectories(sourceDir);
            foreach (var subdir in subdirEntries)
                // Do not iterate through reparse points
                if ((File.GetAttributes(subdir) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    GetFiles(subdir, recursive, files);
        }
    }
}
