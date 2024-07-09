using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace ReleaseNotesEmailSender.Services
{
    internal class GitHubClientService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly string _repoOwner;
        private readonly string _repoName;

        private GitHubClientService(GitHubClient gitHubClient, string repoOwner, string repoName)
        {
            _gitHubClient = gitHubClient;
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        public static GitHubClientService Create(string accessToken, string repoOwner, string repoName)
        {
            var gitHubClient = new GitHubClient(new ProductHeaderValue("ReleaseEmail"))
            {
                Credentials = new Credentials(accessToken)
            };

            return new GitHubClientService(gitHubClient, repoOwner, repoName);
        }

        public async Task<List<PullRequest>> GetPRsAsync(string tag)
        {
            var allPullRequests = await _gitHubClient.Repository.PullRequest.GetAllForRepository(_repoOwner, _repoName, new PullRequestRequest
            {
                State = ItemStateFilter.Closed
            });

            var filteredPRs = allPullRequests
                .Where(pr => pr.Merged && pr.Labels.Any(label => label.Name.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return filteredPRs;
        }
    }
}
