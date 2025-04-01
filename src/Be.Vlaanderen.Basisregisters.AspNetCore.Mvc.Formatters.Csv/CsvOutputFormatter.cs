namespace Be.Vlaanderen.Basisregisters.AspNetCore.Mvc.Formatters.Csv
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;

    /// <inheritdoc />
    /// <summary>
    /// Original code taken from
    /// http://www.tugberkugurlu.com/archive/creating-custom-csvmediatypeformatter-in-asp-net-web-api-for-comma-separated-values-csv-format
    /// Adapted for ASP.NET Core and uses ; instead of , for delimiters
    /// </summary>
    public class CsvOutputFormatter : OutputFormatter
    {
        private readonly CsvFormatterOptions _options;

        public static string Format { get; } = "csv";
        public static string ContentType { get; } = "text/csv";

        public CsvOutputFormatter(CsvFormatterOptions csvFormatterOptions)
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ContentType));

            _options = csvFormatterOptions ?? throw new ArgumentNullException(nameof(csvFormatterOptions));

            //SupportedEncodings.Add(Encoding.GetEncoding("utf-8"));
        }

        protected override bool CanWriteType(Type? type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return IsTypeOfIEnumerable(type);
        }

        private static bool IsTypeOfIEnumerable(Type type) => type.GetInterfaces().Any(interfaceType => interfaceType == typeof(IList));

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Object);

            var response = context.HttpContext.Response;

            var type = context.Object.GetType();
            var itemType = type.GetGenericArguments().Length > 0 ? type.GetGenericArguments()[0] : type.GetElementType();

            ArgumentNullException.ThrowIfNull(itemType);

            var stringWriter = new StringWriter();

            if (_options.UseSingleLineHeaderInCsv)
                await stringWriter.WriteLineAsync(string.Join<string>(_options.CsvDelimiter, itemType.GetProperties().Select(x => x.Name)));

            foreach (var obj in (IEnumerable<object>)context.Object)
            {
                var vals = obj.GetType().GetProperties().Select(pi => pi.GetValue(obj, null));

                var valueLine = string.Empty;

                foreach (var val in vals)
                {
                    var innerValue = val?.ToString();
                    if (innerValue is not null)
                    {
                        //Check if the value contains a comma and place it in quotes if so
                        if (innerValue.Contains(_options.CsvDelimiter))
                            innerValue = string.Concat("\"", innerValue, "\"");

                        //Replace any \r or \n special characters from a new line with a space
                        innerValue = innerValue.Replace("\r", " ");
                        innerValue = innerValue.Replace("\n", " ");

                        valueLine = string.Concat(valueLine, innerValue, _options.CsvDelimiter);
                    }
                    else
                    {
                        valueLine = string.Concat(valueLine, string.Empty, _options.CsvDelimiter);
                    }
                }

                await stringWriter.WriteLineAsync(valueLine.TrimEnd(_options.CsvDelimiter.ToCharArray()));
            }

            var streamWriter = new StreamWriter(response.Body);
            await streamWriter.WriteAsync(stringWriter.ToString());
            await streamWriter.FlushAsync();
        }
    }
}
