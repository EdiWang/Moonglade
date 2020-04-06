// Created by Joshua Flanagan
// http://flimflan.com/blog
// May 2004
//
// You may freely use this code as you wish, I only ask that you retain my name in the source code

// Modified by Pavel Janda
// - added support for 32bpp bitmaps
// November 2006

using System;
using System.IO;

namespace Moonglade.Web.SiteIconGenerator.IconEncoder
{
	public struct Iconimage
	{
		/// <summary>
		/// icHeader: DIB format header
		/// </summary>
		public Bitmapinfoheader   Header;
		/// <summary>
		/// icColors: Color table
		/// </summary>
		public Rgbquad[]         Colors;
		/// <summary>
		/// icXOR: DIB bits for XOR mask
		/// </summary>
		public byte[]            Xor;
		/// <summary>
		/// icAND: DIB bits for AND mask
		/// </summary>
		public byte[]            And;

		public void Populate(BinaryReader br)
		{
			// read in the header
			Header.Populate(br);
			Colors = new Rgbquad[Header.BiClrUsed];
			// read in the color table
			for(var i=0; i<Header.BiClrUsed; ++i)
			{
				Colors[i].Populate(br);
			}
			// read in the XOR mask
			Xor = br.ReadBytes(NumBytesInXor());
			// read in the AND mask
			And = br.ReadBytes(NumBytesInAnd());
		}

		public void Save(BinaryWriter bw)
		{
			Header.Save(bw);
			for(var i=0; i<Colors.Length; i++)
				Colors[i].Save(bw);
			bw.Write(Xor);
			bw.Write(And);
		}

