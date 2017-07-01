using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace TagTranslit
{
    sealed class Options
    {
        [ValueList(typeof(List<string>))]
        public IList<string> Input = null;

        [Option("m", "map", HelpText = "Specifies optional file with transliteration map. Default.xml will be used if parameter omited.")]
        public string TranslitMap = "Default.xml";

        [Option("n", "name", HelpText = "Don't transliterate file names.")]
        public bool NoTranslitFileName = false;

        [Option("t", "tag", HelpText = "Don't transliterate mp3 tags.")]
        public bool NoTranslitMp3Tags = false;

        [Option("r", "recursive", HelpText = "Recursive folder processing.")]
        public bool Recursive = false;

        [HelpOption("h", "help", HelpText = "Dispaly this help screen.")]
        public string GetUsage()
        {
            var help = new HelpText(new HeadingInfo("TagTranslit v1.0 - mp3 tag and file name transliterator", ""));
            //help.AdditionalNewLineAfterOption = true;
            help.Copyright = new CopyrightInfo("KVA Technologies", 2012);
            help.AddPreOptionsLine(" ");
            help.AddPreOptionsLine("This is free software. You may redistribute copies of it under the terms of");
            help.AddPreOptionsLine("the MIT License <http://www.opensource.org/licenses/mit-license.php>.");
            help.AddPreOptionsLine(" ");
            help.AddPreOptionsLine("Usage: TagTranslit [-n] [-t] [-r] [-m MappingFile] file(s) | folder(s)");
            help.AddPreOptionsLine("       TagTranslit Mp3File.mp3");
            help.AddPreOptionsLine("       TagTranslit \"C:\\My Mp3 Files\\Rock\"");
            help.AddPreOptionsLine("       TagTranslit -n -r -m MyTranslitMap.xml Mp3File.mp3 \"C:\\MyMp3Files\"");
            help.AddOptions(this);
            return help;
        }
    }
}
