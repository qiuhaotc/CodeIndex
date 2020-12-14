using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using CodeIndex.IndexBuilder;

namespace CodeIndex.VisualStudioExtension
{
    class StringToXamlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value as string;

            if (input != null)
            {
                var textBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap
                };

                var escapedXml = input; // TODO: a good way to use SecurityElement.Escape(input);

                while (escapedXml.IndexOf(CodeContentProcessing.HighLightPrefix) != -1)
                {
                    var startIndex = escapedXml.IndexOf(CodeContentProcessing.HighLightPrefix);
                    var endIndex = escapedXml.IndexOf(CodeContentProcessing.HighLightSuffix);

                    if(startIndex < endIndex)
                    {
                        //up to start is normal
                        textBlock.Inlines.Add(new Run(escapedXml.Substring(0, startIndex)));

                        //between start and end is highlighted
                        textBlock.Inlines.Add(new Run(escapedXml.Substring(startIndex + CodeContentProcessing.HighLightPrefix.Length, endIndex - startIndex - CodeContentProcessing.HighLightPrefix.Length))

                        {
                            FontWeight = FontWeights.Bold,
                            Background = Brushes.OrangeRed
                        });

                        //the rest of the string (after the end)
                        escapedXml = escapedXml.Substring(endIndex + CodeContentProcessing.HighLightSuffix.Length);
                    }
                    else
                    {
                        escapedXml = escapedXml.Replace(CodeContentProcessing.HighLightPrefix, string.Empty).Replace(CodeContentProcessing.HighLightSuffix, string.Empty);
                        break;
                    }
                    // TODO: can high light the one like "<h>AAA</h> BBB <h>VCC</h> DDDD"
                }

                if (escapedXml.Length > 0)
                {
                    textBlock.Inlines.Add(new Run(escapedXml));
                }

                return textBlock;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This converter cannot be used in two-way binding.");
        }
    }
}
