using PdfSharp.Fonts;
using System.IO;
using System.Reflection;

public sealed class DejaVuFontResolver : IFontResolver
{
    static byte[] Load(string endsWith)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream(); s.CopyTo(ms); return ms.ToArray();
    }
    static readonly Lazy<byte[]> Reg = new(() => Load("resources.DejaVuSans.ttf"));
    static readonly Lazy<byte[]> Bold = new(() => Load("resources.DejaVuSans-Bold.ttf"));

    public FontResolverInfo ResolveTypeface(string family, bool isBold, bool isItalic)
    {
        if (family.Equals("DejaVu Sans", StringComparison.OrdinalIgnoreCase) || family.Equals("DejaVuSans", StringComparison.OrdinalIgnoreCase)) return new FontResolverInfo(isBold ? "DejaVuSans-Bold" : "DejaVuSans");
        return new FontResolverInfo(isBold ? "DejaVuSans-Bold" : "DejaVuSans");
    }

    public byte[] GetFont(string faceName) =>
        faceName == "DejaVuSans-Bold" ? Bold.Value : Reg.Value;
}