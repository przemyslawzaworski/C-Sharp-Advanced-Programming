// csc Zip.cs -reference:System.IO.Compression.FileSystem.dll
using System.IO.Compression;

class Program
{
    static void Main()
    {
        ZipFile.CreateFromDirectory("E:\\Zip", "E:\\Archieve.zip", CompressionLevel.Optimal, false);
    }
}