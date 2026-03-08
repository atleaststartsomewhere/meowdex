using SkiaSharp;
using Svg.Skia;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: IconGen <input.svg> <output.ico>");
    return 1;
}

var inputSvg = args[0];
var outputIco = args[1];
const int size = 256;

if (!File.Exists(inputSvg))
{
    Console.Error.WriteLine($"SVG not found: {inputSvg}");
    return 2;
}

var svg = new SKSvg();
using (var stream = File.OpenRead(inputSvg))
{
    svg.Load(stream);
}

if (svg.Picture is null)
{
    Console.Error.WriteLine("Unable to load SVG picture.");
    return 3;
}

var bounds = svg.Picture.CullRect;
if (bounds.Width <= 0 || bounds.Height <= 0)
{
    Console.Error.WriteLine("SVG has invalid bounds.");
    return 4;
}

var scale = Math.Min(size / bounds.Width, size / bounds.Height);
var tx = (size - (bounds.Width * scale)) / 2f - (bounds.Left * scale);
var ty = (size - (bounds.Height * scale)) / 2f - (bounds.Top * scale);

using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
var canvas = surface.Canvas;
canvas.Clear(SKColors.Transparent);
canvas.Translate(tx, ty);
canvas.Scale(scale);
canvas.DrawPicture(svg.Picture);
canvas.Flush();

using var image = surface.Snapshot();
using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
var pngBytes = encoded.ToArray();

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputIco))!);
using var fs = File.Create(outputIco);
using var bw = new BinaryWriter(fs);

// ICONDIR
bw.Write((ushort)0); // reserved
bw.Write((ushort)1); // type: icon
bw.Write((ushort)1); // image count

// ICONDIRENTRY
bw.Write((byte)0);   // width: 0 means 256
bw.Write((byte)0);   // height: 0 means 256
bw.Write((byte)0);   // color count
bw.Write((byte)0);   // reserved
bw.Write((ushort)1); // planes
bw.Write((ushort)32);// bit count
bw.Write((uint)pngBytes.Length);
bw.Write((uint)22);  // offset to image data

bw.Write(pngBytes);

Console.WriteLine($"Wrote icon: {outputIco}");
return 0;
