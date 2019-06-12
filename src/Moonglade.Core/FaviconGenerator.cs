using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Moonglade.Core
{
    public interface IFaviconGenerator
    {
        void GenerateIcons(string sourceImagePath, string directory);
    }

    public class MoongladeFaviconGenerator : IFaviconGenerator
    {
        public void GenerateIcons(string sourceImagePath, string directory)
        {
            if (!File.Exists(sourceImagePath))
            {
                throw new FileNotFoundException("Can not find source image for generating favicons.", sourceImagePath);
            }

            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("Can not find target directory to save generated favicons");
            }

            var ext = Path.GetExtension(sourceImagePath);
            if (ext.ToLower() != ".png")
            {
                throw new FormatException("Source file is not an PNG image.");
            }

            void WriteImageFiles(string prefix, int[] sizes)
            {
                foreach (var size in sizes)
                {
                    var destFileName = $"{prefix}{size}x{size}.png";
                    var destPath = Path.Combine(directory, destFileName);
                    Utils.ResizePngImage(sourceImagePath, size, size, destPath);
                }
            }

            // How to OOP design these? May be don't need over design?
            var dic = new Dictionary<string, int[]>
            {
                { "android-icon-", new[] { 36, 48, 72, 96, 144, 192 } },
                { "favicon", new[] { 16, 32, 96 } },
                { "apple-icon-", new[] { 57, 60, 72, 76, 114, 120, 144, 152, 180 } }
            };

            foreach (var (key, value) in dic)
            {
                WriteImageFiles(key, value);
            }

            // Here comes the extras, Apple, always so special!
            Utils.ResizePngImage(sourceImagePath, 192, 192, Path.Combine(directory, "apple-icon.png"));
            Utils.ResizePngImage(sourceImagePath, 192, 192, Path.Combine(directory, "apple-icon-precomposed.png"));
            // And one more...
            // How to generate .ico file in .NET Core?
        }
    }
}
