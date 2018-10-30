using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using LittleBigBot.Attributes;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using LittleBigBot.Services;
using Qmmands;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

/**
 * Commands in this module
 *
 * - Track
 * - Reauthorise
 * - Album
 */
namespace LittleBigBot.Modules
{
    [Name("Spotify")]
    [Description(
        "Commands that relate to Spotify, a digital music service that gives you access to millions of songs.")]
    public class SpotifyModule : LittleBigBotModuleBase
    {
        public SpotifyService Spotify { get; set; }

        [Command("Track", "Song", "GetSong", "GetTrack")]
        [Description("Searches the Spotify database for a song.")]
        [RunMode(RunMode.Parallel)]
        public async Task<BaseResult> Command_TrackAsync(
            [Name("Track Name")]
            [Description("The track to search for.")]
            [Remainder]
            [DefaultValueDescription("The track that you're currently listening to.")]
            string trackQuery = null)
        {
            FullTrack track;
            if (trackQuery != null)
            {
                var tracks = await Spotify.RequestAsync(api => api.SearchItemsAsync(trackQuery, SearchType.Track));

                if (tracks.Error != null)
                    return BadRequest($"Spotify returned error code {tracks.Error.Status}: {tracks.Error.Message}");

                track = tracks.Tracks.Items.FirstOrDefault();
            }
            else
            {
                if (!(Context.Invoker.Activity is SpotifyGame spot))
                    return BadRequest("You didn't supply a track, and you're not currently listening to anything!");

                track = await Spotify.RequestAsync(a => a.GetTrackAsync(spot.TrackId));
            }

            if (track == null) return NotFound("Cannot find a track by that name.");

            var embed = new EmbedBuilder
            {
                Color = LittleBigBot.DefaultEmbedColour,
                Author = new EmbedAuthorBuilder
                {
                    Name = track.Artists.Select(a => a.Name).Humanize(),
                    IconUrl = track.Album.Images.FirstOrDefault()?.Url,
                    Url = track.Artists.FirstOrDefault().GetArtistUrl()
                },
                Title = track.Name,
                ThumbnailUrl = track.Album.Images.FirstOrDefault()?.Url
            };

            var length = TimeSpan.FromMilliseconds(track.DurationMs);

            embed.AddField("Length", $"{length.Minutes} minutes, {length.Seconds} seconds", true);
            embed.AddField("Release Date",
                DateTime.TryParse(track.Album.ReleaseDate, out var dt) ? dt.ToString("D") : track.Album.ReleaseDate,
                true);
            embed.AddField("Album", UrlHelper.CreateMarkdownUrl(track.Album.Name, track.Album.GetAlbumUrl()), true);
            embed.AddField("Is Explicit", track.Explicit ? "Yes" : "No", true);

            embed.AddField("\u200B", UrlHelper.CreateMarkdownUrl("Click to listen!", track.GetTrackUrl()));

            return Ok(embed);
        }

        [Command("Spotify", "Listening", "Music", "SpotifyInfo", "MusicInfo", "Playing")]
        [Description("Retrieves information about a user's Spotify status, if any.")]
        [Thumbnail("https://i.imgur.com/d7HQlA9.png")]
        [RunMode(RunMode.Parallel)]
        public async Task<BaseResult> Command_GetSpotifyDataAsync(
            [Name("User")]
            [Description("The user to get Spotify data for.")]
            [DefaultValueDescription("The user who invoked this command.")]
            [Remainder]
            SocketUser user = null)
        {
            user = user ?? Context.Invoker;

            if (user.Activity == null || !(user.Activity is SpotifyGame spotify))
                return BadRequest("User is not listening to anything~!");

            var track = await Spotify.RequestAsync(a => a.GetTrackAsync(spotify.TrackId));
            var embed = new EmbedBuilder
            {
                Color = LittleBigBot.DefaultEmbedColour,
                Title = "Listening to Spotify",
                Url = spotify.TrackUrl,
                Description =
                    $"{user} is listening to \"{track.Name}\" by {track.Artists.Select(a => a.Name).Humanize()}",
                ThumbnailUrl = spotify.AlbumArtUrl,
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Track",
                        Value = UrlHelper.CreateMarkdownUrl(spotify.TrackTitle, spotify.TrackUrl),
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Album",
                        Value = UrlHelper.CreateMarkdownUrl(spotify.AlbumTitle, track.Album.GetAlbumUrl()),
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = $"Artist{(track.Artists.Count == 1 ? "" : "s")}",
                        Value = track.Artists.Select(a => UrlHelper.CreateMarkdownUrl(a.Name, a.GetArtistUrl()))
                            .Humanize(),
                        IsInline = true
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Track ID: {spotify.TrackId}"
                },
                Timestamp = DateTime.Now
            };

