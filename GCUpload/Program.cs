/*
    GCUpload - Uploads a file to a googlecode.com project.
    Copyright (C) 2009 Brodrick E. Bassham, Jr.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Text;

namespace GCUpload
{
    class Program
    {
        static void Main(string[] args)
        {
            string username = null;
            string password = null;
            string projectName = null;
            string summary = null;
            List<string> labels = null;
            string targetFileName = null;
            string fileName = null;
            string url = null;
            bool quiet = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("-u", StringComparison.InvariantCultureIgnoreCase))
                {
                    username = arg.Substring(3);
                }
                else if (arg.StartsWith("-p", StringComparison.InvariantCultureIgnoreCase))
                {
                    password = arg.Substring(3);
                }
                else if (arg.StartsWith("-n", StringComparison.InvariantCultureIgnoreCase))
                {
                    projectName = arg.Substring(3);
                }
                else if (arg.StartsWith("-s", StringComparison.InvariantCultureIgnoreCase))
                {
                    summary = arg.Substring(3);
                }
                else if (arg.StartsWith("-t", StringComparison.InvariantCultureIgnoreCase))
                {
                    targetFileName = arg.Substring(3);
                }
                else if (arg.StartsWith("-f", StringComparison.InvariantCultureIgnoreCase))
                {
                    fileName = arg.Substring(3);
                }
                else if (arg.StartsWith("-l", StringComparison.InvariantCultureIgnoreCase))
                {
                    labels = new List<string>(arg.Substring(3).Split(','));
                }
                else if (arg.StartsWith("--url", StringComparison.InvariantCultureIgnoreCase))
                {
                    url = arg.Substring(5);
                }
                else if (arg == "-q" || arg == "--quiet")
                {
                    quiet = true;
                }
            }

            if (!quiet)
            {
                Console.WriteLine("GCUpload - Uploads a file to a googlecode.com project.");
                Console.WriteLine("Copyright (C) 2009 Brodrick E. Bassham, Jr.");
                Console.WriteLine();
            }

            try
            {
                FileUpload.UploadFile(username, password, projectName, fileName, targetFileName, summary, labels, url);
                Console.WriteLine("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                PrintHelp();
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: GCUpload.exe -u:<username> -p:<password> -n:<projectName> -s:<summary> -f:<fileName> [-t:<targetFileName>] [-l:<label1>[,<label2>,...]] [--url:<uploadUrl>] [--quiet|-q]");
        }
    }
}
