using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace mmria.server.utils;

public sealed class cFolderCompressor
{

    public cFolderCompressor() {}

    public void Compress(string outPathname, string password, string folderName) 
    {
        Compress(new FileInfo(outPathname), password, new DirectoryInfo(folderName));
    }

    public void Compress(FileInfo outFile, string password, DirectoryInfo folder) 
    {
        if (outFile == null)
        {
            throw new ArgumentNullException(nameof(outFile));
        }

        if (folder == null || !folder.Exists)
        {
            throw new DirectoryNotFoundException(folder?.FullName ?? string.Empty);
        }

        using(FileStream fsOut = outFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
        using(ZipOutputStream zipStream = new ZipOutputStream(fsOut))
        {
            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

            zipStream.Password = password;	// optional. Null is the same as not setting. Required if using AES.
            var rootPath = folder.FullName;
            int folderOffset = rootPath.Length +
                (rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                 rootPath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? 0 : 1);

            CompressFolder(folder, zipStream, folderOffset);

            zipStream.IsStreamOwner = true;	// Makes the Close also Close the underlying stream
            zipStream.Close();
        }
    }

    // Recurses down the folder structure
    //
    private void CompressFolder(DirectoryInfo directory, ZipOutputStream zipStream, int folderOffset) 
    {
        foreach (FileInfo fi in directory.GetFiles()) 
        {

            string entryName = fi.FullName.Substring(folderOffset); // Makes the name in zip based on the folder
            entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
            ZipEntry newEntry = new ZipEntry(entryName);
            newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

            // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
            // A secret phrase on the ZipOutputStream is required if using AES.
            //   newEntry.AESKeySize = 256;

            // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
            // you need to do one of the following: Specify UseZip64.Off, or set the Size.
            // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
            // but the zip will be in Zip64 format which not all utilities can understand.
            //   zipStream.UseZip64 = UseZip64.Off;
            newEntry.Size = fi.Length;
            //try
            //{
                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[ ] buffer = new byte[4096];
                using (FileStream streamReader = fi.OpenRead()) 
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            /*}
            catch(Exception ex)
            {
                System.Console.WriteLine(ex);
            }*/
        }
        
        foreach (DirectoryInfo folder in directory.GetDirectories()) 
        {
            CompressFolder(folder, zipStream, folderOffset);
        }
    }
}


