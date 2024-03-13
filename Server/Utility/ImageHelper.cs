using System;
using System.IO;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public static class ImageHelper
{
    public static async Task<string> GetBase64ImageStringAsync(string thumbnailUrl, int width, int height)
    {
        using (var httpClient = new HttpClient())
        {
            var imageBytes = await httpClient.GetByteArrayAsync(thumbnailUrl);

            using (var memoryStream = new MemoryStream(imageBytes))
            {
                using (var image = await Image.LoadAsync(memoryStream))
                {
                    var resizedImage = image.Clone(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(width, height),
                        Mode = ResizeMode.Max
                    }));

                    using (var resizedMemoryStream = new MemoryStream())
                    {
                        await resizedImage.SaveAsPngAsync(resizedMemoryStream);
                        var resizedImageBytes = resizedMemoryStream.ToArray();

                        return "data:image/png;base64," + Convert.ToBase64String(resizedImageBytes);
                    }
                }
            }
        }
    }
}