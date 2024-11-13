﻿using Github.NET.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Github.NET.Sdk.Request
{
    public sealed class GithubProjectAPI
    {

        public async ValueTask<(bool, string)> ExistAsync(string ownerName, string repoName, string projectName, bool? isOpen = null)
        {
            var exist = false;
            string error = string.Empty;
            if (isOpen.HasValue)
            {
                if (isOpen.Value)
                {
                    (var result, error) = await GithubGraphRequest
                    .Query()
                    .Define("repository", p => p.WithParameter("owner", $"{ownerName}").WithParameter("name", $"{repoName}"))
                    .Child("projectsV2", p => p.WithParameter("first", 1).WithParameter("query", projectName), e => e
                        .Child("nodes", e => e
                            .Child("id", "title", "number"))).GraphResultAsync<GithubGraphReturn>();

                    var nodes = result?.Data?.Repository?.ProjectsV2?.Nodes;
                    if (nodes != null)
                    {
                        exist = nodes.Length > 0;
                    }

                }
                else
                {
                    (var result, error) = await GithubGraphRequest
                   .Query()
                   .Define("repository", p => p.WithParameter("owner", $"{ownerName}").WithParameter("name", $"{repoName}"))
                   .Child("projectsV2", p => p.WithParameter("first", 100).WithParameter("orderBy", "{direction:DESC,field:UPDATED_AT}", false), e => e
                       .Child("nodes", e => e
                           .Child("id", "title", "number"))).GraphResultAsync<GithubGraphReturn>();

                    var projects = result?.Data?.Repository?.ProjectsV2?.Nodes;
                    if (projects != null && projects.Length > 0)
                    {
                        exist = projects.Any(p => p.Title == projectName);
                    }
                }
            }
            else
            {

                (var result, error) = await GithubGraphRequest
                    .Query()
                    .Define("repository", p => p.WithParameter("owner", $"{ownerName}").WithParameter("name", $"{repoName}"))
                    .Child("projectsV2", p => p.WithParameter("first", 1).WithParameter("query", projectName), e => e
                        .Child("nodes", e => e
                            .Child("id", "title", "number"))).GraphResultAsync<GithubGraphReturn>();

                var projects = result?.Data?.Repository?.ProjectsV2?.Nodes;
                if (projects != null && projects.Length > 0)
                {
                    exist = projects.Any(p => p.Title == projectName);
                }
                if (!exist)
                {
                    (result, error) = await GithubGraphRequest
                    .Query()
                    .Define("repository", p => p.WithParameter("owner", $"{ownerName}").WithParameter("name", $"{repoName}"))
                    .Child("projectsV2", p => p.WithParameter("first", 100).WithParameter("orderBy", "{direction:DESC,field:UPDATED_AT}", false), e => e
                        .Child("nodes", e => e
                            .Child("id", "title", "number"))).GraphResultAsync<GithubGraphReturn>();

                    projects = result?.Data?.Repository?.ProjectsV2?.Nodes;
                    if (projects != null && projects.Length > 0)
                    {
                        exist = projects.Any(p => p.Title == projectName);
                    }
                }

            }
            return (exist, error);

        }
        public async Task<(GithubProject?, string)> GetAsync(string ownerName, string repoName, string projectName)
        {
            (var result, string error) = await GithubGraphRequest
                .Query()
                .Define("repository", p => p.WithParameter("owner", $"{ownerName}").WithParameter("name", $"{repoName}"))
                    .Child("projectsV2", p => p.WithParameter("first", 1).WithParameter("query", projectName), e => e
                            .Child("nodes", e => e
                                 .Child("id", "title", "number")
                                 .Child("field", p => p.WithParameter("name", "Status"), e => e
                                       .ChildWithStrongType("ProjectV2SingleSelectField", e => e
                                           .Child("id")
                                           .Child("options", e => e.Child("id", "name"))))
                                .Child("items", p => p.WithParameter("last", 100), e => e
                                         .Child("nodes", e => e
                                              .Child("id")
                                              .Child("content", e => e
                                                   .ChildWithStrongType("PullRequest", e => e
                                                       .Child("id"))))))).GraphResultAsync<GithubGraphReturn>();

            var projects = result?.Data?.Repository?.ProjectsV2;
            if (projects != null && projects.Nodes != null && projects.Nodes.Length > 0)
            {
                var project = projects.Nodes[0];
                if (project.Title == projectName)
                {
                    return (project, error);
                }
            }
            return (null, error);

        }
        public async Task<(GithubProject?, string)> CreateAsync(string ownerId, string repoId, string projectName)
        {
            (var result, string error) = await GithubGraphRequest
                .Mutation()
                .Define("createProjectV2", p => p
                     .WithParameter("ownerId", ownerId)
                     .WithParameter("repositoryId", repoId)
                     .WithParameter("title", projectName))
                .Child("projectV2", e => e.Child("id", "number"))
                .GraphResultAsync<GithubGraphReturn>();
            return (result?.Data?.CreateProjectV2?.ProjectV2, error);
        }
        public async ValueTask<(bool, string)> UpdateAsync(string projectId, bool? closed = null, bool? visiable = null, string? readme = null, string? shortDescription = null, string? title = null)
        {
            (var result, string error) = await GithubGraphRequest
                .Mutation()
                .Define("updateProjectV2", p =>
                {
                    p.WithParameter("projectId", projectId);
                    if (closed != null)
                    {
                        p.WithParameter("closed", closed.Value);
                    }
                    if (visiable != null)
                    {
                        p.WithParameter("public", visiable.Value);
                    }
                    if (title != null)
                    {
                        p.WithParameter("title", title);
                    }
                    if (shortDescription != null)
                    {
                        p.WithParameter("shortDescription", shortDescription);
                    }
                    if (readme != null)
                    {
                        p.WithParameter("readme", readme);
                    }
                })
                .Child("projectV2", e => e.Child("id", "number"))
                .GraphResultAsync<GithubGraphReturn>();
            var id = result?.Data?.UpdateProjectV2?.ProjectV2?.Id;
            return (id != null && id != string.Empty, error);
        }

        public async ValueTask<(bool, string)> UpdateItemStatusAsync(string projectId, string itemId, string fieldId, ProjectV2SingleSelectOptions value)
        {
            (var result, string error) = await GithubGraphRequest
               .Mutation()
               .Define("updateProjectV2ItemFieldValue", p => p
                   .WithParameter("projectId", projectId)
                   .WithParameter("itemId", itemId)
                   .WithParameter("fieldId", fieldId)
                   .WithParameter("value", $"{{singleSelectOptionId: \\\"{value.Id}\\\"}}", false)
                )
               .Child("projectV2Item", e => e
                   .Child("id")
                   .Child("fieldValueByName", p => p.WithParameter("name", "Status"), e => e
                    .ChildWithStrongType("ProjectV2ItemFieldSingleSelectValue", e => e
                        .Child("id", "name"))
                )).GraphResultAsync<GithubGraphReturn>();
            var id = result?.Data?.UpdateProjectV2ItemFieldValue?.ProjectV2Item?.FieldValueByName?.Name;
            return (id != null && id == value.Name, error);
        }

        public async Task<(GithubProjectItem?, string)> AddItemAsync(string projectId, string contentId)
        {

            (var result, string error) = await GithubGraphRequest
               .Mutation()
               .Define("addProjectV2ItemById", p => p
                   .WithParameter("projectId", projectId)
                   .WithParameter("contentId", contentId)
                )
               .Child("item", e => e
                   .Child("id")
                ).GraphResultAsync<GithubGraphReturn>();

            return (result?.Data?.AddProjectV2ItemById?.Item, error);

        }
    }
}