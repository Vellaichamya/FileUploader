using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace WebApplication6.Controllers
{
    public class HomeController : Controller
    {
        string logFilename = @"C:\Data\FileUpload_Modified\Log.txt";
        string informationLogFilename = @"C:\Data\FileUpload_Modified\InfoLog.txt";
        bool enableLog = false;
        public HomeController()
        {
            if (!System.IO.File.Exists(informationLogFilename))
            {
                using (System.IO.File.Create(informationLogFilename))
                {

                }

            }

            if (enableLog)
            {
                if (!System.IO.File.Exists(logFilename))
                {
                    using (System.IO.File.Create(logFilename))
                    {

                    }

                }
            }
        }
        static string serverPath = "~/App_Data/uploads";
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Download()
        {
            DataSet ds = new DataSet();
            using (SqlConnection connection = new SqlConnection(@"Data Source=ABN-QNX-SQL-E01.dev.trizetto.com\SQL2017;Initial Catalog=PlanDocument_Penn; integrated security=true"))
            {
                connection.Open();


                String sql = @"
        SELECT B.qdocumentid, B.originalfilename   FROM qdocuments as A join QA_Process_57..memodocuments as B on A.qdocumentid =  B.qdocumentid";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    SqlDataAdapter ad = new SqlDataAdapter(cmd);
                    ad.Fill(ds);
                }

                connection.Close();
            }
            //Directory.CreateDirectory(Server.MapPath(serverPath));
            //string[] files = Directory.GetFiles(Server.MapPath(serverPath));
            //for (int i = 0; i < files.Length; i++)
            //{
            //    files[i] = Path.GetFileName(files[i]);
            //}

            List<document> docList = new List<document>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                document doc = new document()
                {
                    qdocumentid = ds.Tables[0].Rows[i]["qdocumentid"].ToString(),
                    fileName = ds.Tables[0].Rows[i]["originalfilename"].ToString()
                };
                docList.Add(doc);
            }

            ViewBag.Files = docList;
            return View();
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public HttpResponseMessage UploadFile()
        {

            try
            {
                if (Request.Params["mode"] != null && Request.Params["mode"].ToString() == "DELETE" && Request.Params["fileName"] != null)
                {
                    var fileName = Path.GetFileName(Request.Params["fileName"].ToString());
                    var UploadPath = Server.MapPath(serverPath);
                    Directory.CreateDirectory(UploadPath);
                    string filePath = Path.Combine(UploadPath, fileName);
                    CleanWorkArea(filePath);

                }
                else if (Request.Params["mode"] != null && Request.Params["mode"].ToString() == "MERGE" && Request.Params["fileName"] != null)
                {
                    var fileName = Path.GetFileName(Request.Params["fileName"].ToString());
                    var UploadPath = Server.MapPath(serverPath);
                    Directory.CreateDirectory(UploadPath);
                    string filePath = Path.Combine(UploadPath, fileName);
                    Shared.Utils UT = new Shared.Utils();
                    if (UT.MergeFile(filePath))
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        //string partToken = ".part_";
                        //string baseFileName = filePath.Substring(0, filePath.IndexOf(partToken));
                        UploadToDatabase(filePath);
                        watch.Stop();
                        System.IO.File.AppendAllText(informationLogFilename, "Database operation completed in :" + watch.ElapsedMilliseconds.ToString() +"ms" + Environment.NewLine);
                    }
                }
                else
                {
                    foreach (string file in Request.Files)
                    {
                        var FileDataContent = Request.Files[file];
                        if (FileDataContent != null && FileDataContent.ContentLength > 0)
                        {
                            var stream = FileDataContent.InputStream;
                            var fileName = Path.GetFileName(FileDataContent.FileName);
                            var UploadPath = Server.MapPath(serverPath);
                            Directory.CreateDirectory(UploadPath);
                            string filePath = Path.Combine(UploadPath, fileName);
                            if (System.IO.File.Exists(filePath))
                                System.IO.File.Delete(filePath);

                            using (var fileStream = System.IO.File.Create(filePath))
                            {
                                stream.CopyTo(fileStream);
                            }


                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (enableLog)
                    System.IO.File.AppendAllText(logFilename, "Error in UploadFile method :" + ex.Message + Environment.NewLine);
                throw ex;
            }


            // Write result.
            //System.IO.File.AppendAllText(@"C:\Data\5.7R4\Modified\Log.txt",  stopwatch.ElapsedMilliseconds.ToString() + Environment.NewLine); ;
            return new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("File uploaded.")
            };
        }

        private void UploadToDatabase(string filename)
        {
            try
            {
                string partToken = ".part_";
                string baseFileName = filename.Substring(0, filename.IndexOf(partToken));

                using (FileStream FS = new FileStream(baseFileName, FileMode.Open, FileAccess.Read))
                {

                    byte[] bytes = new byte[FS.Length];
                    int numBytesToRead = (int)FS.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {

                        int n = FS.Read(bytes, numBytesRead, numBytesToRead);

                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;


                    DataSet ds = new DataSet();

                    using (SqlConnection connection = new SqlConnection(@"Data Source=ABN-QNX-SQL-E01.dev.trizetto.com\SQL2017;Initial Catalog=PlanDocument_Penn; integrated security=true"))
                    {
                        connection.Open();

                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = connection;
                        cmd.CommandTimeout = 0;

                        //string commandText = "select top 1 * from qdocuments  order by lastupdate desc";
                        //cmd.CommandText = commandText;
                        //SqlDataAdapter ad = new SqlDataAdapter(cmd);
                        //ad.Fill(ds);

                        ////ZZXMMQAP19974  
                        //string qdocumentid = ds.Tables[0].Rows[0]["qdocumentid"].ToString();
                        //qdocumentid = "DEZXMM0" + (Convert.ToInt64(ds.Tables[0].Rows[0]["qdocumentid"].ToString().Replace("DEZXMM0", "")) + 1);

                        string qdocumentid = "DEZXMM044608";

                        string commandText = "delete from qdocuments where qdocumentid ='DEZXMM044608'";
                        cmd.CommandText = commandText;
                        cmd.CommandType = System.Data.CommandType.Text;

                        cmd.ExecuteNonQuery();

                         commandText = "INSERT INTO qdocuments (qdocumentid,qdocument, createid) values (@qdocumentid,@qdocument,@createid)";
                        cmd.CommandText = commandText;
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@qdocumentid", qdocumentid);
                        cmd.Parameters.AddWithValue("@createid", "avellaichamy");
                        cmd.Parameters.AddWithValue("@qdocument", bytes);

                        cmd.ExecuteNonQuery();

                        //commandText = "INSERT INTO qa_process_57..memodocuments (memoid,qdocumentid, docdescription,originalfilename,addedby,adddate) values (@memoid,@documentid,@docdescription,@originalfilename,@addedby,@adddate)";
                        //cmd.CommandText = commandText;
                        //cmd.CommandType = System.Data.CommandType.Text;
                        //cmd.Parameters.AddWithValue("@memoid", "ZZXMMQAP19963");
                        //cmd.Parameters.AddWithValue("@documentid", qdocumentid);
                        //cmd.Parameters.AddWithValue("@docdescription", "");
                        //cmd.Parameters.AddWithValue("@originalfilename", Path.GetFileName(baseFileName));
                        //cmd.Parameters.AddWithValue("@addedby", "avellaichamy");
                        //cmd.Parameters.AddWithValue("@adddate", DateTime.Now);

                        //cmd.ExecuteNonQuery();

                        connection.Close();
                    }


                }
            }
            catch (Exception ex)
            {
                if (enableLog)
                    System.IO.File.AppendAllText(logFilename, "Error in UploadToDatabase method :" + ex.Message + Environment.NewLine);
                throw ex;
            }
            finally
            {
                CleanWorkArea(filename);
            }

        }

        private void CleanWorkArea(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            var fileNameSplit = fileName.Split('_');
            if (fileNameSplit.Length > 0)
            {
                string Searchpattern = fileNameSplit[0] + "_" + fileNameSplit[1] + "*"; ;
                string[] FilesList = Directory.GetFiles(Path.GetDirectoryName(filePath), Searchpattern);
                foreach (string File in FilesList)
                {
                    if ((System.IO.File.Exists(File)))
                    {
                        System.IO.File.Delete(File);
                    }
                }
            }

        }

        [HttpGet]
        public void DownloadFile(string fileName, string documentId)
        {
            byte[] byteArray = null;
            using (SqlConnection connection = new SqlConnection(@"Data Source=ABN-QNX-SQL-E01.dev.trizetto.com\SQL2017;Initial Catalog=PlanDocument_Penn; integrated security=true"))
            {
                connection.Open();


                string sql = @"
                                SELECT * 
                                FROM qdocuments 
                                WHERE (qdocumentid = @qdocumentid)";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.Add("@qdocumentid", SqlDbType.NVarChar).Value = documentId;
                    using (SqlDataReader d = cmd.ExecuteReader())
                    {
                        if (d.Read())
                        {
                            byteArray = (byte[])d["qdocument"];

                        }

                    }
                }

                connection.Close();
            }
            if (byteArray != null)
            {
                Response.Clear();
                Response.BufferOutput = false;
                Response.ContentType = "APPLICATION/OCTET-STREAM";
                System.String disHeader = "Attachment; Filename=\"" + Server.UrlEncode(fileName) + "\"";
                Response.AppendHeader("Content-Disposition", disHeader);
                Response.AddHeader("Content-Length", byteArray.Length.ToString());
                Response.Flush();
                int blockSize = 10240; //multiple of 4
                using (BinaryWriter binWriter = new BinaryWriter(new MemoryStream()))
                {

                    // Write the data to the stream.

                    binWriter.Write(byteArray);

                    // Create the reader using the stream from the writer.
                    using (BinaryReader binReader =
                        new BinaryReader(binWriter.BaseStream))
                    {

                        // Set Position to the beginning of the stream.
                        binReader.BaseStream.Position = 0;
                        byte[] buffer = new byte[blockSize];
                        buffer = binReader.ReadBytes(blockSize);
                        while (buffer.Length > 0)
                        {
                            if (Response.IsClientConnected)
                            {
                                Response.BinaryWrite(buffer);
                                Response.Flush();
                                buffer = binReader.ReadBytes(blockSize);
                            }
                            else
                            {
                                break;
                            }
                        }

                    }
                }
                byteArray = null;
                Response.Flush();
                return;
            }

        }

        private long GetOriginalLengthInBytes(FileStream fs)
        {
            if (fs == null || fs.Length < 4)
            {
                return 0;
            }
            fs.Seek(fs.Length - 4, SeekOrigin.Begin);
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            string binaryString = System.Text.Encoding.Default.GetString(buffer);

            int paddingCount = binaryString.Substring(2, 2)
                                           .Count(c => c == '=');
            fs.Seek(0, SeekOrigin.Begin);
            return 3 * (fs.Length / 4) - paddingCount;
        }


        private void downloadFile(FileStream fs, string filename)
        {
            Response.Clear();
            Response.BufferOutput = false;
            Response.ContentType = "APPLICATION/OCTET-STREAM";
            System.String disHeader = "Attachment; Filename=\"" + Server.UrlEncode(filename) + "\"";
            Response.AppendHeader("Content-Disposition", disHeader);
            //var originalSize = GetOriginalLengthInBytes(fs);
            Response.AddHeader("Content-Length", fs.Length.ToString());
            Response.Flush();
            int blockSize = 10240; //multiple of 4
            for (long offset = 0; offset < fs.Length; offset += blockSize)
            {
                if (Response.IsClientConnected)
                {
                    if ((offset + blockSize) > fs.Length)
                        blockSize = (int)(fs.Length - offset);
                    byte[] buffer = new byte[blockSize];
                    fs.Read(buffer, 0, buffer.Length);

                    //string binarystring = System.Text.Encoding.Default.GetString(buffer);
                    //byte[] data = Convert.FromBase64String(binarystring);

                    Response.BinaryWrite(buffer);
                    Response.Flush();
                }
            }
            fs.Close();
            Response.Flush();
        }
    }

    public class document
    {
        public string qdocumentid { get; set; }
        public string fileName { get; set; }
    }
}