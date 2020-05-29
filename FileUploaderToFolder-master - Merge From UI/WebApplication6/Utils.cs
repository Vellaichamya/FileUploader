using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Shared
{
    public class Utils
    {
        public string FileName { get; set; }
        public string TempFolder { get; set; }
        public int MaxFileSizeMB { get; set; }
        public List<String> FileParts { get; set; }

        public Utils()
        {
            FileParts = new List<string>();
        }

        public bool MergeFile(string FileName)
        {
            bool rslt = false;
            bool enableLog = false;
            string logFilename = @"C:\Data\FileUpload_Modified\Log.txt";
            if (enableLog)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(logFilename));
                if (!System.IO.File.Exists(logFilename))
                {
                    using (System.IO.File.Create(logFilename))
                    {

                    }

                }
            }
            try
            {
                string partToken = ".part_";
                string baseFileName = FileName.Substring(0, FileName.IndexOf(partToken));
                string trailingTokens = FileName.Substring(FileName.IndexOf(partToken) + partToken.Length);
                int FileIndex = 0;
                int FileCount = 0;
                int.TryParse(trailingTokens.Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
                int.TryParse(trailingTokens.Substring(trailingTokens.IndexOf(".") + 1), out FileCount);
                string Searchpattern = Path.GetFileName(baseFileName) + partToken + "*";
                string[] FilesList = Directory.GetFiles(Path.GetDirectoryName(FileName), Searchpattern);
                if (FilesList.Count() == FileCount)
                {
                    if (enableLog)
                        System.IO.File.AppendAllText(logFilename, "Inside If Loop" + Environment.NewLine);
                    if (File.Exists(baseFileName))
                        File.Delete(baseFileName);
                    List<SortedFile> MergeList = new List<SortedFile>();
                    foreach (string File in FilesList)
                    {
                        SortedFile sFile = new SortedFile();
                        sFile.FileName = File;
                        trailingTokens = File.Substring(File.IndexOf(partToken) + partToken.Length);
                        int.TryParse(trailingTokens.Substring(0, trailingTokens.IndexOf(".")), out FileIndex);
                        sFile.FileOrder = FileIndex;
                        MergeList.Add(sFile);
                    }
                    var MergeOrder = MergeList.OrderBy(s => s.FileOrder).ToList();
                    if (enableLog)
                        System.IO.File.AppendAllText(logFilename, "File Merge start" + Environment.NewLine);
                    using (FileStream FS = new FileStream(baseFileName, FileMode.Create))
                    {
                        foreach (var chunk in MergeOrder)
                        {
                            if (enableLog)
                                System.IO.File.AppendAllText(logFilename, "Part Merge started" + Environment.NewLine);
                            using (FileStream fileChunk = new FileStream(chunk.FileName, FileMode.Open))
                            {
                                fileChunk.CopyTo(FS);
                            }
                            if (enableLog)
                                System.IO.File.AppendAllText(logFilename, "Part Merge complted" + Environment.NewLine);
                            if ((System.IO.File.Exists(chunk.FileName)))
                            {
                                if (enableLog)
                                    System.IO.File.AppendAllText(logFilename, "Delete part file start" + Environment.NewLine);
                                System.IO.File.Delete(chunk.FileName);
                                if (enableLog)
                                    System.IO.File.AppendAllText(logFilename, "Delete part file Completed" + Environment.NewLine);
                            }

                        }
                    }
                    rslt = true;
                }


            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logFilename, "Error in MeregFile :" + ex.Message + Environment.NewLine);
                throw ex;
            }
            return rslt;

        }


    }

    public struct SortedFile
    {
        public int FileOrder { get; set; }
        public String FileName { get; set; }
    }



}



