using System;
using System.Collections.Generic;
using System.IO;

namespace DreadPKG
{
    struct FileData
    {
        public long hash;
        public int start;
        public int end;
        public int pad;
        public string name;
        public FileData(long ahash, int astart, int aend, int apad = 0, string aname = null)
        {
            hash = ahash;
            start = astart;
            end = aend;
            pad = apad;
            name = aname;
        }
    }
    class Program
    {
        static Dictionary<ulong, string> strArr = new Dictionary<ulong, string>();
        public static int RoundUp(int x)
        {
            return ((x + 7) & (-8));
        }

        public static void UnPack(string filename)
        {
            string newfolder = filename.Replace(".pkg", "");
            newfolder = newfolder.Replace(@".\packs\", @".\unpacked\");
            newfolder += @"\";
            //  string folds = Path.GetFileNameWithoutExtension(filename);

            Console.WriteLine("Package: {0}, Unpack Directory: {1} - ", filename, newfolder);

            List<FileData> FDL = new List<FileData>();

            //Open it as a binary reader
            using BinaryReader BR = new BinaryReader(File.Open(filename, FileMode.Open));
            int header = BR.ReadInt32(); //Header = filecount * 16 + 12 (12 to account for first 12 bytes)
            int datasize = BR.ReadInt32(); //DataSize = the total amount of data stored in the file
            int filecount = BR.ReadInt32(); //Filecount = The amount of files contained in the package

            Console.WriteLine("Total Package Size: " + (header + datasize + 4)); // The plus 4 is to 8 align the header. 16*x + 12 will always be 4 bytes unaligned
            Console.WriteLine("Header 4 Bytes: " + header);
            Console.WriteLine("Files in pkg: " + filecount);
            Console.WriteLine("Data Size: " + datasize);

            for (int i = 0; i < filecount; i++)
            {
                long hash = BR.ReadInt64();
                int start = BR.ReadInt32();
                int end = BR.ReadInt32();
                FileData fd = new FileData(hash, start, end);

                FDL.Add(fd);
            }


            long totsize = 0;
            foreach (FileData fd in FDL)
            {
                if (strArr == null && strArr.Count == 0)
                    strArr = LoadStringList();

                string thefilename;

                if (strArr.ContainsKey((ulong)fd.hash))
                {
                    thefilename = strArr[(ulong)fd.hash];
                    Directory.CreateDirectory(Path.GetDirectoryName(newfolder + thefilename));
                }
                else
                {
                    Directory.CreateDirectory(newfolder);
                    thefilename = fd.hash.ToString("X");
                }

                BR.BaseStream.Seek(fd.start, SeekOrigin.Begin);
                int filesize = fd.end - fd.start;
                byte[] filedata = BR.ReadBytes(filesize);
                totsize += filesize;

                using BinaryWriter BW = new BinaryWriter(File.Open(newfolder + thefilename, FileMode.Create));
                BW.Write(filedata);
                Console.WriteLine("{0} - {1:X} {2:X} {3:X}", thefilename, fd.hash, fd.start, fd.end);
            }
        }

        public static void RePack(string dir)
        {

            string filedir = dir.Replace(".pkg", "");
            filedir = filedir.Replace(@".\packs\", @".\unpacked\");

            string newpkgdir = dir.Replace(@".\packs\", @".\repacked\");

            Directory.CreateDirectory(Path.GetDirectoryName(newpkgdir));
            using BinaryWriter BR = new BinaryWriter(File.Open(newpkgdir, FileMode.Create));
            //First we need to create all the header information
            //header (4 bytes) - filecount*16 + 12
            string[] fileNames = Directory.GetFiles(filedir, "*", SearchOption.AllDirectories);
            int header = fileNames.Length * 16 + 12;
            BR.Write(header);
            //datasize (4 bytes) - size of all the data in file
            int datasize = 0;
            BR.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            //filecount - the total amount of files in the package
            int filecount = fileNames.Length;
            BR.Write(filecount);

            //File entries are added next - filename, startpos, endpos
            int datastart = RoundUp(0xC + (filecount * 16));

            List<FileData> FDL = new List<FileData>();
            foreach (string file in fileNames)
            {
                //we need to read each file, grab its length
                FileInfo FI = new FileInfo(file);
                int filelen = (int)FI.Length;
                //End position of data is where it started + length
                int dataend = datastart + filelen;
                //If align and get padded bytes
                int aligned = RoundUp(dataend);
                int pad = aligned - dataend;

                if (strArr == null || strArr.Count == 0)
                    LoadStringList();

                long hashn;

                string realname = file.Replace(filedir + "\\", "");
                realname = realname.Replace("\\", "/");

                ulong checksum = Dreadcrc.Crc64(realname);

                if (strArr.ContainsKey(checksum))
                    hashn = (long)checksum;
                else
                    if (!long.TryParse(FI.Name, System.Globalization.NumberStyles.HexNumber, null, out hashn))
                        hashn = long.Parse(Dreadcrc.Crc64(realname).ToString("X"), System.Globalization.NumberStyles.HexNumber);
                 
                FileData FD = new FileData(hashn, datastart, dataend, pad, realname);

                FDL.Add(FD);
                BR.Write(hashn);
                BR.Write(datastart);
                BR.Write(dataend);
                //The next data start padded to 8
                datastart = dataend + pad;
            }

            //After the header, datasize, filecount and all file entries have been added... We align data start to the nearest 8
            //Which is always an extra 4 bytes (EG with 5 files... 5*16 = 80 + 12 = 92.... 92 MOD 8 = 4. This always proves true.
            BR.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });

            foreach (FileData FileD in FDL)
            {
                //we need to read each file, grab its length
                FileInfo FI = new FileInfo(filedir + "/" + FileD.name);
                int filelen = (int)FI.Length;
                BR.Write(File.ReadAllBytes(FI.FullName));
                for (int i = 0; i < FileD.pad; i++)
                    BR.Write((byte)0x00);

                Console.WriteLine("Packing {0} - Start:{1:X}, End:{2:X}, Align:{3} Name:{4}", FileD.hash.ToString("X"), FileD.start, FileD.end, FileD.pad, FileD.name);
                datasize += filelen + FileD.pad;
            }

            Console.WriteLine("");
            Console.WriteLine("{0}Bytes of data written", datasize);
            Console.WriteLine("");

            BR.BaseStream.Seek(0x4, SeekOrigin.Begin);
            BR.Write(datasize);
        }

        static int Menu()
        {
            Console.WriteLine("Metroid Dread PKG Tool by Kriogenic");
            Console.WriteLine("Thanks to MrCheeze, Stuckpixel");
            Console.WriteLine(" ");
            Console.WriteLine("Usage instructions:");
            Console.WriteLine("\tPlace your pkg files into the packs directory");
            Console.WriteLine("\tThe pkgs MUST remain in the packs folder for both unpacking and repacking.");
            Console.WriteLine("\tChoose to unpack or repack pkg files.");
            Console.WriteLine(" ");
            Console.WriteLine("1. Unpack");
            Console.WriteLine("2. Repack");
            Console.WriteLine("3. Exit");
            Console.WriteLine("Enter choice: ");
            return ParseInput(Console.ReadLine());
        }

        static int ParseInput(string input)
        {
            if (!int.TryParse(input, out int number))
                return -1;

            return number;
        }

        static Dictionary<ulong, string> LoadStringList()
        {
            Dictionary<ulong, string> checksumDict = new Dictionary<ulong, string>();
            string test = ResourcePKG.confirmed_strings;
            string[] allLines = test.Split("\r\n");

            foreach (string line in allLines)
            {
                string[] arr = line.Split('\t');
                ulong crc = Dreadcrc.Crc64(arr[0]);
                checksumDict.Add(crc, arr[0]);
            }
            return checksumDict;
        }
        static void Main(string[] args)
        {
            strArr = LoadStringList();
            int input;
            if (args.Length == 1)
                input = ParseInput(args[0]);
            else if (args.Length > 1)
            {
                Console.WriteLine("Too many arguments, try again");
                input = Menu();
            }
            else
                input = Menu();

            while (input != 1 && input != 2 && input != 3)
            {
                Console.Clear();
                Console.WriteLine("Invalid selection, try again");
                input = Menu();
            }

            string[] allpkgfiles = Directory.GetFiles(@".\packs", "*.pkg", SearchOption.AllDirectories);
            switch (input)
            {
                case 1:
                    foreach (string pkgfile in allpkgfiles)
                        UnPack(pkgfile);
                    Console.WriteLine("Unpacking operation complete!");
                    break;
                case 2:
                    foreach (string pkgfile in allpkgfiles)
                        RePack(pkgfile);
                    Console.WriteLine("Repacking operation complete!");
                    break;
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
