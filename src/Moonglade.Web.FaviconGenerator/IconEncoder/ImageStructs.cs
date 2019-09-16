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
using BYTE = System.Byte;
using WORD = System.UInt16;
using DWORD = System.UInt32;
using LONG = System.Int32;

namespace Moonglade.Web.FaviconGenerator.IconEncoder
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
		public BYTE[]            Xor;
		/// <summary>
		/// icAND: DIB bits for AND mask
		/// </summary>
		public BYTE[]            And;

		public void Populate(BinaryReader br)
		{
			// read in the header
			this.Header.Populate(br);
			this.Colors = new Rgbquad[Header.BiClrUsed];
			// read in the color table
			for(var i=0; i<this.Header.BiClrUsed; ++i)
			{
				this.Colors[i].Populate(br);
			}
			// read in the XOR mask
			this.Xor = br.ReadBytes(NumBytesInXor());
			// read in the AND mask
			this.And = br.ReadBytes(NumBytesInAnd());
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
			var bytesPerLine = Convert.ToInt32(Math.Ceiling((Header.BiWidth * Header.BiBitCount) / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			var padding = (bytesPerLine % 4);
			if (padding > 0)
				bytesPerLine += (4 - padding);
			return bytesPerLine * (Header.BiHeight >> 1);

		}
		public int NumBytesInAnd()
		{
			// each byte can hold 8 pixels (1bpp)
			// check for a remainder
			var bytesPerLine = Convert.ToInt32(Math.Ceiling(Header.BiWidth / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			var padding = (bytesPerLine % 4);
			if (padding > 0)
				bytesPerLine += (4 - padding);
			return bytesPerLine * (Header.BiHeight >> 1);
		}
		#endregion
	}

	public struct Icondir
	{
		/// <summary>
		/// idReserved: Always 0
		/// </summary>
		public WORD			Reserved;   // Reserved
		/// <summary>
		/// idType: Resource type (Always 1 for icons)
		/// </summary>
		public WORD			ResourceType;
		/// <summary>
		/// idCount: Number of images in directory
		/// </summary>
		public WORD			EntryCount;
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
			Entries = new Icondirentry[this.EntryCount];
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
		public BYTE	Width;
		/// <summary>
		/// bHeight: In pixels.  Must be 16, 32, or 64
		/// </summary>
		public BYTE	Height;
		/// <summary>
		/// bColorCount: Number of colors in image (0 if >=8bpp)
		/// </summary>
		public BYTE	ColorCount;
		/// <summary>
		/// bReserved: Must be zero
		/// </summary>
		public BYTE	Reserved;
		/// <summary>
		/// wPlanes: Number of color planes in the icon bitmap
		/// </summary>
		public WORD	Planes;
		/// <summary>
		/// wBitCount: Number of bits in each pixel of the icon.  Must be 1,4,8, or 24
		/// </summary>
		public WORD	BitCount;
		/// <summary>
		/// dwBytesInRes: Number of bytes in the resource
		/// </summary>
		public DWORD BytesInRes;
		/// <summary>
		/// dwImageOffset: Number of bytes from the beginning of the file to the image
		/// </summary>
		public DWORD ImageOffset;
		
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
		public WORD    Type;
		public DWORD   Size;
		public WORD    Reserved1;
		public WORD    Reserved2;
		public DWORD   OffBits;

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
			else
			{
				// number of colors is based on the bitcount
				switch(InfoHeader.BiBitCount)
				{
					case 1:
						return 2;
					case 4:
						return 16;
					case 8:
						return 256;
					default:
						return 0;
				}
			}
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
		public DWORD  BiSize;
		public LONG   BiWidth;
		/// <summary>
		/// Height of bitmap.  For icons, this is the height of XOR and AND masks together. Divide by 2 to get true height.
		/// </summary>
		public LONG   BiHeight;
		public WORD   BiPlanes;
		public WORD   BiBitCount;
		public DWORD  BiCompression;
		public DWORD  BiSizeImage;
		public LONG   BiXPelsPerMeter;
		public LONG   BiYPelsPerMeter;
		public DWORD  BiClrUsed;
		public DWORD  BiClrImportant;

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
		public BYTE Blue;
		public BYTE Green;
		public BYTE Red;
		public BYTE Alpha;

		public Rgbquad(BYTE[] bgr) : this(bgr[0], bgr[1], bgr[2]){}

		public Rgbquad(BYTE blue, BYTE green, BYTE red)
		{
			this.Blue = blue;
			this.Green = green;
			this.Red = red;
			this.Alpha = 0;
		}

		public Rgbquad(BYTE blue, BYTE green, BYTE red, BYTE alpha) 
		{
			this.Blue = blue;
			this.Green = green;
			this.Red = red;
			this.Alpha = alpha;
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
