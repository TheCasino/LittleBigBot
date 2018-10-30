using System.Linq;
using System.Threading.Tasks;
using Discord;
using LittleBigBot.Attributes;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using Microsoft.Extensions.Options;
using Octokit;
using Qmmands;

/**
 * Commands in this module
 *
 * - GHUser
 * - Repo
 * - Updates
 */

namespace LittleBigBot.Modules
{
    [Name("GitHub")]
    [Description("Commands that relate to GitHub, the open-source, online code sharing platform.")]
    public class GitHubModule : LittleBigBotModuleBase
    {
        public const string GitHubRepoName = "LittleBigBot";
        public const string GitHubRepoOwner = "littlebigtragedy";

        public IOptions<LittleBigBotConfig> AppConfig { get; set; }
        public CommandService CommandService { get; set; }
        public GitHubClient GHClient { get; set; }

        [Command("GHUser", "GitHubUser")]
        [RunMode(RunMode.Parallel)]
        [Cooldown(1, 3, CooldownMeasure.Seconds, CooldownType.User)]
        public async Task<BaseResult> Command_GetGHUserAsync(
            [Name("Username")] [Description("The username of the user to view.")]
            string username)
        {
            try
            {
                var user = await GHClient.User.Get(username);
                var embed = new EmbedBuilder
                {
                    Color = LittleBigBot.DefaultEmbedColour,
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = user.AvatarUrl,
                        Name = $"{user.Login}{(string.IsNullOrWhiteSpace(user.Name) ? "" : $" ({user.Name})")}",
                        Url = user.HtmlUrl
                    },
                    Description = string.IsNullOrWhiteSpace(user.Bio) ? "No biography" : user.Bio,
                    ThumbnailUrl = user.AvatarUrl
                };

                embed.AddField("Public Repositories", user.PublicRepos, true);
                embed.AddField("Public Gists", user.PublicGists, true);
                embed.AddField("Open for Hire", user.Hireable != null && user.Hireable.Value ? "Yes" : "No", true);
                if (!string.IsNullOrWhiteSpace(user.Company)) embed.AddField("Company", user.Company, true);
                if (!string.IsNullOrWhiteSpace(user.Location)) embed.AddField("Location", user.Location, true);
                if (user.DiskUsage != null) embed.AddField("Disk Usage", user.DiskUsage + "mb", true);

                return Ok(embed);
            }
            catch (NotFoundException)
            {
                return NotFound("User not found.");
            }
        }

        [Command("Repo", "Repository", "ViewRepo", "ViewRepository", "GH", "GitHub")]
        [RunMode(RunMode.Parallel)]
        [Description("Views a GitHub repository.")]
        [Cooldown(1, 3, CooldownMeasure.Seconds, CooldownType.User)]
        public async Task<BaseResult> Command_GetGitHubRepoAsync(
            [Name("Repo ID")] [Description("The repo ID of the repository to view.")]
            string repoLink = GitHubRepoOwner + "/" + GitHubRepoName)
        {
            var repoLinkParts = repoLink.Split("/");

            var repoOwner = repoLinkParts.FirstOrDefault();
            var repoName = repoLinkParts.ElementAtOrDefault(1);

            if (string.IsNullOrWhiteSpace(repoOwner) || string.IsNullOrWhiteSpace(repoName))
                return BadRequest("Invalid repository owner or name.");

            try
            {
                var repo = await GHClient.Repository.Get(repoOwner, repoName);
                var embed = new EmbedBuilder
                {
                    Color = LittleBigBot.DefaultEmbedColour,
                    Author = new EmbedAuthorBuilder
                    {
                        Url = repo.HtmlUrl,
                        IconUrl = repo.Owner.AvatarUrl,
                        Name = repo.FullName
                    },
                    Description = repo.Description,
                    ThumbnailUrl = repo.Owner.AvatarUrl
                };
                embed.AddField("Language", string.IsNullOrWhiteSpace(repo.Language) ? "No Language" : repo.Language,
                    true);
                if (repo.License != null) embed.AddField("License", repo.License.Name, true);
                embed.AddField("Stargazers", repo.StargazersCount, true);
                embed.AddField("Subscribers", repo.SubscribersCount, true);
                embed.AddField("Created", repo.CreatedAt.ToUniversalTime().ToString("R"), true);
                if (repo.PushedAt != null)
                    embed.AddField("Last Push", repo.PushedAt?.ToUniversalTime().ToString("R"), true);

                return Ok(embed);
            }
            catch (NotFoundException)
            {
                return NotFound("Repository not found.");
            }
        }

