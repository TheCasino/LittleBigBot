using System;
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
        public async Task<CommandResult> Command_GetBowsettePictureAsync()
        {
            var url = JToken.Parse(await HttpApi.DownloadStringTaskAsync(BowsetteApi)).Value<string>("url");

            return Ok(embed =>
            {
                embed.WithTitle("Here's your Bowsette, pervert.")
                    .WithColor(LittleBigBot.DefaultEmbedColour)
                    .WithImageUrl(url);
            });
        }

        [Command("Cat", "Meow")]
        [Description("Meow.")]
        public async Task<CommandResult> Command_GetCatPictureAsync()
        {
            var url = JToken.Parse(await HttpApi.DownloadStringTaskAsync(CatApi)).Value<string>("file");

            return Ok(embed =>
            {
                embed.WithTitle("Meow~!")
                    .WithColor(LittleBigBot.DefaultEmbedColour)
                    .WithImageUrl(url);
            });
        }
    }
}