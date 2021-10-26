using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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


            //Open it as a binary reader

            FileInfo mainFileInfo = new FileInfo(filename);

            string newfolder = filename.Replace(".pkg", "");
            newfolder = newfolder.Replace(@".\packs\", @".\unpacked\");
            newfolder += @"\";
          //  string folds = Path.GetFileNameWithoutExtension(filename);

            Console.WriteLine("{0} - {1} - ", filename, newfolder);

            List<FileData> FDL = new List<FileData>();

            using (BinaryReader BR = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                int header = BR.ReadInt32();
                int datasize = BR.ReadInt32();
                int filecount = BR.ReadInt32();

                Console.WriteLine("Total Package Size: " + (header + datasize + 4)); // The total size of the file is header + data + 4
                Console.WriteLine("Header 4 Bytes: " + header);
                Console.WriteLine("FileCount*16: " + (filecount * 16));
                Console.WriteLine("Data Size: " + datasize);



                //Header should be the total size of filecount * 16 + 12 (Total Header Size)


                //So to make a custom package that POSSIBLY will work we need to.

                //Extract original package using the hash as file names
                //We can work out filenames using MrCheeze work in future
                //Make modifications to files smaller\larger should not matter.

                //Create a brand new package
                //Get a count of the number of files in directory (filecount)
                //create the first 4 bytes (filecount * 16 + 12)
                //

                for (int i = 0; i < filecount; i++)
                {
                    long hash = BR.ReadInt64();
                    int start = BR.ReadInt32();
                    int end = BR.ReadInt32();
                    FileData fd = new FileData(hash, start, end);

                    FDL.Add(fd);

                }

                long totsize = 0;

                long lastend = 0;


                foreach (FileData fd in FDL)
                {
                    //    Console.WriteLine("Hash: {0:X}, Star: {1:X}, End: {2:X}, Align: {3:X}", fd.hash, fd.start, fd.end, (fd.start - lastend));



                    if (strArr == null && strArr.Count == 0)
                    {
                        strArr = LoadStringList();
                    }

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

                    using (BinaryWriter BW = new BinaryWriter(File.Open(newfolder + thefilename, FileMode.Create)))
                    {
                        BW.Write(filedata);
                        Console.WriteLine("{0} - {1:X} {2:X} {3:X}", thefilename, fd.hash, fd.start, fd.end);
                    }


                    lastend = fd.end;
                }
            }

            ////Get the total file size which should match datasize
            //long totalSize = 0;
            //long tsize = 0;
            //string[] a = Directory.GetFiles("./unpacked");
            //Console.WriteLine("READFileCount*16: " + (a.Length * 16)); //This looks good.
            //foreach (string aa in a)
            //{

            //    using (FileStream FS = File.Open(aa, FileMode.Open))
            //    {
            //        tsize += FS.Length;
            //    }
            //    FileInfo FI = new FileInfo(aa);
            //    //  Console.WriteLine(FI.Name + " " + FI.Length);
            //    totalSize += FI.Length;
            //}
            //// Console.WriteLine(datasize);
            //Console.WriteLine("READDataSize: " + totalSize);
            //Console.WriteLine("READDataSizesss: " + tsize);




        }
        public static void RePack(string dir)
        {

            string filedir = dir.Replace(".pkg", "");
            filedir = filedir.Replace(@".\packs\", @".\unpacked\");

            string newpkgdir = dir.Replace(@".\packs\", @".\repacked\");

           //   Console.WriteLine("{0} - {1}", newpkgdir, filedir);
            //  string folds = Path.GetFileNameWithoutExtension(filename);
            Directory.CreateDirectory(Path.GetDirectoryName(newpkgdir));
            using (BinaryWriter BR = new BinaryWriter(File.Open(newpkgdir, FileMode.Create)))
            {
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



                    //If start  is not alligned
                    int aligned = RoundUp(dataend);


                    int pad = aligned - dataend;

                    if (strArr == null || strArr.Count == 0)
                    {
                        LoadStringList();
                    }

                    long hashn;



                    string realname = file.Replace(filedir + "\\", "");
                    realname = realname.Replace("\\", "/");

                    ulong checksum = dreadcrc.crc64(realname);

                 //   Console.WriteLine("{0} - {1} - {2:X}", filedir, realname, checksum);


      
                    if (strArr.ContainsKey(checksum))
                    {
                        hashn = (long)checksum;
                    }
                    else
                    {
                        hashn = long.Parse(FI.Name, System.Globalization.NumberStyles.HexNumber);
                    }


                    FileData FD = new FileData(hashn, datastart, dataend, pad, realname);




                    FDL.Add(FD);
                    BR.Write(hashn);
                    BR.Write(datastart);
                    BR.Write(dataend);
                    //The next data start padded to 8
                    datastart = dataend + pad;

                }

                BR.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });

                foreach (FileData FileD in FDL)
                {




                    //we need to read each file, grab its length
                    FileInfo FI = new FileInfo(filedir + "/" + FileD.name);


                    int filelen = (int)FI.Length;
                    //End position of data is where it started + length





                    BR.Write(File.ReadAllBytes(FI.FullName));
                    for (int i = 0; i < FileD.pad; i++)
                    {
                        BR.Write((byte)0x00);
                    }

                    Console.WriteLine("Packing {0} - Start:{1:X}, End:{2:X}, Align:{3} Name:{4}", FileD.hash.ToString("X"), FileD.start, FileD.end, FileD.pad, FileD.name);
                    datasize += filelen + FileD.pad;

                }
                Console.WriteLine("");
                Console.WriteLine("{0}Bytes of data written",datasize);
                Console.WriteLine("");

                BR.BaseStream.Seek(0x4, SeekOrigin.Begin);
                BR.Write(datasize);
            }

        }

        static int Menu()
        {
            Console.WriteLine("Metroid Dread PKG Tool by Kriogenic");
            Console.WriteLine("Thanks to MrCheeze, Stuckpixel");

            Console.WriteLine("Usage instructions:");
            Console.WriteLine("\tPlace your pkg files into the packs directory");
            Console.WriteLine("\tThe pkgs MUST remain in the packs folder for both unpacking and repacking.");
            Console.WriteLine("\tChoose to unpack or repack pkg files.");
            Console.WriteLine(" ");
            Console.WriteLine("1. Unpack");
            Console.WriteLine("2. Repack");
            Console.WriteLine("3. Exit");
            Console.WriteLine("Enter choice: ");
            string input = Console.ReadLine();
            int number;
            if (!Int32.TryParse(input, out number))
            {
                return -1;
            }

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
                ulong crc = dreadcrc.crc64(arr[0]);
                checksumDict.Add(crc, arr[0]);
            }
            return checksumDict;
        }
        static void Main(string[] args)
        {

            strArr = LoadStringList();

            int input = Menu();
            while (input != 1 && input != 2)
            {
                Console.Clear();
                Console.WriteLine("Invalid selection, try again");
                input = Menu();
            }

            

            string[] allpkgfiles = Directory.GetFiles(@".\packs", "*.pkg", SearchOption.AllDirectories);
            switch (input)
            {
                case 1:
                    foreach(string pkgfile in allpkgfiles)
                    {
                       // Console.WriteLine(pkgfile);
                        UnPack(pkgfile);
                    }
                    break;
                case 2:
                    foreach (string pkgfile in allpkgfiles)
                    {
                        RePack(pkgfile);
                    }
                    break;
            }
        }
    }
}