		#region byte count calculation functions
		public int NumBytesInXor()
		{
			// number of bytes per pixel depends on bitcount
			var bytesPerLine = Convert.ToInt32(Math.Ceiling(Header.BiWidth * Header.BiBitCount / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			var padding = bytesPerLine % 4;
			if (padding > 0)
				bytesPerLine += 4 - padding;
			return bytesPerLine * (Header.BiHeight >> 1);

		}
		public int NumBytesInAnd()
		{
			// each byte can hold 8 pixels (1bpp)
			// check for a remainder
			var bytesPerLine = Convert.ToInt32(Math.Ceiling(Header.BiWidth / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			var padding = bytesPerLine % 4;
			if (padding > 0)
				bytesPerLine += 4 - padding;
			return bytesPerLine * (Header.BiHeight >> 1);
		}
		#endregion
	}

	public struct Icondir
	{
		/// <summary>
		/// idReserved: Always 0
		/// </summary>
		public ushort			Reserved;   // Reserved
		/// <summary>
		/// idType: Resource type (Always 1 for icons)
		/// </summary>
		public ushort			ResourceType;
		/// <summary>
		/// idCount: Number of images in directory
		/// </summary>
		public ushort			EntryCount;
		/// <summary>
		/// idEntries: Directory entries for each image
		/// </summary>
		public Icondirentry[]	Entries;

		public void Save(BinaryWriter bw)
		{
			bw.Write(Reserved);
			bw.Write(ResourceType);
			bw.Write(EntryCount);
			for (var i=0; i<Entries.Length; ++i)
				Entries[i].Save(bw);
		}

		public void Populate(BinaryReader br)
		{
			Reserved = br.ReadUInt16();
			ResourceType = br.ReadUInt16();
			EntryCount = br.ReadUInt16();
			Entries = new Icondirentry[EntryCount];
			for (var i=0; i < Entries.Length; i++)
			{
				Entries[i].Populate(br);
			}
		}
	}

	public struct Icondirentry
	{
		/// <summary>
		/// bWidth: In pixels.  Must be 16, 32, or 64
		/// </summary>
		public byte	Width;
		/// <summary>
		/// bHeight: In pixels.  Must be 16, 32, or 64
		/// </summary>
		public byte	Height;
		/// <summary>
		/// bColorCount: Number of colors in image (0 if >=8bpp)
		/// </summary>
		public byte	ColorCount;
		/// <summary>
		/// bReserved: Must be zero
		/// </summary>
		public byte	Reserved;
		/// <summary>
		/// wPlanes: Number of color planes in the icon bitmap
		/// </summary>
		public ushort	Planes;
		/// <summary>
		/// wBitCount: Number of bits in each pixel of the icon.  Must be 1,4,8, or 24
		/// </summary>
		public ushort	BitCount;
		/// <summary>
		/// dwBytesInRes: Number of bytes in the resource
		/// </summary>
		public uint BytesInRes;
		/// <summary>
		/// dwImageOffset: Number of bytes from the beginning of the file to the image
		/// </summary>
		public uint ImageOffset;
		
		public void Save(BinaryWriter bw)
		{
			bw.Write(Width);
			bw.Write(Height);
			bw.Write(ColorCount);
			bw.Write(Reserved);
			bw.Write(Planes);
			bw.Write(BitCount);
			bw.Write(BytesInRes);
			bw.Write(ImageOffset);
		}
	
		public void Populate(BinaryReader br)
		{
			Width = br.ReadByte();
			Height = br.ReadByte();
			ColorCount = br.ReadByte();
			Reserved = br.ReadByte();
			Planes = br.ReadUInt16();
			BitCount = br.ReadUInt16();
			BytesInRes = br.ReadUInt32();
			ImageOffset = br.ReadUInt32();
		}
	}


	public struct Bitmapfileheader 
	{
		public ushort    Type;
		public uint   Size;
		public ushort    Reserved1;
		public ushort    Reserved2;
		public uint   OffBits;

		public void Populate(BinaryReader br)
		{
			Type = br.ReadUInt16();
			Size = br.ReadUInt32();
			Reserved1 = br.ReadUInt16();
			Reserved2 = br.ReadUInt16();
			OffBits = br.ReadUInt32();
		}

		public void Save(BinaryWriter bw)
		{
			bw.Write(Type);
			bw.Write(Size);
			bw.Write(Reserved1);
			bw.Write(Reserved2);
			bw.Write(OffBits);
		}

	}
	public struct Bitmapinfo 
	{
		public Bitmapinfoheader    InfoHeader;
		public Rgbquad[]             ColorMap;

		public void Populate(BinaryReader br)
		{
			InfoHeader.Populate(br);
			ColorMap = new Rgbquad[GetNumberOfColors()];
			// read in the color table
			for(var i=0; i<ColorMap.Length; ++i)
			{
				ColorMap[i].Populate(br);
			}
		}
		public void Save(BinaryWriter bw)
		{
			InfoHeader.Save(bw);
			for(var i=0; i<ColorMap.Length; i++)
				ColorMap[i].Save(bw);
		}

		private uint GetNumberOfColors() 
		{
			if (InfoHeader.BiClrUsed > 0)
			{
				// number of colors is specified
				return InfoHeader.BiClrUsed;
			}

            // number of colors is based on the bitcount
            return InfoHeader.BiBitCount switch
            {
                1 => 2,
                4 => 16,
                8 => 256,
                _ => 0
            };
        }
	}
	
	/// <summary>
	/// Describes the format of the bitmap image
	/// </summary>
	/// <remarks>
	/// BITMAPHEADERINFO struct
	/// referenced by http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnwui/html/msdn_icons.asp
	/// defined by http://www.whisqu.se/per/docs/graphics52.htm
	/// Only the following members are used: biSize, biWidth, biHeight, biPlanes, biBitCount, biSizeImage. All other members must be 0. The biHeight member specifies the combined height of the XOR and AND masks. The members of icHeader define the contents and sizes of the other elements of the ICONIMAGE structure in the same way that the BITMAPINFOHEADER structure defines a CF_DIB format DIB
	/// </remarks>
	public struct  Bitmapinfoheader
	{
		public const int Size = 40;
		public uint  BiSize;
		public int   BiWidth;
		/// <summary>
		/// Height of bitmap.  For icons, this is the height of XOR and AND masks together. Divide by 2 to get true height.
		/// </summary>
		public int   BiHeight;
		public ushort   BiPlanes;
		public ushort   BiBitCount;
		public uint  BiCompression;
		public uint  BiSizeImage;
		public int   BiXPelsPerMeter;
		public int   BiYPelsPerMeter;
		public uint  BiClrUsed;
		public uint  BiClrImportant;

		public void Save(BinaryWriter bw)
		{
			bw.Write(BiSize);
			bw.Write(BiWidth);
			bw.Write(BiHeight);
			bw.Write(BiPlanes);
			bw.Write(BiBitCount);
			bw.Write(BiCompression);
			bw.Write(BiSizeImage);
			bw.Write(BiXPelsPerMeter);
			bw.Write(BiYPelsPerMeter);
			bw.Write(BiClrUsed);
			bw.Write(BiClrImportant);
		}

		public void Populate(BinaryReader br)
		{
			BiSize = br.ReadUInt32();
			BiWidth = br.ReadInt32();
			BiHeight = br.ReadInt32();
			BiPlanes = br.ReadUInt16();
			BiBitCount = br.ReadUInt16();
			BiCompression = br.ReadUInt32();
			BiSizeImage = br.ReadUInt32();
			BiXPelsPerMeter = br.ReadInt32();
			BiYPelsPerMeter = br.ReadInt32();
			BiClrUsed = br.ReadUInt32();
			BiClrImportant = br.ReadUInt32();
		}
	} 

	// RGBQUAD structure changed by Pavel Janda on 14/11/2006
	public struct Rgbquad
	{
		public const int Size = 4;
		public byte Blue;
		public byte Green;
		public byte Red;
		public byte Alpha;

		public Rgbquad(byte[] bgr) : this(bgr[0], bgr[1], bgr[2]){}

		public Rgbquad(byte blue, byte green, byte red)
		{
			Blue = blue;
			Green = green;
			Red = red;
			Alpha = 0;
		}

		public Rgbquad(byte blue, byte green, byte red, byte alpha) 
		{
			Blue = blue;
			Green = green;
			Red = red;
			Alpha = alpha;
		}

		public void Save(BinaryWriter bw)
		{
			bw.BaseStream.WriteByte(Blue);
			bw.BaseStream.WriteByte(Green);
			bw.BaseStream.WriteByte(Red);
			bw.BaseStream.WriteByte(Alpha);
		}

		public void Populate(BinaryReader br)
		{
			Blue = br.ReadByte();
			Green = br.ReadByte();
			Red = br.ReadByte();
			Alpha = br.ReadByte();
		}
	}

}
