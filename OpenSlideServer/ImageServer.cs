using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace OpenSlideServer
{
    public class Settings
    {
        public string location;
        public string macro;
        public string dicom;
        public string cache;
        public string transfer;
        public string[] prefixes;
    }

    public class ImageMetaData
    {
        public string name;
        public bool mirror;
    }

    public class MetaData
    {
        public ImageMetaData[] images;
    }

    public class ImageServer
    {
        private Dictionary<string, bool> _mirror;

        public ImageServer()
        {
            var listener = new HttpListener();
            var settings = LoadSettings();
            var metaData = LoadMetaData();

            _mirror = new Dictionary<string, bool>();

            foreach (var image in metaData.images)
            {
                _mirror.Add(image.name, image.mirror);
            }

            foreach (var prefix in settings.prefixes)
            {
                listener.Prefixes.Add(prefix);
            }

            listener.Start();

            Console.WriteLine("Server running ...");

            while (true)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(o => HandleRequest(context, settings));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public Settings LoadSettings()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                var json = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<Settings>(json);
            }
        }

        public MetaData LoadMetaData()
        {
            using (StreamReader sr = new StreamReader("metadata.json"))
            {
                var json = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<MetaData>(json);
            }
        }

        private void HandleRequest(object state, Settings settings)
        {
            var context = (HttpListenerContext)state;

            Console.WriteLine("");
            Console.WriteLine(context.Request.HttpMethod);
            Console.WriteLine(context.Request.RawUrl);
            Console.WriteLine(context.Request.RemoteEndPoint.ToString());

            var start = DateTime.Now;
            Console.WriteLine(start.ToString("MMM d  h m s ff"));

            try
            {
                if (context.Request.HttpMethod.Equals("POST") && context.Request.ContentType.Equals("text/plain; charset=UTF-8"))
                {
                    HandlePostRequest(context, settings.transfer);
                }
                else if (context.Request.HttpMethod.Equals("GET"))
                {
                    var command = context.Request.QueryString.Get("command");
                    if (command == "image")
                    {
                        HandleImageRequest(context, settings.location, settings.cache);
                    }
                    else if (command == "file")
                    {
                        HandleFileRequest(context, settings.transfer);
                    }
                    else if (command == "macro")
                    {
                        HandleMacroRequest(context, settings.macro, settings.cache);
                    }
                    else if (command == "list")
                    {
                        HandleListRequest(context, settings.location);
                    }
                    else if (command == "macro_list")
                    {
                        HandleListRequest(context, settings.macro);
                    }
                    else if (command == "cases")
                    {
                        HandleCasesRequest(context, settings.location);
                    }
                    else if (command == "details")
                    {
                        HandleDetailsRequest(context, settings.location);
                    }
                    else if (command == "macro_cases")
                    {
                        HandleCasesRequest(context, settings.macro);
                    }
                    else if (command == "macro_details")
                    {
                        HandleMacroDetailsRequest(context, settings.macro);
                    }
                    else if (command == "dicom")
                    {
                        HandleDicomRequest(context, settings.dicom);
                    }
                    else if (command == "dicom_list")
                    {
                        HandleDicomCasesRequest(context, settings.dicom);
                    }
                    else if (command == "dicom_details")
                    {
                        HandleDicomDetailsRequest(context, settings.dicom);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var end = DateTime.Now;

            Console.WriteLine(context.Response.StatusCode.ToString());
            Console.WriteLine("Time:" + (end - start).TotalMilliseconds.ToString() + " ms");
        }

        private void HandlePostRequest(HttpListenerContext context, string cache)
        {
            try
            {
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new StreamReader(context.Request.InputStream, encoding))
                {
                    var body = reader.ReadToEnd();

                    var o = JObject.Parse(body);
                    if (o.TryGetValue("file", out JToken file))
                    {
                        if (o.TryGetValue("content", out JToken content))
                        {
                            var fileName = cache + file.ToString();
                            Console.WriteLine(fileName);
                            File.WriteAllText(fileName, content.ToString());
                        }
                    }
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleCasesRequest(HttpListenerContext context, string location)
        {
            try
            {
                var dirs = Directory.GetDirectories(location);
                var json = "{\r\n\t\"Cases\":[\r\n";

                for (var i = 0; i < dirs.Length; i++)
                {
                    json += "\t\t\"" + Path.GetFileName(dirs[i]) + "\"" + ((i < dirs.Length - 1) ? ", \r\n" : "");
                }
                json += "\r\n\t]\r\n}";

                var bytes = Encoding.ASCII.GetBytes(json);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleListRequest(HttpListenerContext context, string location)
        {
            try
            {
                var caseID = context.Request.QueryString.Get("caseID");
                var files = Directory.GetFiles(location + "\\" + caseID + "\\");
                var json = "{\r\n\t\"Images\":[\r\n";

                for (var i = 0; i < files.Length; i++)
                {
                    json += "\t\t\"" + Path.GetFileName(files[i]) + "\"" + ((i < files.Length - 1) ? ", \r\n" : "");
                }
                json += "\r\n\t]\r\n}";

                var bytes = Encoding.ASCII.GetBytes(json);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleDetailsRequest(HttpListenerContext context, string location)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                var caseID = context.Request.QueryString.Get("caseID");
                var bytes = Utilities.ImageDetails(location + "\\" + caseID + "\\" + name);

                if (bytes == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";

                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleImageRequest(HttpListenerContext context, string location, string cache)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                var caseID = context.Request.QueryString.Get("caseID");
                var level = Int32.Parse(context.Request.QueryString.Get("level"));
                var format = context.Request.QueryString.Get("format");
                var mirror = context.Request.QueryString.Get("mirror");

                var x = Int64.Parse(context.Request.QueryString.Get("x"));
                var y = Int64.Parse(context.Request.QueryString.Get("y"));
                var w = Int32.Parse(context.Request.QueryString.Get("w"));
                var h = Int32.Parse(context.Request.QueryString.Get("h"));

                Console.WriteLine("Image:" + name
                        + ", level:" + level.ToString()
                        + ", X:" + x.ToString() + ", Y:" + y.ToString()
                        + ", W:" + w.ToString() + ", H:" + h.ToString());

                if (format == null)
                {
                    format = "PNG";
                }

                bool mirrorImage = false;

                if (mirror != null)
                {
                    mirrorImage = bool.Parse(mirror);
                }

                Console.WriteLine("Name: " + name);

                if (!mirrorImage && _mirror.ContainsKey(name))
                {
                    mirrorImage = true;
                }

                if (mirrorImage)
                {
                    Console.WriteLine("Mirror image");
                }

                format = format.ToUpper();

                Console.WriteLine("Format: " + format);

                if (w < 0 || h < 0 || w > 10000 || h > 10000 || level < 0)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                }
                else
                {
                    var fileName = name
                            + "&x=" + x.ToString()
                            + "&y=" + y.ToString()
                            + "&w=" + w.ToString()
                            + "&h=" + h.ToString()
                            + "&level=" + level.ToString()
                            + "&mirror=" + mirrorImage.ToString();

                    var folder = (cache + caseID + "\\" + name + "\\" + level);
                    Directory.CreateDirectory(folder);

                    var f = (folder + "\\" + fileName + "." + format).ToLower();

                    var bytes = Utilities.LoadFile(f);

                    if (bytes == null)
                    {
                        bytes = Utilities.CreateRegion(location + "\\" + caseID + "\\" + name, level, x, y, w, h, format, mirrorImage);
                        if (bytes != null)
                        {
                            Console.WriteLine("Saving file: " + f);
                            Utilities.SaveFile(f, bytes);
                        }
                    }

                    if (bytes == null)
                    {
                        Console.WriteLine("bytes == null");

                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        if (format == null || format.Equals("PNG"))
                        {
                            context.Response.ContentType = "image/png";
                        }
                        else if (format.Equals("RAW"))
                        {
                            context.Response.ContentType = "application/octet-stream";
                        }
                        else if (format.Equals("JPG"))
                        {
                            context.Response.ContentType = "image/jpeg";
                        }
                        else if (format.Equals("BMP"))
                        {
                            context.Response.ContentType = "image/bmp";
                        }

                        context.Response.ContentLength64 = bytes.Length;
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleDicomCasesRequest(HttpListenerContext context, string dicom)
        {
            try
            {
                var files = Directory.GetFiles(dicom + "json\\");
                var json = "{\r\n\t\"Cases\":[\r\n";

                for (var i = 0; i < files.Length; i++)
                {
                    json += "\t\t\"" + Path.GetFileName(files[i]) + "\"" + ((i < files.Length - 1) ? ", \r\n" : "");
                }
                json += "\r\n\t]\r\n}";

                var bytes = Encoding.ASCII.GetBytes(json);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";

                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleDicomDetailsRequest(HttpListenerContext context, string dicom)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                Console.WriteLine("Json:" + name);

                var start = DateTime.Now;
                Console.WriteLine("Start:" + start.ToString(" h:m:s.ff"));

                var f = (dicom + "json\\" + name).ToLower();
                var bytes = Utilities.LoadFile(f);

                var end = DateTime.Now;
                Console.WriteLine("Time:" + (end - start).TotalMilliseconds.ToString() + " ms");

                if (bytes == null)
                {
                    Console.WriteLine("bytes == null");

                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleDicomRequest(HttpListenerContext context, string dicom)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                Console.WriteLine("Image:" + name);

                var start = DateTime.Now;
                Console.WriteLine("Start:" + start.ToString(" h:m:s.ff"));

                var f = (dicom + "images\\" + name + "." + "jpg").ToLower();
                var bytes = Utilities.LoadFile(f);

                var end = DateTime.Now;
                Console.WriteLine("Time:" + (end - start).TotalMilliseconds.ToString() + " ms");

                if (bytes == null)
                {
                    Console.WriteLine("bytes == null");

                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "image/jpeg";
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleMacroRequest(HttpListenerContext context, string location, string cache)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                var caseID = context.Request.QueryString.Get("caseID");
                var format = context.Request.QueryString.Get("format");

                var level = int.Parse(context.Request.QueryString.Get("level"));

                var x = int.Parse(context.Request.QueryString.Get("x")) / (int)Math.Pow(2, level);
                var y = int.Parse(context.Request.QueryString.Get("y")) / (int)Math.Pow(2, level);
                var w = int.Parse(context.Request.QueryString.Get("w"));
                var h = int.Parse(context.Request.QueryString.Get("h"));

                Console.WriteLine("Image:" + name
                        + ", X:" + x.ToString() + ", Y:" + y.ToString()
                        + ", W:" + w.ToString() + ", H:" + h.ToString());

                if (format == null)
                {
                    format = "PNG";
                }

                format = format.ToUpper();

                Console.WriteLine("Format: " + format);

                if (w < 0 || h < 0 || w > 10000 || h > 10000)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                }
                else
                {
                    var fileName = name
                            + "&x=" + x.ToString()
                            + "&y=" + y.ToString()
                            + "&w=" + w.ToString()
                            + "&h=" + h.ToString()
                            + "&level=" + level.ToString();

                    var folder = (cache + caseID + "\\" + name + "\\" + level);
                    Directory.CreateDirectory(folder);

                    var f = (folder + "\\" + fileName + "." + format).ToLower();

                    var bytes = Utilities.LoadFile(f);

                    if (bytes == null)
                    {
                        bytes = Utilities.CreateMacro(location + "\\" + caseID + "\\" + level.ToString() + "\\" + name, x, y, w, h, format);
                        if (bytes != null)
                        {
                            Console.WriteLine("Saving file: " + f);
                            Utilities.SaveFile(f, bytes);
                        }
                    }

                    if (bytes == null)
                    {
                        Console.WriteLine("bytes == null");

                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        if (format == null || format.Equals("PNG"))
                        {
                            context.Response.ContentType = "image/png";
                        }
                        else if (format.Equals("JPG"))
                        {
                            context.Response.ContentType = "image/jpeg";
                        }
                        else if (format.Equals("BMP"))
                        {
                            context.Response.ContentType = "image/bmp";
                        }

                        context.Response.ContentLength64 = bytes.Length;
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleMacroDetailsRequest(HttpListenerContext context, string location)
        {
            try
            {
                var name = context.Request.QueryString.Get("name");
                var caseID = context.Request.QueryString.Get("caseID");
                var bytes = Utilities.MacroDetails(location + "\\" + caseID + "\\" + name);

                if (bytes == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";

                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }

        private void HandleFileRequest(HttpListenerContext context, string transferFolder)
        {
            try
            {
                var name = context.Request.QueryString.Get("file");
                Console.WriteLine("File:" + name);

                var f = (transferFolder + name).ToLower();
                byte[] bytes = null;

                try
                {
                    bytes = Utilities.LoadFile(f);
                }
                catch (Exception)
                {
                    Console.WriteLine("File access collision");
                    bytes = null;
                }

                if (bytes == null)
                {
                    var json = "{\r\n\t\"valid\": false\r\n}";
                    var b = Encoding.ASCII.GetBytes(json);

                    Console.WriteLine("Not found");

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = b.Length;
                    context.Response.OutputStream.Write(b, 0, b.Length);
                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
            }
        }
    }
}
