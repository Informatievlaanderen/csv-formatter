namespace Be.Vlaanderen.Basisregisters.AspNetCore.Mvc.Formatters.Csv
{
    public class CsvFormatterOptions
    {
        public bool UseSingleLineHeaderInCsv { get; set; } = true;

        public string CsvDelimiter { get; set; } = ";";
    }
}
