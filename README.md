# GitMerger
Jira WebHook Receiver to merge Git branches based on Issue Keys.

Whenever Jira is the leading system and no dedicated Git-hosted workflow is present (that takes care of merging finished branches for you, such as [GitHub](https://github.com), [GitLab](https://gitlab.com) or [Atlassian Stash](https://www.atlassian.com/software/stash)), merging branches whenever the Jira issue is marked as done needs to be handled in some way. And in most cases, this means a developer needs to do the merge.

Using a [GitFlow-style branching model](http://nvie.com/posts/a-successful-git-branching-model/) (except that his ```develop``` becomes ```master``` and his ```master``` becomes ```stable```) where every feature branch has the exact same name as the related Jira issue and a central repository is used instead of forks, a 1:1 mapping from Jira issues to Git branches is fairly easy and certain things can be done by convention.

## Targetted usage workflow
The targetted scenario and usage workflow of GitMerger is a single, centralized repository server (one remote for all developers, no forks) where Branch names map to Jira issues. Other than ```fetch```/```push``` access for the service user, the only thing that needs to be set up is a [WebHook inside Jira](#jira-webhook-setup).

1. Someone creates a Jira issue (which could be a Bug Report, an Enhancement Request or just a general Task to be worked on)
2. A developer takes over and creates a new Branch to start his work. The Branch uses the Jira issue key as name (such as ```JRA-724```). He also sets the Jira issue to be "In Progress" so everyone knows what he's up to.
3. The developer commits his changes, pushes them to the central server and transitions the Jira issue to the "Resolved" state. This indicates that other developers can start their Code Review, comment on the work and most preferably try out the implementation as some general QA before anything hits the development Branch.
4. A different developer and/or someone that has access to a build of this particular Branch performs various tasks to confirm the "Resolved" state and ultimately transitions the issue to "Closed".
5. All that's left now is to merge the Feature Branch into the main development branch to make sure everyone else has access to it...plus some cleanup on the central repository to make sure no old branches stick around.

Step 5. is the one that GitMerger aims to automate, and right now this even works (with some degree of stability). Over the course of actually using it in a production environment, Bugs are likely to appear and will be fixed as we go.  
Step 2. would be another thing that could easily be automated using a ```post-receive``` hook on the central Git repository server. And to make things nice, this could be taken care of by GitMerger aswell (since it has all the data available already); all it needs is a little push.

## Service Setup
At the moment, GitMerger is just a Command line application and needs to be run by hand. For the future, a Windows Service host is planned to make it a real Windows Service. Other than that, it only needs some [configuration](#service-configuration).

### Service configuration
Configuration is mostly confined to App.config, which uses a trick to let Castle Windsor read the values into configuration objects automatically. The following configuration values are current present:

#### Host Settings (```hostSettings```)
* ```BaseAddress``` specifies Hostname and Port (plus a base uri) where the service will be available. GitMerger uses ASP.Net WebApi and OWIN Self-Hosting, so this should be a HTTP address. Use a single \* (star) instead of a hostname to listen on all available interfaces.

#### Jira Settings (```jiraSettings```)
* ```BaseUrl``` specifies the full Uri to the Jira installation.
* ```UserName``` is a Jira user that can both see the related projects and has at least permissions to comment on issues.
* ```Password``` is the **plain-text** password for ```UserName```. A future version might use a more sophisticated and safer mechanism (such as OAuth), but this is a low priority target at this point since it runs in a local environment anyways.
* ```ValidTransitions``` is an array of transitions inside Jira that count as valid triggers to begin a merge. Use the Jira REST API Browser or the Administrators Interface to find out the actual ID of a transition (or resolution/status).
* ```ValidResolutions``` is an array of resolutions inside Jira that count as valid triggers to begin a merge. Even when a valid transition has happened, the resolution might be a "Won't Fix" type one that doesn't have a branch.
* ```ClosedStatus``` is an array of status inside Jira that indicate an issue as "Closed" or otherwise "Should be merged".

#### Git Settings (```gitSettings```)
* ```GitExecutable``` is the full path to the Git executable so we can actually interact with a Git repository.
* ```UserName``` is the Committers User Name that shows up in the merge commit.
* ```EMail``` is the Committers Email Address that shows up in the merge commit.
* ```MergeDelay``` is a TimeSpan value used to delay the actual merge. Sometimes, users might accidentally close an issue and reopen it again right away. If this happens, no merge should occur unless the Jira issue is still eligible for a merge.
* ```RepositoryBasePath``` is the full folder path to a location where GitMerger will put the cloned repositories. Make sure this is on a disk with sufficiant space available.
* ```Repositories``` is an array of repository uris which will be probed for a branch with the same name as the Jira issue key. At the moment, no issue key to repository mapping exists (as the same issue key might affect multiple repositories); and all repositories are always checked for all merges.

## Jira WebHook Setup
1. Log into the Jira Administration Interface as Jira Administrator.
2. Browse to "System" > "WebHooks" (in the "Advanced" section) and create a new WebHook.
3. As WebHook Url, specify the GitMerger Base address (from [Host Settings](#host-settings-hostsettings)) and append ```/jira``` (ex. with a Base Address of ```http://*:1234/merger```, enter this as Uri: ```http://git-merger-host:1234/merger/jira```). Depending on your Jira version, you might need to check "Issue updates" for this trigger to work from a transition.
4. Browse to "Issues" > "Workflows" and select your workflow. If multiple workflows are used across different Jira projects, repeat the steps for every affected workflow. **Note**: If the default workflow is used, a copy needs to be made and switched in the related projects; since the system workflow cannot be edited.
5. View the workflow and select the eligible transition. This transition should be entered in the [Jira Settings section](#jira-settings-jirasettings) to allow the validation to succeed.
6. Switch to "Post Functions" and hit the "Add a new post function" Link. Select the function created in Step 3.
7. When using multiple workflows, repeat the Steps from 4. again for every affected workflow.

## Infrastructure and Technolgy
GitMerger uses the following components, mostly pulled from NuGet due to convinience reasons:
* [ASP.Net WebApi](http://www.asp.net/web-api) and [OWIN](http://www.asp.net/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api) to self-host a simple HTTP service that can receive HTTP POST callbacks (such as the Jira WebHooks).
* [Castle Windsor](http://www.castleproject.org/projects/windsor/) for Dependency Injection, because I've worked with Windsor for quite some time and grew fond of its features.
* [Common.Logging](http://netcommon.sourceforge.net/) and [NLog](http://nlog-project.org/) for Logging (mostly because Common.Logging is one of the few providing a lambda syntax instead of the boring ```if (logger.LevelEnabled) logger.Level("message")``` pattern). NLog just tags along, because my first thought log4net seems to have some odd dependency issues at the moment.
