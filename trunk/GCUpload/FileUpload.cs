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
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GCUpload
{
    public class FileUpload
    {
        private static readonly string BOUNDARY = "BASSHAMMAHSSAB";
        private static readonly string URL_FORMAT = "https://{0}.googlecode.com/files";
        private static readonly string METHOD = "POST";
        private static readonly string CONTENT_TYPE = string.Format("multipart/form-data; boundary={0}", BOUNDARY);
        private static readonly string USER_AGENT = "Bassham GoogleCode Uploader v1.0";

        public FileUpload()
        {
        }

        private static Uri GetUploadUrl(string url, string projectName)
        {
            if (!string.IsNullOrEmpty(url))
            {
                return new Uri(url);
            }
            else
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    throw new ArgumentException("You must supply either a url or a projectName");
                }

                return new Uri(string.Format(URL_FORMAT, projectName));
            }
        }

        private static string GetAuthToken(string userName, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", userName, password)));
        }

        private static void WriteToStream(Stream dest, string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);

            dest.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Uploads a file to a specified Google Code project.
        /// </summary>
        /// <param name="userName">Your googlecode.com username.</param>
        /// <param name="password">Your googlecode.com password.  NOTE: This is NOT your gmail or other google account password.  This is the password you use to connect to SVN.  It can be found at http://code.google.com/hosting/settings/ .</param>
        /// <param name="projectName">The googlecode.com Project.</param>
        /// <param name="fileName">The path to the file you want to upload.</param>
        /// <param name="summary">A description of the file.  This must not be null.</param>
        public static void UploadFile(
            string userName,
            string password,
            string projectName,
            string fileName,
            string summary)
        {
            UploadFile(userName, password, projectName, fileName, null, summary, null, null);
        }

        /// <summary>
        /// Uploads a file to a specified Google Code project.
        /// </summary>
        /// <param name="userName">Your googlecode.com username.</param>
        /// <param name="password">Your googlecode.com password.  NOTE: This is NOT your gmail or other google account password.  This is the password you use to connect to SVN.  It can be found at http://code.google.com/hosting/settings/ .</param>
        /// <param name="projectName">The googlecode.com Project.</param>
        /// <param name="fileName">The path to the file you want to upload.</param>
        /// <param name="targetFileName">The name you want the file to have once uploaded.</param>
        /// <param name="summary">A description of the file.  This must not be null.</param>
        /// <param name="labels">A list of labels to give the uploaded file.  Example: A label of "Featured" will make the download show up on the project home page.</param>
        /// <param name="uploadUrl">The URL to upload the file to.  Usually you will set this to null and it will be generated from the project name.  Do not set this unless you know what you are doing.</param>
        public static void UploadFile(
            string userName,
            string password,
            string projectName,
            string fileName,
            string targetFileName,
            string summary,
            List<string> labels,
            string uploadUrl)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("You must specify a file to upload.", "fileName");
            }

            if (string.IsNullOrEmpty(summary))
            {
                throw new ArgumentException("You must specify a description of the file.", "summary");
            }

            if (string.IsNullOrEmpty(targetFileName))
            {
                targetFileName = Path.GetFileName(fileName);
            }

            Uri uploadUri = GetUploadUrl(uploadUrl, projectName);

            System.Diagnostics.Trace.WriteLine(uploadUri.ToString());

            HttpWebRequest req = WebRequest.Create(uploadUri) as HttpWebRequest;

            req.Method = METHOD;
            req.ContentType = CONTENT_TYPE;
            req.UserAgent = USER_AGENT;

            req.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", GetAuthToken(userName, password)));

            System.Diagnostics.Trace.WriteLine("Connecting...");

            using (Stream reqStream = req.GetRequestStream())
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(string.Format("--{0}", BOUNDARY));
                sb.AppendLine("Content-Disposition: form-data; name=\"summary\"");
                sb.AppendLine();
                sb.AppendLine(summary);

                if (labels != null && labels.Count > 0)
                {
                    foreach (string label in labels)
                    {
                        sb.AppendLine(string.Format("--{0}", BOUNDARY));
                        sb.AppendLine("Content-Disposition: form-data; name=\"label\"");
                        sb.AppendLine();
                        sb.AppendLine(label);
                    }
                }

                sb.AppendLine(string.Format("--{0}", BOUNDARY));
                sb.AppendLine(string.Format("Content-Disposition: form-data; name=\"filename\"; filename=\"{0}\"", fileName));
                sb.AppendLine("Content-Type: application/octet-stream");
                sb.AppendLine();

                WriteToStream(reqStream, sb.ToString());

                using (FileStream f = File.OpenRead(fileName))
                {
                    int bufferSize = 4096;
                    byte[] buffer = new byte[bufferSize];
                    int count = 0;

                    while ((count = f.Read(buffer, 0, bufferSize)) > 0)
                    {
                        reqStream.Write(buffer, 0, count);
                    }
                }

                sb = new StringBuilder();

                sb.AppendLine();
                sb.AppendLine(string.Format("--{0}--", BOUNDARY));

                WriteToStream(reqStream, sb.ToString());
            }

            try
            {
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                {
                    System.Diagnostics.Trace.WriteLine(res.Headers.ToString());
                    System.Diagnostics.Trace.WriteLine(new StreamReader(res.GetResponseStream()).ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    HttpWebResponse res = ex.Response as HttpWebResponse;

                    System.Diagnostics.Trace.WriteLine(res.StatusDescription);
                    string responseBody;

                    using (StreamReader rdr = new StreamReader(res.GetResponseStream()))
                    {
                        responseBody = rdr.ReadToEnd();

                        responseBody = Regex.Replace(
                            Regex.Replace(responseBody, "<br(\\s*/*)>", "\\n"),
                            "(<.*?>)", string.Empty).Replace("&nbsp;", " ");

                        System.Diagnostics.Trace.WriteLine(responseBody);
                    }

                    // Put the response body in the exception, since google puts the actual error there.
                    throw new Exception(responseBody, ex);
                }
            }
        }

        /// <summary>
        /// Uploads a file to a specified Google Code project.
        /// </summary>
        public void UploadFile()
        {
            FileUpload.UploadFile(this.UserName, this.Password, this.ProjectName, this.FileName, this.TargetFileName, this.Summary, this.Labels, this.UploadUrl);
        }

        /// <summary>
        /// Your googlecode.com username.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Your googlecode.com password.  NOTE: This is NOT your gmail or other google account password.  This is the password you use to connect to SVN.  It can be found at http://code.google.com/hosting/settings/.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The googlecode.com Project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The path to the file you want to upload.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The name you want the file to have once uploaded.
        /// </summary>
        public string TargetFileName { get; set; }

        /// <summary>
        /// A description of the file.  This must not be null.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// A list of labels to give the uploaded file.  Example: A label of "Featured" will make the download show up on the project home page.
        /// </summary>
        public List<string> Labels { get; set; }

        /// <summary>
        /// The URL to upload the file to.  Usually you will set this to null and it will be generated from the project name.  Do not set this unless you know what you are doing.
        /// </summary>
        public string UploadUrl { get; set; }
    }
}
