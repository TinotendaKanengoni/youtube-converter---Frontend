using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using FFMpegCore;
using youtube_converter.Model;

namespace youtube_converter.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ConverterController : ControllerBase
    {
        private readonly YoutubeClient _youtubeClient;

        public ConverterController()
        {
            _youtubeClient = new YoutubeClient();
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertVideo([FromBody] ConvertRequest request)
        {
            if (string.IsNullOrEmpty(request.YoutubeUrl))
                return BadRequest("Invalid input");

            try
            {
                var video = await _youtubeClient.Videos.GetAsync(request.YoutubeUrl);
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                var sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{sanitizedTitle}.{streamInfo.Container.Name}");

                await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempFile);

                var outputPath = Path.Combine(Path.GetTempPath(), $"{sanitizedTitle}.mp3");
                await FFMpegArguments
                    .FromFileInput(tempFile)
                    .OutputToFile(outputPath, true, options => options.WithAudioCodec("libmp3lame"))
                    .ProcessAsynchronously();

                System.IO.File.Delete(tempFile);

                var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
                var fileName = Path.GetFileName(outputPath);
                System.IO.File.Delete(outputPath);

                return File(fileBytes, "application/octet-stream", fileName);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }


    }

