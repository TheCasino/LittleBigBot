using System.Net;
using System.Threading.Tasks;
using Discord;
using LittleBigBot.Checks;
using LittleBigBot.Entities;
using Newtonsoft.Json.Linq;
using Qmmands;

namespace LittleBigBot.Modules
{
    [Name("Pictures")]
    [Description("Commands that let you access pictures from various websites.")]
    public class PicturesModule : LittleBigBotModuleBase
    {
        private const string BowsetteApi = "https://lewd.bowsette.pictures/api/request";
        private const string CatApi = "http://aws.random.cat/meow";

        public WebClient HttpApi { get; set; }

        [Command("Bowsette")]
        [Description("Pictures from lewd.bowsette.pictures.")]
        [Remarks("This website has not been filtered, so there could be NSFW content.")]
        [RequireNsfw]
        public async Task Command_GetBowsettePictureAsync()
        {
            var url = JToken.Parse(await HttpApi.DownloadStringTaskAsync(BowsetteApi)).Value<string>("url");

            await ReplyAsync(string.Empty, false, new EmbedBuilder()
                .WithTitle("Bowsette!")
                .WithColor(LittleBigBot.DefaultEmbedColour)
                .WithImageUrl(url)
                .Build());
        }

        [Command("Cat", "Meow")]
        [Description("Meow.")]
        public async Task Command_GetCatPictureAsync()
        {
            var url = JToken.Parse(await HttpApi.DownloadStringTaskAsync(CatApi)).Value<string>("file");

            await ReplyAsync(string.Empty, false, new EmbedBuilder()
                .WithTitle("Meow.")
                .WithColor(LittleBigBot.DefaultEmbedColour)
                .WithImageUrl(url)
                .Build());
        }
    }
}