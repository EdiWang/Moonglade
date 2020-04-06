// Created by Joshua Flanagan
// http://flimflan.com/blog
// May 2004
//
// You may freely use this code as you wish, I only ask that you retain my name in the source code

// Modified by Pavel Janda
// - added support for 32bpp bitmaps
// November 2006

using System;
using System.Collections;
using System.Drawing;
using System.IO;

namespace Moonglade.Web.SiteIconGenerator.IconEncoder
{
    /// <summary>
    /// Provides methods for converting between the bitmap and icon formats
    /// </summary>
    public class Converter
    {
        private Converter() { }
        public static Icon BitmapToIcon(Bitmap b)
        {
            var ico = BitmapToIconHolder(b);
            Icon newIcon;
            using (var bw = new BinaryWriter(new MemoryStream()))
            {
                ico.Save(bw);
                bw.BaseStream.Position = 0;
                newIcon = new Icon(bw.BaseStream);
            }
            return newIcon;
        }

        public static IconHolder BitmapToIconHolder(Bitmap b)
        {
            var bmp = new BitmapHolder();
            using (var stream = new MemoryStream())
            {
                b.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = 0;
                bmp.Open(stream);
            }
            return BitmapToIconHolder(bmp);
        }

        public static IconHolder BitmapToIconHolder(BitmapHolder bmp)
        {
            var mapColors = bmp.Info.InfoHeader.BiBitCount <= 24;
            var maximumColors = 1 << bmp.Info.InfoHeader.BiBitCount;
            //Hashtable uniqueColors = new Hashtable(maximumColors);
            // actual colors is probably nowhere near maximum, so dont try to initialize the hashtable
            var uniqueColors = new Hashtable();

            var sourcePosition = 0;
            var numPixels = bmp.Info.InfoHeader.BiHeight * bmp.Info.InfoHeader.BiWidth;
            var indexedImage = new byte[numPixels];
            byte colorIndex;

            if (mapColors)
            {
                for (var i = 0; i < indexedImage.Length; i++)
                {
                    //TODO: currently assumes source bitmap is 24bit color
                    //read 3 bytes, convert to color
                    var pixel = new byte[3];
                    Array.Copy(bmp.ImageData, sourcePosition, pixel, 0, 3);
                    sourcePosition += 3;

                    var color = new Rgbquad(pixel);
                    if (uniqueColors.Contains(color))
                    {
                        colorIndex = Convert.ToByte(uniqueColors[color]);
                    }
                    else
                    {
                        if (uniqueColors.Count > byte.MaxValue)
                        {
                            throw new NotSupportedException(
                                $"The source image contains more than {byte.MaxValue} colors.");
                        }
                        colorIndex = Convert.ToByte(uniqueColors.Count);
                        uniqueColors.Add(color, colorIndex);
                    }
                    // store pixel as an index into the color table
                    indexedImage[i] = colorIndex;
                }
            }
            else
            {
                // added by Pavel Janda on 14/11/2006
                if (bmp.Info.InfoHeader.BiBitCount == 32)
                {
                    for (var i = 0; i < indexedImage.Length; i++)
                    {
                        //TODO: currently assumes source bitmap is 32bit color with alpha set to zero
                        //ignore first byte, read another 3 bytes, convert to color
                        var pixel = new byte[4];
                        Array.Copy(bmp.ImageData, sourcePosition, pixel, 0, 4);
                        sourcePosition += 4;

                        var color = new Rgbquad(pixel[0], pixel[1], pixel[2], pixel[3]);
                        if (uniqueColors.Contains(color))
                        {
                            colorIndex = Convert.ToByte(uniqueColors[color]);
                        }
                        else
                        {
                            if (uniqueColors.Count > byte.MaxValue)
                            {
                                throw new NotSupportedException(
                                    $"The source image contains more than {byte.MaxValue} colors.");
                            }
                            colorIndex = Convert.ToByte(uniqueColors.Count);
                            uniqueColors.Add(color, colorIndex);
                        }
                        // store pixel as an index into the color table
                        indexedImage[i] = colorIndex;
                    }
                    // end of addition
                }
                else
                {
                    //TODO: implement converting an indexed bitmap
                    throw new NotImplementedException("Unable to convert indexed bitmaps.");
                }
            }

            var bitCount = GetBitCount(uniqueColors.Count);
            // *** Build Icon ***
            var ico = new IconHolder { IconDirectory = { Entries = new Icondirentry[1] } };
            //TODO: is it really safe to assume the bitmap width/height are bytes?
            ico.IconDirectory.Entries[0].Width = (byte)bmp.Info.InfoHeader.BiWidth;
            ico.IconDirectory.Entries[0].Height = (byte)bmp.Info.InfoHeader.BiHeight;
            ico.IconDirectory.Entries[0].BitCount = bitCount; // maybe 0?
            ico.IconDirectory.Entries[0].ColorCount = uniqueColors.Count > byte.MaxValue ? (byte)0 : (byte)uniqueColors.Count;
            //HACK: safe to assume that the first imageoffset is always 22
            ico.IconDirectory.Entries[0].ImageOffset = 22;
            ico.IconDirectory.Entries[0].Planes = 0;
            ico.IconImages[0].Header.BiBitCount = bitCount;
            ico.IconImages[0].Header.BiWidth = ico.IconDirectory.Entries[0].Width;
            // height is doubled in header, to account for XOR and AND
            ico.IconImages[0].Header.BiHeight = ico.IconDirectory.Entries[0].Height << 1;
            ico.IconImages[0].Xor = new byte[ico.IconImages[0].NumBytesInXor()];
            ico.IconImages[0].And = new byte[ico.IconImages[0].NumBytesInAnd()];
            ico.IconImages[0].Header.BiSize = 40; // always
            ico.IconImages[0].Header.BiSizeImage = (uint)ico.IconImages[0].Xor.Length;
            ico.IconImages[0].Header.BiPlanes = 1;
            ico.IconImages[0].Colors = BuildColorTable(uniqueColors, bitCount);
            //BytesInRes = biSize + colors * 4 + XOR + AND
            ico.IconDirectory.Entries[0].BytesInRes = (uint)(ico.IconImages[0].Header.BiSize
                + ico.IconImages[0].Colors.Length * 4
                + ico.IconImages[0].Xor.Length
                + ico.IconImages[0].And.Length);

            // copy image data
            var bytePosXor = 0;
            var bytePosAnd = 0;
            byte transparentIndex = 0;
            transparentIndex = indexedImage[0];
            //initialize AND
            ico.IconImages[0].And[0] = byte.MaxValue;

            int pixelsPerByte;
            int[] shiftCounts;

            switch (bitCount)
            {
                case 1:
                    pixelsPerByte = 8;
                    shiftCounts = new[] { 7, 6, 5, 4, 3, 2, 1, 0 };
                    break;
                case 4:
                    pixelsPerByte = 2;
                    shiftCounts = new[] { 4, 0 };
                    break;
                case 8:
                    pixelsPerByte = 1;
                    shiftCounts = new[] { 0 };
                    break;
                default:
                    throw new NotSupportedException("Bits per pixel must be 1, 4, or 8");
            }
            var bytesPerRow = ico.IconDirectory.Entries[0].Width / pixelsPerByte;
            var padBytes = bytesPerRow % 4;
            if (padBytes > 0)
                padBytes = 4 - padBytes;

            sourcePosition = 0;
            for (var row = 0; row < ico.IconDirectory.Entries[0].Height; ++row)
            {
                for (var rowByte = 0; rowByte < bytesPerRow; ++rowByte)
                {
                    byte currentByte = 0;
                    for (var pixel = 0; pixel < pixelsPerByte; ++pixel)
                    {
                        var index = indexedImage[sourcePosition++];
                        var shiftedIndex = (byte)(index << shiftCounts[pixel]);
                        currentByte |= shiftedIndex;
                    }
                    ico.IconImages[0].Xor[bytePosXor] = currentByte;
                    ++bytePosXor;
                }
                // make sure each scan line ends on a long boundary
                bytePosXor += padBytes;
            }

            for (var i = 0; i < indexedImage.Length; i++)
            {
                var index = indexedImage[i];
                var bitPosAnd = 128 >> (i % 8);
                if (index != transparentIndex)
                    ico.IconImages[0].And[bytePosAnd] ^= (byte)bitPosAnd;
                if (bitPosAnd != 1) continue;
                // need to start another byte for next pixel
                if (bytePosAnd % 2 == 1)
                {
                    //TODO: fix assumption that icon is 16px wide
                    //skip some bytes so that scanline ends on a long barrier
                    bytePosAnd += 3;
                }
                else
                {
                    bytePosAnd += 1;
                }
                if (bytePosAnd < ico.IconImages[0].And.Length)
                    ico.IconImages[0].And[bytePosAnd] = byte.MaxValue;
            }
            return ico;
        }

        private static ushort GetBitCount(int uniqueColorCount)
        {
            if (uniqueColorCount <= 2)
            {
                return 1;
            }
            if (uniqueColorCount <= 16)
            {
                return 4;
            }
            return uniqueColorCount <= 256 ? (ushort) 8 : (ushort) 24;
        }

        private static Rgbquad[] BuildColorTable(Hashtable colors, ushort bpp)
        {
            //RGBQUAD[] colorTable = new RGBQUAD[colors.Count];
            //HACK: it looks like the color array needs to be the max size based on bitcount
            var numColors = 1 << bpp;
            var colorTable = new Rgbquad[numColors];
            foreach (Rgbquad color in colors.Keys)
            {
                var colorIndex = Convert.ToInt32(colors[color]);
                colorTable[colorIndex] = color;
            }
            return colorTable;
        }

        //		public static BitmapHolder IconToBitmap(IconHolder ico)
        //		{
        //			//TODO: implement
        //			return new BitmapHolder();
        //		}
    }
}