        [Command("Updates", "GetUpdate", "ViewUpdate", "ViewUpdates", "Update")]
        [RunMode(RunMode.Parallel)]
        [Description("Views all updates on the GH repo, or gets a commit by ID.")]
        [Cooldown(1, 3, CooldownMeasure.Seconds, CooldownType.User)]
        public async Task<BaseResult> Command_GetUpdateAsync([Name("Repo")] [Description("The repository to use.")]
            string repo = GitHubRepoOwner + "/" + GitHubRepoName, [Name("Commit ID")] [Description("The ID of the commit to get.")] [DefaultValueDescription("Views the last three commits.")]
            string updateId = null)
        {
            var repoParts = repo.Split("/");
            if (repoParts.Length != 2) return BadRequest("Invalid repository.");

            var repoAuthor = repoParts.FirstOrDefault();
            var repoName = repoParts.ElementAtOrDefault(1);

            try
            {
                if (updateId != null)
                {
                    if (updateId == "latest")
                        updateId = (await GHClient.Repository.Commit.GetAll(repoAuthor, repoName)).FirstOrDefault()
                            ?.Sha;

                    var commit = await GHClient.Repository.Commit.Get(repoAuthor, repoName, updateId);

                    if (commit == null) return BadRequest($"Cannot find commit {updateId}.");

                    var commitEmbed = new EmbedBuilder();
                    commitEmbed.WithColor(LittleBigBot.DefaultEmbedColour);
                    commitEmbed.WithAuthor(a =>
                    {
                        a.Name = commit.Author.Login + " (ID " + commit.Author.Id + ")";
                        a.IconUrl = commit.Author.AvatarUrl;
                        a.Url = commit.HtmlUrl;
                    });
                    commitEmbed.WithTitle(commit.Commit.Message);
                    commitEmbed.WithDescription("SHA reference " + commit.Sha);
                    commitEmbed.AddField("Files changed", commit.Files.Count, true);
                    commitEmbed.AddField("Additions", commit.Stats.Additions, true);
                    commitEmbed.AddField("Deletions", commit.Stats.Deletions, true);
                    commitEmbed.AddField("Parent", commit.Parents.First().Sha);
                    return Ok(commitEmbed);
                }

                var commits = await Task.WhenAll((await GHClient.Repository.Commit.GetAll(repoAuthor, repoName)).Take(3)
                    .Select(a => GHClient.Repository.Commit.Get(repoAuthor, repoName, a.Sha)));

                var embed = new EmbedBuilder();
                embed.WithColor(LittleBigBot.DefaultEmbedColour);
                embed.WithAuthor(a =>
                    a.WithIconUrl(Context.Client.CurrentUser.GetEffectiveAvatarUrl()).WithName("Recent Updates"));
                embed.WithDescription(string.Join("\n", commits.Select(FormatCommit)));

                return Ok(embed);
            }
            catch (RateLimitExceededException)
            {
                return BadRequest("Rate limited! Please try again later.");
            }
            catch (NotFoundException)
            {
                return NotFound("Cannot find that repository.");
            }
        }

        private string FormatCommit(GitHubCommit commit)
        {
            return
                $"{UrlHelper.CreateMarkdownUrl(commit.Sha.Substring(0, 7), commit.HtmlUrl)}: {commit.Commit.Message} (author: {commit.Author.Login}, additions: {commit.Stats.Additions}, deletions: {commit.Stats.Deletions})";
        }
    }
}