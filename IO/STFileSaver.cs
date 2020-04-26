﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Toolbox.Core.IO
{
    public class STFileSaver
    {
        public class SaveLog
        {
            public string SaveTime = "";
        }

        /// <summary>
        /// Saves the <see cref="IFileFormat"/> as a file from the given <param name="FileName">
        /// </summary>
        /// <param name="IFileFormat">The format instance of the file being saved</param>
        /// <param name="FileName">The name of the file</param>
        /// <param name="Alignment">The Alignment used for compression. Used for Yaz0 compression type. </param>
        /// <returns></returns>
        public static SaveLog SaveFileFormat(IFileFormat fileFormat, string fileName)
        {
            SaveLog log = new SaveLog();
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            if (fileFormat.FileInfo.Compression != null)
            {
                Stream mem = new MemoryStream();
                fileFormat.Save(mem);
                mem = CompressFile(mem, fileFormat);
                File.WriteAllBytes(fileName, mem.ToArray());
            }
            else if (fileFormat.FileInfo.KeepOpen && File.Exists(fileName))
            {
                string savedPath = Path.GetDirectoryName(fileName);
                string tempPath = Path.Combine(savedPath, "tempST.bin");

                //Save a temporary file first to not disturb the opened file
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileFormat.Save(fileStream);
                    if (fileFormat is IDisposable)
                        ((IDisposable)fileFormat).Dispose();

                    //After saving is done remove the existing file
                    File.Delete(fileName);

                    //Now move and rename our temp file to the new file path
                    File.Move(tempPath, fileName);

                    fileFormat.Load(File.OpenRead(fileName));
                }
            }
            else
            {
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                    fileFormat.Save(fileStream);
                }
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            log.SaveTime = string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Minutes, ts.Seconds, ts.Milliseconds);

            return log;
        }

        /// <summary>
        /// Saves the <see cref="IFileFormat"/> into the given stream.
        /// </summary>
        /// <param name="fileFormat"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static SaveLog SaveFileFormat(IFileFormat fileFormat, Stream stream)
        {
            SaveLog log = new SaveLog();

            Stream mem = new MemoryStream();
            fileFormat.Save(mem);

            if (fileFormat.FileInfo.Compression != null) {
                mem = CompressFile(mem, fileFormat);
            }

            mem.CopyTo(stream);
            return log;
        }

        static Stream CompressFile(Stream mem, IFileFormat fileFormat)
        {
            var compressionFormat = fileFormat.FileInfo.Compression;
            var compressedStream = compressionFormat.Compress(mem);
            //Update the compression size
            fileFormat.FileInfo.CompressedSize = (uint)compressedStream.Length;
            return compressedStream;
        }
    }
}
