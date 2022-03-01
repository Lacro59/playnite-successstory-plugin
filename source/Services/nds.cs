using System;
using System.IO;
using System.Linq;

namespace SuccessStory.Services
{
    public class NDS
    {
        private ROMHeader nds = new ROMHeader();

        private readonly byte[] header;
        private readonly byte[] icon;
        private readonly byte[] arm9;
        private readonly byte[] arm7;


        public NDS(string file)
        {
            nds = new ROMHeader();
            BinaryReader br = new BinaryReader(File.OpenRead(file));

            nds.gameTitle = br.ReadChars(12);
            nds.gameCode = br.ReadChars(4);
            nds.makerCode = br.ReadChars(2);
            nds.unitCode = br.ReadByte();
            nds.encryptionSeed = br.ReadByte();
            nds.tamaño = (UInt32)Math.Pow(2, 17 + br.ReadByte());
            nds.reserved = br.ReadBytes(9);
            nds.ROMversion = br.ReadByte();
            nds.internalFlags = br.ReadByte();
            nds.ARM9romOffset = br.ReadUInt32();
            nds.ARM9entryAddress = br.ReadUInt32();
            nds.ARM9ramAddress = br.ReadUInt32();
            nds.ARM9size = br.ReadUInt32();
            nds.ARM7romOffset = br.ReadUInt32();
            nds.ARM7entryAddress = br.ReadUInt32();
            nds.ARM7ramAddress = br.ReadUInt32();
            nds.ARM7size = br.ReadUInt32();
            nds.fileNameTableOffset = br.ReadUInt32();
            nds.fileNameTableSize = br.ReadUInt32();
            nds.FAToffset = br.ReadUInt32();
            nds.FATsize = br.ReadUInt32();
            nds.ARM9overlayOffset = br.ReadUInt32();
            nds.ARM9overlaySize = br.ReadUInt32();
            nds.ARM7overlayOffset = br.ReadUInt32();
            nds.ARM7overlaySize = br.ReadUInt32();
            nds.flagsRead = br.ReadUInt32();
            nds.flagsInit = br.ReadUInt32();
            nds.bannerOffset = br.ReadUInt32();
            nds.secureCRC16 = br.ReadUInt16();
            nds.ROMtimeout = br.ReadUInt16();
            nds.ARM9autoload = br.ReadUInt32();
            nds.ARM7autoload = br.ReadUInt32();
            nds.secureDisable = br.ReadUInt64();
            nds.ROMsize = br.ReadUInt32();
            nds.headerSize = br.ReadUInt32();
            nds.reserved2 = br.ReadBytes(56);
            br.BaseStream.Seek(156, SeekOrigin.Current); //nds.logo = br.ReadBytes(156); Logo de Nintendo utilizado para comprobaciones
            nds.logoCRC16 = br.ReadUInt16();
            nds.headerCRC16 = br.ReadUInt16();
            nds.debug_romOffset = br.ReadUInt32();
            nds.debug_size = br.ReadUInt32();
            nds.debug_ramAddress = br.ReadUInt32();
            nds.reserved3 = br.ReadUInt32();


            br.BaseStream.Position = 0;
            header = br.ReadBytes(352);

            br.BaseStream.Position = nds.bannerOffset;
            icon = br.ReadBytes(2560);

            br.BaseStream.Position = nds.ARM9romOffset;
            arm9 = br.ReadBytes((int)nds.ARM9size);

            br.BaseStream.Position = nds.ARM7romOffset;
            arm7 = br.ReadBytes((int)nds.ARM7size);

            br.Close();
        }


        public byte[] getByteToHash()
        {
            byte[] byteReturn = header.Concat(arm9).Concat(arm7).Concat(icon).ToArray();
            return byteReturn;
        }

    }

    public struct ROMHeader
    {
        public char[] gameTitle;
        public char[] gameCode;
        public char[] makerCode;
        public byte unitCode;
        public byte encryptionSeed;
        public UInt32 tamaño;
        public byte[] reserved;
        public byte ROMversion;
        public byte internalFlags;
        public UInt32 ARM9romOffset;
        public UInt32 ARM9entryAddress;
        public UInt32 ARM9ramAddress;
        public UInt32 ARM9size;
        public UInt32 ARM7romOffset;
        public UInt32 ARM7entryAddress;
        public UInt32 ARM7ramAddress;
        public UInt32 ARM7size;
        public UInt32 fileNameTableOffset;
        public UInt32 fileNameTableSize;
        public UInt32 FAToffset;            // File Allocation Table offset
        public UInt32 FATsize;              // File Allocation Table size
        public UInt32 ARM9overlayOffset;      // ARM9 overlay file offset
        public UInt32 ARM9overlaySize;
        public UInt32 ARM7overlayOffset;
        public UInt32 ARM7overlaySize;
        public UInt32 flagsRead;            // Control register flags for read
        public UInt32 flagsInit;            // Control register flags for init
        public UInt32 bannerOffset;           // Icon + titles offset
        public UInt16 secureCRC16;          // Secure area CRC16 0x4000 - 0x7FFF
        public UInt16 ROMtimeout;
        public UInt32 ARM9autoload;
        public UInt32 ARM7autoload;
        public UInt64 secureDisable;        // Magic number for unencrypted mode
        public UInt32 ROMsize;
        public UInt32 headerSize;
        public byte[] reserved2;            // 56 bytes
                                            //public byte[] logo;               // 156 bytes de un logo de nintendo usado para comprobaciones de seguridad
        public UInt16 logoCRC16;
        public UInt16 headerCRC16;
        public bool secureCRC;
        public bool logoCRC;
        public bool headerCRC;
        public UInt32 debug_romOffset;      // only if debug
        public UInt32 debug_size;           // version with
        public UInt32 debug_ramAddress;     // 0 = none, SIO and 8 MB
        public UInt32 reserved3;            // Zero filled transfered and stored but not used
                                            //public byte[] reserved4;          // 0x90 bytes => Zero filled transfered but not stored in RAM
    }
}
