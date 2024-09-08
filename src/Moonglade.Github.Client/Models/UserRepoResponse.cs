namespace Moonglade.Github.Client.Models;

using System.Text.Json.Serialization;

public class License
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("spdx_id")]
    public string SpdxId { get; set; } = String.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = String.Empty;

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = String.Empty;
}

public class Owner
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = String.Empty;

    [JsonPropertyName("id")]
    public int? Id { get; set; } = 0;

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = String.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = String.Empty;

    [JsonPropertyName("gravatar_id")]
    public string GravatarId { get; set; } = String.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = String.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = String.Empty;

    [JsonPropertyName("followers_url")]
    public string FollowersUrl { get; set; } = String.Empty;

    [JsonPropertyName("following_url")]
    public string FollowingUrl { get; set; } = String.Empty;

    [JsonPropertyName("gists_url")]
    public string GistsUrl { get; set; } = String.Empty;

    [JsonPropertyName("starred_url")]
    public string StarredUrl { get; set; } = String.Empty;

    [JsonPropertyName("subscriptions_url")]
    public string SubscriptionsUrl { get; set; } = String.Empty;

    [JsonPropertyName("organizations_url")]
    public string OrganizationsUrl { get; set; } = String.Empty;

    [JsonPropertyName("repos_url")]
    public string ReposUrl { get; set; } = String.Empty;

    [JsonPropertyName("events_url")]
    public string EventsUrl { get; set; } = String.Empty;

    [JsonPropertyName("received_events_url")]
    public string ReceivedEventsUrl { get; set; } = String.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = String.Empty;

    [JsonPropertyName("site_admin")]
    public bool? SiteAdmin { get; set; }
}

public class Permissions
{
    [JsonPropertyName("admin")]
    public bool? Admin { get; set; }

    [JsonPropertyName("maintain")]
    public bool? Maintain { get; set; }

    [JsonPropertyName("push")]
    public bool? Push { get; set; }

    [JsonPropertyName("triage")]
    public bool? Triage { get; set; }

    [JsonPropertyName("pull")]
    public bool? Pull { get; set; }
}

