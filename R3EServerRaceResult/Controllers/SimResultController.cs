using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    public class SimResultController(IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;

        [HttpGet("ResultUrl")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        public IActionResult SimResultUrl()
        {
            List<string> urls = [];

            var files = Directory.GetFiles(fileStorageAppSettings.MountedVolumePath, $"{fileStorageAppSettings.ResultFileName}.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.StartsWith(fileStorageAppSettings.MountedVolumePath))
                {
                    string webPath = file[fileStorageAppSettings.MountedVolumePath.Length..];
                    string url = $"simresults.net/remote?results={HttpUtility.UrlEncode($"{settings.WebServer}{webPath}")}";
                    urls.Add(url);
                }
            }

            return Ok(urls);
        }
    }
}
