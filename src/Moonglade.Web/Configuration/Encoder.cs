using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Moonglade.Web.Configuration;

public static class Encoder
{
    public static HtmlEncoder MoongladeHtmlEncoder => HtmlEncoder.Create(
        UnicodeRanges.BasicLatin,
        UnicodeRanges.CjkCompatibility,
        UnicodeRanges.CjkCompatibilityForms,
        UnicodeRanges.CjkCompatibilityIdeographs,
        UnicodeRanges.CjkRadicalsSupplement,
        UnicodeRanges.CjkStrokes,
        UnicodeRanges.CjkUnifiedIdeographs,
        UnicodeRanges.CjkUnifiedIdeographsExtensionA,
        UnicodeRanges.CjkSymbolsandPunctuation,
        UnicodeRanges.EnclosedCjkLettersandMonths,
        UnicodeRanges.MiscellaneousSymbols,
        UnicodeRanges.HalfwidthandFullwidthForms
    );
}