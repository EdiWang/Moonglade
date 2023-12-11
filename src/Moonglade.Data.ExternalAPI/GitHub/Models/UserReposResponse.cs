namespace Moonglade.Data.ExternalAPI.GitHub.Models
{
    public class UserReposResponse
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class License
        {
            public string key { get; set; }
            public string name { get; set; }
            public string spdx_id { get; set; }
            public object url { get; set; }
            public string node_id { get; set; }
        }

        public class Owner
        {
            public string login { get; set; }
            public int id { get; set; }
            public string node_id { get; set; }
            public string avatar_url { get; set; }
            public string gravatar_id { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string followers_url { get; set; }
            public string following_url { get; set; }
            public string gists_url { get; set; }
            public string starred_url { get; set; }
            public string subscriptions_url { get; set; }
            public string organizations_url { get; set; }
            public string repos_url { get; set; }
            public string events_url { get; set; }
            public string received_events_url { get; set; }
            public string type { get; set; }
            public bool site_admin { get; set; }
        }

        public class Permissions
        {
            public bool admin { get; set; }
            public bool maintain { get; set; }
            public bool push { get; set; }
            public bool triage { get; set; }
            public bool pull { get; set; }
        }

        public class Root
        {
            public int id { get; set; }
            public string node_id { get; set; }
            public string name { get; set; }
            public string full_name { get; set; }
            public bool @private { get; set; }
            public Owner owner { get; set; }
            public string html_url { get; set; }
            public string description { get; set; }
            public bool fork { get; set; }
            public string url { get; set; }
            public string forks_url { get; set; }
            public string keys_url { get; set; }
            public string collaborators_url { get; set; }
            public string teams_url { get; set; }
            public string hooks_url { get; set; }
            public string issue_events_url { get; set; }
            public string events_url { get; set; }
            public string assignees_url { get; set; }
            public string branches_url { get; set; }
            public string tags_url { get; set; }
            public string blobs_url { get; set; }
            public string git_tags_url { get; set; }
            public string git_refs_url { get; set; }
            public string trees_url { get; set; }
            public string statuses_url { get; set; }
            public string languages_url { get; set; }
            public string stargazers_url { get; set; }
            public string contributors_url { get; set; }
            public string subscribers_url { get; set; }
            public string subscription_url { get; set; }
            public string commits_url { get; set; }
            public string git_commits_url { get; set; }
            public string comments_url { get; set; }
            public string issue_comment_url { get; set; }
            public string contents_url { get; set; }
            public string compare_url { get; set; }
            public string merges_url { get; set; }
            public string archive_url { get; set; }
            public string downloads_url { get; set; }
            public string issues_url { get; set; }
            public string pulls_url { get; set; }
            public string milestones_url { get; set; }
            public string notifications_url { get; set; }
            public string labels_url { get; set; }
            public string releases_url { get; set; }
            public string deployments_url { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public DateTime pushed_at { get; set; }
            public string git_url { get; set; }
            public string ssh_url { get; set; }
            public string clone_url { get; set; }
            public string svn_url { get; set; }
            public string homepage { get; set; }
            public int size { get; set; }
            public int stargazers_count { get; set; }
            public int watchers_count { get; set; }
            public object language { get; set; }
            public bool has_issues { get; set; }
            public bool has_projects { get; set; }
            public bool has_downloads { get; set; }
            public bool has_wiki { get; set; }
            public bool has_pages { get; set; }
            public bool has_discussions { get; set; }
            public int forks_count { get; set; }
            public object mirror_url { get; set; }
            public bool archived { get; set; }
            public bool disabled { get; set; }
            public int open_issues_count { get; set; }
            public License license { get; set; }
            public bool allow_forking { get; set; }
            public bool is_template { get; set; }
            public bool web_commit_signoff_required { get; set; }
            public List<object> topics { get; set; }
            public string visibility { get; set; }
            public int forks { get; set; }
            public int open_issues { get; set; }
            public int watchers { get; set; }
            public string default_branch { get; set; }
            public Permissions permissions { get; set; }
        }


    }
}