public class UserRepository
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = String.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = String.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = String.Empty;

    [JsonPropertyName("private")]
    public bool? Private { get; set; }

    [JsonPropertyName("owner")]
    public Owner Owner { get; set; } = new();

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = String.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = String.Empty;

    [JsonPropertyName("fork")]
    public bool? Fork { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = String.Empty;

    [JsonPropertyName("forks_url")]
    public string ForksUrl { get; set; } = String.Empty;

    [JsonPropertyName("keys_url")]
    public string KeysUrl { get; set; } = String.Empty;

    [JsonPropertyName("collaborators_url")]
    public string CollaboratorsUrl { get; set; } = String.Empty;

    [JsonPropertyName("teams_url")]
    public string TeamsUrl { get; set; } = String.Empty;

    [JsonPropertyName("hooks_url")]
    public string HooksUrl { get; set; } = String.Empty;

    [JsonPropertyName("issue_events_url")]
    public string IssueEventsUrl { get; set; } = String.Empty;

    [JsonPropertyName("events_url")]
    public string EventsUrl { get; set; } = String.Empty;

    [JsonPropertyName("assignees_url")]
    public string AssigneesUrl { get; set; } = String.Empty;

    [JsonPropertyName("branches_url")]
    public string BranchesUrl { get; set; } = String.Empty;

    [JsonPropertyName("tags_url")]
    public string TagsUrl { get; set; } = String.Empty;

    [JsonPropertyName("blobs_url")]
    public string BlobsUrl { get; set; } = String.Empty;

    [JsonPropertyName("git_tags_url")]
    public string GitTagsUrl { get; set; } = String.Empty;

    [JsonPropertyName("git_refs_url")]
    public string GitRefsUrl { get; set; } = String.Empty;

    [JsonPropertyName("trees_url")]
    public string TreesUrl { get; set; } = String.Empty;

    [JsonPropertyName("statuses_url")]
    public string StatusesUrl { get; set; } = String.Empty;

    [JsonPropertyName("languages_url")]
    public string LanguagesUrl { get; set; } = String.Empty;

    [JsonPropertyName("stargazers_url")]
    public string StargazersUrl { get; set; } = String.Empty;

    [JsonPropertyName("contributors_url")]
    public string ContributorsUrl { get; set; } = String.Empty;

    [JsonPropertyName("subscribers_url")]
    public string SubscribersUrl { get; set; } = String.Empty;

    [JsonPropertyName("subscription_url")]
    public string SubscriptionUrl { get; set; } = String.Empty;

    [JsonPropertyName("commits_url")]
    public string CommitsUrl { get; set; } = String.Empty;

    [JsonPropertyName("git_commits_url")]
    public string GitCommitsUrl { get; set; } = String.Empty;

    [JsonPropertyName("comments_url")]
    public string CommentsUrl { get; set; } = String.Empty;

    [JsonPropertyName("issue_comment_url")]
    public string IssueCommentUrl { get; set; } = String.Empty;

    [JsonPropertyName("contents_url")]
    public string ContentsUrl { get; set; } = String.Empty;

    [JsonPropertyName("compare_url")]
    public string CompareUrl { get; set; } = String.Empty;

    [JsonPropertyName("merges_url")]
    public string MergesUrl { get; set; } = String.Empty;

    [JsonPropertyName("archive_url")]
    public string ArchiveUrl { get; set; } = String.Empty;

    [JsonPropertyName("downloads_url")]
    public string DownloadsUrl { get; set; } = String.Empty;

    [JsonPropertyName("issues_url")]
    public string IssuesUrl { get; set; } = String.Empty;

    [JsonPropertyName("pulls_url")]
    public string PullsUrl { get; set; } = String.Empty;

    [JsonPropertyName("milestones_url")]
    public string MilestonesUrl { get; set; } = String.Empty;

    [JsonPropertyName("notifications_url")]
    public string NotificationsUrl { get; set; } = String.Empty;

    [JsonPropertyName("labels_url")]
    public string LabelsUrl { get; set; } = String.Empty;

    [JsonPropertyName("releases_url")]
    public string ReleasesUrl { get; set; } = String.Empty;

    [JsonPropertyName("deployments_url")]
    public string DeploymentsUr { get; set; } = String.Empty;

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("pushed_at")]
    public DateTime? PushedAt { get; set; }

    [JsonPropertyName("git_url")]
    public string GitUrl { get; set; } = String.Empty;

    [JsonPropertyName("ssh_url")]
    public string SshUrl { get; set; } = String.Empty;

    [JsonPropertyName("clone_url")]
    public string CloneUrl { get; set; } = String.Empty;

    [JsonPropertyName("svn_url")]
    public string SvnUrl { get; set; } = String.Empty;

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = String.Empty;

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int? StargazersCount { get; set; }

    [JsonPropertyName("watchers_count")]
    public int? WatchersCount { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = String.Empty;

    [JsonPropertyName("has_issues")]
    public bool? HasIssues { get; set; }

    [JsonPropertyName("has_projects")]
    public bool? HasProjects { get; set; }

    [JsonPropertyName("has_downloads")]
    public bool? HasDownloads { get; set; }

    [JsonPropertyName("has_wiki")]
    public bool? HasWiki { get; set; }

    [JsonPropertyName("has_pages")]
    public bool? HasPages { get; set; }

    [JsonPropertyName("has_discussions")]
    public bool? HasDiscussions { get; set; }

    [JsonPropertyName("forks_count")]
    public int? ForksCount { get; set; }

    [JsonPropertyName("mirror_url")]
    public string MirrorUrl { get; set; } = String.Empty;

    [JsonPropertyName("archived")]
    public bool? Archived { get; set; }

    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }

    [JsonPropertyName("open_issues_count")]
    public int? OpenIssuesCount { get; set; }

    [JsonPropertyName("license")]
    public License License { get; set; } = new();

    [JsonPropertyName("allow_forking")]
    public bool? AllowForking { get; set; }

    [JsonPropertyName("is_template")]
    public bool? IsTemplate { get; set; }

    [JsonPropertyName("web_commit_signoff_required")]
    public bool? WebCommitSignoffRequired { get; set; }

    [JsonPropertyName("topics")]
    public List<object> Topics { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = String.Empty;

    [JsonPropertyName("forks")]
    public int? Forks { get; set; }

    [JsonPropertyName("open_issues")]
    public int? OpenIssues { get; set; }

    [JsonPropertyName("watchers")]
    public int? Watchers { get; set; }

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = String.Empty;

    [JsonPropertyName("permissions")]
    public Permissions Permissions { get; set; } = new();
}

