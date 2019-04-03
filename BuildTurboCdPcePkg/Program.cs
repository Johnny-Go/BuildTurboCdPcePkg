using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BuildTurboCdPcePkg
{
    class Program
    {
        static void Main(string[] args)
        {
            //make sure proper number of aruments were passed
            if (args.Length != 1)
            {
                Console.WriteLine("Invalid arguments, only argument is a directory containing the game files (.hcd, .ogg, .bin)");
                return;
            }
            var directory = args[0];

            //check if the given directory exists
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Given directory does not exist");
                return;
            }

            //check that the given directory contains all the necessessary files
            if (Directory.GetFiles(directory, "*.hcd").Length == 0
                || Directory.GetFiles(directory, "*.ogg").Length == 0
                || Directory.GetFiles(directory, "*.bin").Length == 0)
            {
                Console.WriteLine("Given directory does not contain .hcd, .ogg, or .bin files, please make sure all extracted game files are present");
                return;
            }

            //build the file to write
            var toWrite = new List<byte>();

            //add the pceconfig.bin file
            var hcdFile = Directory.GetFiles(directory, "*.hcd");
            if (hcdFile.Length != 1)
            {
                Console.WriteLine("Too many .hcd files present, there can be only one");
                return;
            }
            var hcdFileName = hcdFile[0].Split('\\')[1];
            var combinedName = $"{directory}/{hcdFileName}";
            toWrite.AddRange(BuildPceConfigData(combinedName));

            //add the hcd file
            byte[] hcdFileData = GetFileData(combinedName);
            toWrite.AddRange(BuildFileData(hcdFileData, combinedName));

            //loop over the files in the hcd and add them
            string hcdFileText = File.ReadAllText($"{directory}\\{hcdFileName}");
            var rows = hcdFileText.Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < rows.Length; i++)
            {
                var columns = rows[i].Split(',');
                var fileName = columns[2];

                combinedName = $"{directory}/{fileName}";
                var fileData = GetFileData(combinedName);
                toWrite.AddRange(BuildFileData(fileData, combinedName));
            }

            //write the file
            var arrayToWrite = toWrite.ToArray();
            using (var fs = new FileStream("pce.pkg", FileMode.Create, FileAccess.ReadWrite))
            {
                //write the file length data
                fs.Write(GetLengthAsByteArrayInReverseOrder(arrayToWrite.Length), 0, 4);
                fs.Write(arrayToWrite, 0, arrayToWrite.Length);
            }
        }

        private static List<byte> BuildPceConfigData(string fileName)
        {
            var pceConfigData = new List<byte>();
            var fileNameAsByteArray = Encoding.Default.GetBytes(fileName);
            var fileNameLength = (64 - fileNameAsByteArray.Length);

            //write the pceconfig data
            pceConfigData.AddRange(new byte[] { 160, 0, 0, 0 }); //"A0000000" (specifying the pceconfig data is 160 bytes long)
            pceConfigData.AddRange(new byte[] { 112, 99, 101, 99, 111, 110, 102, 105, 103, 46, 98, 105, 110, 0 }); //"706365636F6E6669672E62696E00" (pceconfig.bin) followed by null 0x00 byte
            //doesn't seem to work with all 0s, so I'm using the bytes from the Lords of Thunder pceconfig.bin
            pceConfigData.AddRange(new byte[] { 1, 0, 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }); //"01 00 00 80 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00"
            //write the 64 character rom name
            pceConfigData.AddRange(fileNameAsByteArray);
            pceConfigData.AddRange(Enumerable.Repeat<byte>((byte)0, fileNameLength));
            //write the 64 character rom name a second time
            pceConfigData.AddRange(fileNameAsByteArray);
            pceConfigData.AddRange(Enumerable.Repeat<byte>((byte)0, fileNameLength));

            return pceConfigData;
        }

        //read the file data as a byte array
        private static byte[] GetFileData(string fileName)
        {
            byte[] fileData;
            using (var data = new MemoryStream())
            {
                using (var file = File.OpenRead(fileName))
                {
                    file.CopyTo(data);
                    fileData = data.ToArray();
                }
            }
            return fileData;
        }

        //build file data in the format length in reverse byte order, file name, null byte, file data
        private static List<byte> BuildFileData(byte[] data, string fileName)
        {
            var fileData = new List<byte>();
            var fileNameAsByteArray = Encoding.Default.GetBytes(fileName);
            var fileDataLength = GetLengthAsByteArrayInReverseOrder(data.Length);

            fileData.AddRange(fileDataLength);
            fileData.AddRange(fileNameAsByteArray);
            fileData.Add((byte)0);
            fileData.AddRange(data);

            return fileData;
        }

        //get the length as a byte array in reverse order
        private static byte[] GetLengthAsByteArrayInReverseOrder(int length)
        {
            byte[] bytes = new byte[4];

            bytes[3] = (byte)(length >> 24);
            bytes[2] = (byte)(length >> 16);
            bytes[1] = (byte)(length >> 8);
            bytes[0] = (byte)length;

            return bytes;
        }
    }
}
