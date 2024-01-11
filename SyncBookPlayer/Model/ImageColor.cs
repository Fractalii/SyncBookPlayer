/*using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace SyncBookPlayer.Model;
public class ImageColor
{

    public static string AverageFromPath(string FileName, string defaultColor = "#000000FF")
    {
        try
        {
            using Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(FileName);
            image.Mutate(delegate (IImageProcessingContext x)
            {
                x.Resize(new ResizeOptions
                {
                    Sampler = KnownResamplers.NearestNeighbor,
                    Size = new SixLabors.ImageSharp.Size(100, 0)
                }).Quantize(new OctreeQuantizer(new QuantizerOptions
                {
                    Dither = null,
                    MaxColors = 1
                }));
            });
            return image[0, 0].ToHex().Replace("FF", "");
        }
        catch (Exception)
        {
            return defaultColor;
        }
    }
}*/
