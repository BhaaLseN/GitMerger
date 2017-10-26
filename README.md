# GitMerger
Jira WebHook Receiver to merge Git branches based on Issue Keys.

Whenever Jira is the leading system and no dedicated Git-hosted workflow is present (that takes care of merging finished branches for you, such as [GitHub](https://github.com), [GitLab](https://gitlab.com) or [Atlassian Stash](https://www.atlassian.com/software/stash)), merging branches whenever the Jira issue is marked as done needs to be handled in some way. And in most cases, this means a developer needs to do the merge.

Using a [GitFlow-style branching model](http://nvie.com/posts/a-successful-git-branching-model/) (except that his `develop` becomes `master` and his `master` becomes `stable`) where every feature branch has the exact same name as the related Jira issue and a central repository is used instead of forks, a 1:1 mapping from Jira issues to Git branches is fairly easy and certain things can be done by convention.

## Targetted usage workflow
The targetted scenario and usage workflow of GitMerger is a single, centralized repository server (one remote for all developers, no forks) where Branch names map to Jira issues. Other than `fetch`/`push` access for the service user, the only thing that needs to be set up is a [WebHook inside Jira](#jira-webhook-setup).

1. Someone creates a Jira issue (which could be a Bug Report, an Enhancement Request or just a general Task to be worked on)
2. A developer takes over and creates a new Branch to start his work. The Branch uses the Jira issue key as name (such as `JRA-724`). He also sets the Jira issue to be "In Progress" so everyone knows what he's up to.
3. The developer commits his changes, pushes them to the central server and transitions the Jira issue to the "Resolved" state. This indicates that other developers can start their Code Review, comment on the work and most preferably try out the implementation as some general QA before anything hits the development Branch.
4. A different developer and/or someone that has access to a build of this particular Branch performs various tasks to confirm the "Resolved" state and ultimately transitions the issue to "Closed".
5. All that's left now is to merge the Feature Branch into the main development branch to make sure everyone else has access to it...plus some cleanup on the central repository to make sure no old branches stick around.

Step 5. is the one that GitMerger aims to automate, and right now this even works (with some degree of stability). Over the course of actually using it in a production environment, Bugs are likely to appear and will be fixed as we go.  
Step 2. would be another thing that could easily be automated using a `post-receive` hook on the central Git repository server. And to make things nice, this could be taken care of by GitMerger aswell (since it has all the data available already); all it needs is a little push.

## Service Setup
GitMerger comes in two flavours; as Command line application that can be run by hand and as Windows Service host to run automatically. While the Command line application simply runs, the Windows Service host needs to be registered using [`sc create`](https://technet.microsoft.com/en-us/library/bb490995.aspx#E0KC0AA):
```
sc create GitMerger binPath= "path/to/GitMerger.ServiceHost.exe" displayName= "Git-Jira Automerger"
```
`GitMerger` is the service reference name, not the display name. Both arguments *require* the space after the equal-sign.
Other than that, it only needs some [configuration](#service-configuration).

### Service configuration
Configuration is mostly confined to App.config, which uses a trick to let Castle Windsor read the values into configuration objects automatically. The following configuration values are current present:

#### Host Settings (`hostSettings`)
* `BaseAddress` specifies Hostname and Port (plus a base uri) where the service will be available. GitMerger uses ASP.Net WebApi and OWIN Self-Hosting, so this should be a HTTP address. Use a single \* (star) instead of a hostname to listen on all available interfaces.

#### Jira Settings (`jiraSettings`)
* `BaseUrl` specifies the full Uri to the Jira installation.
* `UserName` is a Jira user that can both see the related projects and has at least permissions to comment on issues.
* `Password` is the **plain-text** password for `UserName`. A future version might use a more sophisticated and safer mechanism (such as OAuth), but this is a low priority target at this point since it runs in a local environment anyways.
* `ValidTransitions` is an array of transitions inside Jira that count as valid triggers to begin a merge. Use the Jira REST API Browser or the Administrators Interface to find out the actual ID of a transition (or resolution/status).
* `ValidResolutions` is an array of resolutions inside Jira that count as valid triggers to begin a merge. Even when a valid transition has happened, the resolution might be a "Won't Fix" type one that doesn't have a branch.
* `ClosedStatus` is an array of status inside Jira that indicate an issue as "Closed" or otherwise "Should be merged".
* `BranchFieldName` is the name of a [Jira custom field](#jira-custom-fields) whose value may specify the actual branch name. By default, the Jira issue key will be used as the branch name to be merged (and matched case insensitively). When set, the branch name is assumed to be the exact spelling and may cause the merge to abort if the spelling does not match.
* `DisableAutomergeFieldName` is the name of a [Jira custom field](#jira-custom-fields) whose value may prevent the automatic merge. Used in combination with `DisableAutomergeFieldValue` which needs to match up.
* `DisableAutomergeFieldValue` is the string value of the custom field specified by `DisableAutomergeFieldName` indicating opt-out for the automatic merge. Should the custom field of the issue be set to that value, no merge will be triggered and left for the assignee to take care of.
* `UpstreamBranchFieldName` is the name of a [Jira custom field](#jira-custom-fields) whose value may specify the upstream branch (which the issue branch is merged into; instead of defaulting to "master").

#### Git Settings (`gitSettings`)
* `GitExecutable` is the full path to the Git executable so we can actually interact with a Git repository.
* `UserName` is the Committers User Name that shows up in the merge commit.
* `EMail` is the Committers Email Address that shows up in the merge commit.
* `MergeDelay` is a TimeSpan value used to delay the actual merge. Sometimes, users might accidentally close an issue and reopen it again right away. If this happens, no merge should occur unless the Jira issue is still eligible for a merge.
* `RepositoryBasePath` is the full folder path to a location where GitMerger will put the cloned repositories. Make sure this is on a disk with sufficiant space available.
* `Repositories` is an array of repository uris which will be probed for a branch with the same name as the Jira issue key. At the moment, no issue key to repository mapping exists (as the same issue key might affect multiple repositories); and all repositories are always checked for all merges.
* `IgnoredBranchPattern` is a .NET regular expression to ignore given branches. This is most useful to specify namespaces such as `private/` or `testing/` to be left alone when the issue key matches. Ignored when an exact branch name was specified by `BranchFieldName`.

## Jira WebHook Setup
1. Log into the Jira Administration Interface as Jira Administrator.
2. Browse to "System" > "WebHooks" (in the "Advanced" section) and create a new WebHook.
3. As WebHook Url, specify the GitMerger Base address (from [Host Settings](#host-settings-hostsettings)) and append `/jira` (ex. with a Base Address of `http://*:1234/merger`, enter this as Uri: `http://git-merger-host:1234/merger/jira`). Depending on your Jira version, you might need to check "Issue updates" for this trigger to work from a transition.
4. Browse to "Issues" > "Workflows" and select your workflow. If multiple workflows are used across different Jira projects, repeat the steps for every affected workflow. **Note**: If the default workflow is used, a copy needs to be made and switched in the related projects; since the system workflow cannot be edited.
5. View the workflow and select the eligible transition. This transition should be entered in the [Jira Settings section](#jira-settings-jirasettings) to allow the validation to succeed.
6. Switch to "Post Functions" and hit the "Add a new post function" Link. Select the function created in Step 3.
7. When using multiple workflows, repeat the Steps from 4. again for every affected workflow.

## Jira Custom Fields
Some features might check for certain custom fields to alter behavior. Custom fields need to be configured using Administration > Issues > Fields > Custom Fields and assigned to an issue screen to show up. They need not be searchable, so they can be configured with a Search Template of "None".
Internally, custom fields get an id (which can be seen in the Administration URL for the given field configuration) and show up as "cf[ID]" in a search query, or as "customfield_ID" in a REST result.

### Automerge Opt-Out / Disable Automerge for a particular issue
1. Create a custom field of type "Checkboxes" (for the lack of a better, built-in type that shows just a single yes/no selection).
2. Name the custom field in a way that the issue assignee recognizes its opt-out value. For example, call it "Automatic Merge" (since its value will affect the automerge).
3. Add exactly one value to the field, again using a name recognizable for the assignee. For example, call it "Disable" (so it reads "Automatic Merge: \[ \] Disable" on the issue screen). This is also the value that needs to be configured as `DisableAutomergeFieldValue`.
4. Save the field and assign it to the issue screens. This depends on your setup, but it might be a good idea to at least use the "Default Screen".

Assignees will then be able to set the checkbox to on and prevent any automatic merges from happening (for example on prototype branches that should not be merged, or on dependent branches that may need other prerequisites first which is not recorded in a Jira issue and thus not automergable).
With the default Screen configuration, the field will only show up on issue details when the value is actually set and read nicely as "Automatic Merge: Disable" (depending on your wording from steps 2. and 3.)

### Changing the default upstream branch from "master" to something else
1. Create a custom field of type "Text Field (single line)".
2. Name the custom field in a way that the issue assignee recognizes its control over where the branch will be merged into.
3. Save the field and assign it to the issue screens. This depends on your setup, but it might be a good idea to at least use the "Default Screen".

Assignees will then be able to change the default branch into which the issue branch is merged. By default (and when the field is not set/empty), this branch will be "master".
Changing this behavior might be a good idea when a longer running feature is developed in a parallel, master-like branch but should still receive the benefits of the automatic merge.

A potential improvement to this feature would be to check for Epic Links, Parent/Sub-Tasks and other Issue Links and try those issue keys as branches first for convention.

### Changing the default branch name from the issue key to something else
Use the same setup as for the Upstream Branch Name, except with different Name/Description of course :)

## Other Features:
* `POST` to `/jiracomment` with some JSON to add a comment to the given Issue:
```json
{
    "key": "JRA-1234",
    "comment": "This comment is added to JRA-1234"
}
```

## Infrastructure and Technology
GitMerger uses the following components, mostly pulled from NuGet due to convinience reasons:
* [ASP.Net WebApi](http://www.asp.net/web-api) and [OWIN](http://www.asp.net/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api) to self-host a simple HTTP service that can receive HTTP POST callbacks (such as the Jira WebHooks).
* [Castle Windsor](http://www.castleproject.org/projects/windsor/) for Dependency Injection, because I've worked with Windsor for quite some time and grew fond of its features.
* [Common.Logging](http://netcommon.sourceforge.net/) and [NLog](http://nlog-project.org/) for Logging (mostly because Common.Logging is one of the few providing a lambda syntax instead of the boring `if (logger.LevelEnabled) logger.Level("message")` pattern). NLog just tags along, because my first thought log4net seems to have some odd dependency issues at the moment.
