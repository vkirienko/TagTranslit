using CommandLine;
using System.Collections.Generic;


namespace TagTranslit
{
    sealed class Options
    {
        [Value(0)]
        public IEnumerable<string> Input { get; set; }

        [Option('m', "map", Required = false, HelpText = "Specifies optional file with transliteration map. Default.xml will be used if parameter omited.")]
        public string TranslitMap { get; set; }

        [Option('n', "name", Required = false, HelpText = "Don't transliterate file names.")]
        public bool NoTranslitFileName { get; set; }

        [Option('t', "tag", Required = false, HelpText = "Don't transliterate mp3 tags.")]
        public bool NoTranslitMp3Tags { get; set; }

        [Option('r', "recursive", Required = false, HelpText = "Recursive folder processing.")]
        public bool Recursive { get; set; }

        public Options()
        {
            TranslitMap = "Default.xml";
        }
    }
}
