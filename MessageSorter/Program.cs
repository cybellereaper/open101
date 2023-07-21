using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MessageOrganiser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a file path as a command-line argument.");
                return;
            }

            string filePath = args[0];
            string fileName = Path.GetFileName(filePath);
            string currentFolder = Path.GetDirectoryName(filePath);

            string messages = File.ReadAllText(filePath);

            string serviceId = ExtractTagContent(messages, "<ServiceID TYPE=\"UBYT\">", "</ServiceID>");
            string serviceName = ExtractTagContent(messages, "<ProtocolType TYPE=\"STR\">", "</ProtocolType>");

            string cleanedMessages = CleanMessages(messages);

            string[] filteredLines = cleanedMessages
                .Split(new char[] { '\n' })
                .Where(line => !line.Trim().StartsWith("</") && !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim().Substring(1, line.Trim().Length - 2))
                .Distinct()
                .OrderBy(line => line, StringComparer.Ordinal)
                .ToArray();

            string outputFile = Path.Combine(currentFolder, $"{serviceId}_{serviceName}.txt");
            WriteOutputToFile(outputFile, filteredLines);
        }

        static string ExtractTagContent(string input, string startTag, string endTag)
        {
            int startPos = input.IndexOf(startTag) + startTag.Length;
            int length = input.IndexOf(endTag) - startPos;
            return input.Substring(startPos, length);
        }

        static string CleanMessages(string messages)
        {
            int startProtocolInfo = messages.IndexOf("</_ProtocolInfo>") + "</_ProtocolInfo>".Length;
            string cleanedMessages = messages.Substring(startProtocolInfo);

            var recordTagIndices = Regex.Matches(cleanedMessages, "<RECORD>");
            var recordCloseTagIndices = Regex.Matches(cleanedMessages, "</RECORD>");

            var stringBuilder = new StringBuilder(cleanedMessages);
            for (var i = recordCloseTagIndices.Count - 1; i >= 0; i--)
            {
                var a = recordTagIndices[i].Index;
                var b = recordCloseTagIndices[i].Index + "</RECORD>".Length;
                stringBuilder.Remove(a, b - a);
            }

            return stringBuilder.ToString();
        }

        static void WriteOutputToFile(string filePath, string[] lines)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    writer.WriteLine($"{i + 1}: {lines[i]}");
                }
            }
        }
    }
}
