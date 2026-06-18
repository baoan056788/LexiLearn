namespace LexiLearn.ViewModels
{
    public class AiDictionaryViewModel
    {
        public string Word { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public string VietnameseMeaning { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public List<AiDictionaryDefinitionViewModel> Definitions { get; set; } = new();
        public List<string> Synonyms { get; set; } = new();
        public List<string> Antonyms { get; set; } = new();
        public string Note { get; set; } = string.Empty;
    }

    public class AiDictionaryDefinitionViewModel
    {
        public string Definition { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string ExampleVi { get; set; } = string.Empty;
    }
}
