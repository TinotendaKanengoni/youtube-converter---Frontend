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
    [HttpPost("convert-to-mp4")]
    public async Task<IActionResult> ConvertToMp4([FromBody] ConvertRequest request)
    {
      if (string.IsNullOrEmpty(request.YoutubeUrl))
        return BadRequest("Invalid input");

      try
      {
        // Fetch video info
        var video = await _youtubeClient.Videos.GetAsync(request.YoutubeUrl);
        var sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

        // Get the stream manifest
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

        // Try to get muxed stream first (video + audio combined)
        var muxedStreams = streamManifest.GetMuxedStreams();
        if (muxedStreams.Any())
        {
          var streamInfo = muxedStreams.GetWithHighestVideoQuality();
          var tempMuxedFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{sanitizedTitle}.{streamInfo.Container.Name}");

          await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempMuxedFile);

          // If it's already MP4, just return it
          if (streamInfo.Container.Name.Equals("mp4", StringComparison.OrdinalIgnoreCase))
          {
            var muxedFileBytes = await System.IO.File.ReadAllBytesAsync(tempMuxedFile);
            var muxedFileName = $"{sanitizedTitle}.mp4";
            System.IO.File.Delete(tempMuxedFile);
            return File(muxedFileBytes, "video/mp4", muxedFileName);
          }

          // Convert to MP4 if needed
          var convertedOutputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{sanitizedTitle}.mp4");
          await FFMpegArguments
              .FromFileInput(tempMuxedFile)
              .OutputToFile(convertedOutputFile, true)
              .ProcessAsynchronously();

          System.IO.File.Delete(tempMuxedFile);
          var convertedFileBytes = await System.IO.File.ReadAllBytesAsync(convertedOutputFile);
          var convertedFileName = $"{sanitizedTitle}.mp4";
          System.IO.File.Delete(convertedOutputFile);
          return File(convertedFileBytes, "video/mp4", convertedFileName);
        }

        // If no muxed streams, combine separate video and audio streams
        var audioStreams = streamManifest.GetAudioOnlyStreams();
        var videoStreams = streamManifest.GetVideoOnlyStreams();

        if (!audioStreams.Any() || !videoStreams.Any())
          return BadRequest("No suitable streams available for conversion.");

        var audioStream = audioStreams.GetWithHighestBitrate();
        var videoStream = videoStreams.GetWithHighestVideoQuality();

        var tempAudioFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_audio.{audioStream.Container.Name}");
        var tempVideoFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_video.{videoStream.Container.Name}");

        // Fix for ValueTask conversion error - use Task.WhenAll with proper Task objects
        await Task.WhenAll(
            _youtubeClient.Videos.Streams.DownloadAsync(audioStream, tempAudioFile).AsTask(),
            _youtubeClient.Videos.Streams.DownloadAsync(videoStream, tempVideoFile).AsTask()
        );

        var combinedOutputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{sanitizedTitle}.mp4");

        // Improved FFmpeg combination
        await FFMpegArguments
            .FromFileInput(tempVideoFile)
            .AddFileInput(tempAudioFile)
            .OutputToFile(combinedOutputFile, true, options => options
                .WithVideoCodec("libx264")
                .WithAudioCodec("aac")
                .WithFastStart())
            .ProcessAsynchronously();

        // Clean up temp files
        System.IO.File.Delete(tempAudioFile);
        System.IO.File.Delete(tempVideoFile);

        var finalFileBytes = await System.IO.File.ReadAllBytesAsync(combinedOutputFile);
        var finalFileName = $"{sanitizedTitle}.mp4";
        System.IO.File.Delete(combinedOutputFile);
        return File(finalFileBytes, "video/mp4", finalFileName);
      }
      catch (Exception ex)
      {
        return StatusCode(500, $"An error occurred: {ex.Message}\n{ex.StackTrace}");
      }
    }
  }


    }