            if (spotify.Duration != null) embed.AddField("Duration", spotify.Duration.Value.Humanize(2), true);

            return Ok(embed);
        }

        [Command("Album")]
        [Description("Searches the Spotify database for an album.")]
        [RunMode(RunMode.Parallel)]
        public async Task<BaseResult> Command_SearchAlbumAsync(
            [Name("Album Name")]
            [Description("The album name to search for.")]
            [Remainder]
            [DefaultValueDescription("The album of the track you're currently listening to.")]
            string albumQuery = null)
        {
            FullAlbum album;
            if (albumQuery == null)
            {
                if (!(Context.Invoker.Activity is SpotifyGame spot))
                    return BadRequest(
                        "You didn't supply an album name, and you're not currently listening to anything!");

                album = await Spotify.RequestAsync(async ab =>
                    await ab.GetAlbumAsync((await Spotify.RequestAsync(a => a.GetTrackAsync(spot.TrackId))).Album.Id));
            }
            else
            {
                var result = await Spotify.RequestAsync(a => a.SearchItemsAsync(albumQuery, SearchType.Album));

                if (result.Error != null)
                    return BadRequest($"Spotify returned error code {result.Error.Status}: {result.Error.Message}");

                var sa0 = result.Albums.Items.FirstOrDefault();

                if (sa0 == null) return BadRequest("Cannot find album by that name.");

                album = await Spotify.RequestAsync(a => a.GetAlbumAsync(sa0.Id));
            }

            var embed = new EmbedBuilder
            {
                Color = LittleBigBot.DefaultEmbedColour,
                Author = new EmbedAuthorBuilder
                {
                    Name = album.Artists.Select(a => a.Name).Humanize(),
                    IconUrl = album.Images.FirstOrDefault()?.Url,
                    Url = album.Artists.FirstOrDefault().GetArtistUrl()
                },
                Title = album.Name,
                ThumbnailUrl = album.Images.FirstOrDefault()?.Url,
                Footer = new EmbedFooterBuilder
                {
                    Text = string.Join("\n",
                        album.Copyrights.Distinct().Select(a =>
                            $"[{(a.Type == "C" ? "Copyright" : a.Type == "P" ? "Recording Copyright" : a.Type == "T" ? "Trademark" : a.Type == "R" ? "Registered Trademark" : a.Type)}] {a.Text}"))
                }
            };

            var sb = new StringBuilder();
            var curIndex = 0;
            var tracks = album.Tracks.Items;
            var trackMax = tracks.Count - 1;
            while (sb.Length < 2000 && curIndex < trackMax)
            {
                var track = tracks[curIndex];
                sb.AppendLine(
                    $"{curIndex + 1} - {UrlHelper.CreateMarkdownUrl(track.Name, track.GetTrackUrl())} by {track.Artists.Select(ab => ab.Name).Humanize()}");
                curIndex++;
            }

            if (curIndex + 1 != tracks.Count) sb.AppendLine($"And {tracks.Count - (curIndex + 1)} more...");

            embed.Description = sb.ToString();

            embed.AddField("Release Date",
                DateTime.TryParse(album.ReleaseDate, out var dt) ? dt.ToString("D") : album.ReleaseDate, true);
            if (album.Genres.Any()) embed.AddField("Genres", album.Genres.Humanize(), true);
            return Ok(embed);
        }
    }
}