using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace MessageOrganiser
{
    class Program
    {
        static void Main(string[] args)
        {
            string filetoopen = System.IO.Path.GetFileName(@args[0]);
            string currentfolder = System.IO.Path.GetDirectoryName(@args[0]);
            string Messages = File.ReadAllText(filetoopen);

            int startPos = Messages.IndexOf(@"<ServiceID TYPE=""UBYT"">") + @"<ServiceID TYPE=""UBYT"">".Length;
            int length = Messages.IndexOf("</ServiceID>") - startPos;
            string svcid = Messages.Substring(startPos, length);

            startPos = Messages.IndexOf(@"<ProtocolType TYPE=""STR"">") + @"<ProtocolType TYPE=""STR"">".Length;
            length = Messages.IndexOf("</ProtocolType>") - startPos;
            string svcname = Messages.Substring(startPos, length);

            var RECORD_tag_indicies = Regex.Matches(Messages, "<RECORD>");
            var RECORD_CLOSE_tag_indicies = Regex.Matches(Messages, "</RECORD>");
            var CLOSE_PROTOCOL_INFO = Regex.Match(Messages, "</_ProtocolInfo>");
            
            var aStringBuilder = new StringBuilder(Messages);
            for (var i = RECORD_CLOSE_tag_indicies.Count-1; i > 0; i--)
            {
                var a = RECORD_tag_indicies[i].Index;
                var b = RECORD_CLOSE_tag_indicies[i].Index + "</RECORD>".Length;
                aStringBuilder.Remove(a, b - a);
            }
            aStringBuilder.Remove(0, CLOSE_PROTOCOL_INFO.Index + "</_ProtocolInfo>".Length);
            Messages = aStringBuilder.ToString();

            var m_lines = Messages.Split(new char[] { '\n' });
            string[] filtered_lines = m_lines.Where(s => !s.Trim().StartsWith("</")).ToArray();
            filtered_lines = filtered_lines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            for (var j = 0; j < filtered_lines.Length; j++)
            {
                var t = filtered_lines[j].Trim();
                filtered_lines[j] = t.Substring(1, t.Length - 2);
            }

            //Array.Sort(filtered_lines, (x, y) => String.CompareOrdinal(x, y));

            var res_list = filtered_lines.Distinct().ToList();
            res_list.Sort((Comparison<String>)(
            (String left, String right) => 
            {
                return String.CompareOrdinal(left, right);
            }
            ));

            string filename = svcid + "_" + svcname + ".txt";
            string output = filename;
            
            for(int i = 0; i< res_list.Count; i++)
            {
                output = output + "\n" + (i+1) + ": " + res_list[i];
            }

            File.WriteAllText(currentfolder + "\\" + filename, output);
        }
    }
}
