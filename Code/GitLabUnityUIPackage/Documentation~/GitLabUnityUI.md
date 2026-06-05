# GitLab Unity UI

A package made to abstract Unity web requests to GitLab in a way that facilitates the creation of GitLab UI Unity games.

## Package contents
LICENSE.md file holds the license relevant to this package. Runtime/GitLabUnityUI holds all the code for the package's actual functionality.

## Installation Instructions
Please follow the instructions at https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html after having downloaded the GitLabUnityUI package folder.

## Requirements
This package is designed to work for 6000.0.61f1 and so is the minimum recommended version to use with this. This package is dependent on UniTask 2.5.10.

## Workflows
Start by importing GitLabUnityUI using the Using directive. initialise an instance of the GitLabUI class. Start using the methods connected to it such as GetFile and fill in the parameters accordingly. 

## Advanced topics
This package provides a range of methods abstracting web requests such as GetFile, GetRepositoryTrees, etc. as well as smaller methods such as GetFileName. In addition, it also provides ways to autonomously interact repositories separately from the main thread through the use of AutonomousReplaceMissing and AutonomousDeleteExtra which can be started through createAutonomousDelete and createAutonomousReplace. To stop these autonomous interactions, remove them from the associated GitLabUI instance's AutoDeleteCatalogue or AutoReplaceCatalogue (with AutonomousDeleteExtras needing to be removed from AutoDeleteCatalogue and AutonomousReplaceMissing needing to be removed from AutoReplaceCatalogue).

## Web request error handling
Methods in this package will send back a Reponse enum type reponse from web request method calls so in the case of a failure in a web request, a Response.Error shall be returned.
In the case of invalid details being given (length being too long or of the wrong type) then a Reponse.InvalidDetails shall be returned.

In the Autonomous delete and replacing objects, any errors will set their working variable to false and write the reasoning into the AutonomousDeleteExtras/AutonomousReplaceMissing's notWorkingReason variable.
The working variable being false makes the asynchronous functionality to stop looping and for the associated instance of AutonomousDeleteExtras/AutonomousReplaceMissing to be removed from the AutoDeleteCatalogue/AutonomouReplaceCatalogue.

## Thread Safety
This package attempts to make the package thread safe by only allowing one method at a time (including the asynchronous delete and replace objects) to run (thus allowing exclusive rights to sending web requests the entire time). 

This does not effect other instances of the package so if you were to instantiate two of this package as objects and use them to send web requests simultaneously, then they would send web requests at the same time and potentially cause one of the web requests to fail. Web request methods used on the same package object regardless of asynchronosity will have to wait for permission to work before it can continue in its main body and start sending web requests.

## Usage, Input, and Return Values

Below is listed each main method for interaction and what its input parameter does. If a method has an overload that switches out the type of one element, this will presented in the parameters as typeA/typeB variableName.\
Methods often have output class deserialization objects as return values - as they are often repeated, they will be placed at the bottom of this section for structure reference.\
An out parameter response of type ResponseObj appears in most of these methods as an optional overload. They will be excluded from the method call here to avoid confusion but will be included in the parameter explanation to let you know what methods can return a ResponseObj.\
The ResponseObj parameter will always come first in the parameter order if it is inside a method call.\
Methods that can default (of format variableName = value in the method call) can be left unassigned when you call the method and it should still be able to run. Keep in mind these are default values.

**public bool CheckElementInPath(RepositoryTreeObj/string fileObj, string element, bool standaloneElement=false, bool caseSensitive = false)**

Used to check if an element is inside the given file path.\
fileObj - the file object to look for an element from.\
element - element to look for in the filePath.\
standaloneElement - false means element has to be the whole path section, otherwise it can be concatenated with other strings to make a file path segment e.g "bedroom1" from "bedroom" and "1".\
caseSensitive - false means capital letters and lower case letters make no difference and will be matched no matter which they are.

**public string GetFileName(RepositoryTreeObj fileObj)**

Used to extract a file name from a repository tree obj.\
fileObj - the file object to get the file name from.

**public string GetFileName(string filePath)**

Used to extract a file name from a string file path.\
filePath - the file path string to get the file name from.

**public bool CheckForFileAtLocation(string hostUrl, string projectCode, string token, string filePath, string repoRef = "HEAD", bool excludeDirectories = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Checks if a file at a given file path exists. Returns true if it does and false if it doesn't. Directories at the file path also count if excludeDirectories is not made true.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - path to the file. Unencoded.\
repoRef - Name of a repository branch/tag.\
excludeDirectories - if false then this method will return false if it finds a directory at the given filePath (with the correct name) but no file.\
projectCodeType - type of projectCode the method thinks was passed in.

**public bool CheckForFile(string hostUrl, string projectCode, string token, string fileName, string repoRef = "HEAD", bool excludeDirectories = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Checks if a file with a given file name exists at all inside a given repository. Directories with the name also count unless excludeDirectories is made true.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
fileName - name of the file to look for.\
repoRef - Name of a repository branch/tag.\
excludeDirectories - if false then this method will return false if it finds a directory at the given filePath (with the correct name) but no file.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response CheckFileChanged(out string state, string hostUrl, string projectCode, string token, string filePath, string originalfileContent, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in a file name and contents of an original file and the details for a new file. Will return "contents changed" if changed, "deleted/not accessible" if it cannot be found, and "unchanged" if it is found and also has the same contents.\
state - return string.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - path of the file to compare the saved file details to.\
repoRef - Name of a repository branch/tag.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response CheckForChanges(out List<string> filesAdded, out List<string> filesDeleted, out List<string> filesChanged, string hostUrl, string projectCode, string token, RepositoryStateObj originalLayout, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in a save state of a repository and the access details of a new repository to compare to. Uses web requests to compile a list of files added, deleted, and had their contents changed when compared to the old repository. Deleted files do not count as changed.\
filesAdded - files that were not in the original repository.\
filesDeleted - files that were in the original repository and now cannot be found in the new repository.\
filesChanged - files that were not deleted from the old repository state but had their contents changed.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
originalLayout - the original save state of the repository. Easily made by using the SaveRepository method.\
repoRef - Name of a repository branch/tag.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response SaveRepository(out RepositoryStateObj contents, string hostUrl, string projectCode, string token, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in repository details, saves the file paths and file names of only files (no directories as directories are made and deleted when the files inside them are also made and deleted) and returns it as a RepositoryStateObj.\
contents - RepositoryStateObj to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
repoRef - Name of a repository branch/tag.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response GetUser(out UserObj contents, string hostUrl, string personalAccessToken)**

Takes in a personal access token and returns a UserObj containing information about the user.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
contents - UserObj to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
personalAccessToken - a personal access token for a GitLab host.

**public Response GetPipelineUnitTestReport(out TestReportObj contents, string hostUrl, string projectCode, string token, string/int pipelineId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in repository details and a pipeline id and returns the unit test report for that pipeline.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
contents - TestReportObj to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
pipelineId - id of the pipeline we are looking for the unit test for.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response GetCommit(out CommitObj contents, string hostUrl, string projectCode, string token, string commitId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and makes a Get Commit request.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
contents - CommitObj to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
commitId - id of the commit we are looking for the unit test for.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response GetRepositoryCommits(out RepositoryCommitListObj[] contents, string hostUrl, string projectCode, string token, string repoRef=null, string startDate = null, string endDate = null, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and returns the list of commits made in a repository with index 0 being the latest.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
contents - RepositoryCommitListObj array to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
repoRef - The name of a repository branch, revision range, or tag.\
startDate - date and time of when you want the commits to start being listed.\
endDate - date and time of when you want the commits to stop being listed.\
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response UpdateFile(string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="updating file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and updates a file with new contents.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - file path of file to be updated.\
content - new content to put in file.\
commitMessage - commit message for your commit.\
branch - the branch of the repository to do this in.\
encodeContents - optional bool to trigger extra encoding. Useful for contents that have more formatting that may not translate properly otherwise.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response GetRepositoryTrees(out RepositoryTreeObj[] contents, string hostUrl, string projectCode, string token, bool recursive = false, string repoRef="HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and returns the list of repository trees from the repository.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
recursive - true if you want this to be able to look into directories.\
repoRef - name of the repository branch or tag you want to request in.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response CreateIssue(string hostUrl, string projectCode, string token, string title, string description="", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and creates an issue in the repository.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
title - title of the issue.\
description - description of the issue.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response DeleteFile(string hostUrl, string projectCode, string token, string filePath, string commitMessage="deleting file", string branch = "main", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and deletes a file or directory.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - file path of file to be deleted.\
commitMessage - commit message of the commit.\
branch - the branch of the repository to do this in.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response GetFile(out FileObj contents, string hostUrl, string projectCode, string token, string filePath, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and gets the information about a file.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
contents - FileObj to be returned.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - file path of file to be retrieved.\
repoRef - name of the repository branch or tag you want to request in.\
projectCodeType - type of projectCode the method thinks was passed in.

**public Response CreateFile(string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="create file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Takes in details and creates a file.\
response - ResponseObj holding more details of what response was given than the enumeration Response.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
filePath - file path where the file is to be created.\
content - the new file's content.\
commitMessage - commit message of the commit.\
projectCodeType - type of projectCode the method thinks was passed in.

**public bool ValidateRepositoryDetails(string hostUrl, string projectCode, string token = "", ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Validates the three parts that make up the repository section of a web request.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
projectCodeType - type of projectCode the method thinks was passed in.

**public AutonomousDeleteExtra createAutonomousDelete(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalLayout, string commitMessage = "deleting added file",  string branch = "main", string repoRef = "HEAD", int secondsBetweenChecks = 3, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Creates an AutonomousDeleteExtra object which deletes any files added to a repository that aren't in the RepositoryStateObj given here.\
Has a public filePathsDeleted string array for file paths of files that were deleted by this as well as a bool variable called working which is false if this has stopped working and a notWorkingReason string variable containing why it has stopped working if working is false.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
package - the instance of this whole package passed in so it can queue for web requests etc.\
originalLayout - the original layout of the repository. Can be made using SaveRepository.\
commitMessage - the commit message that will be used every time this deletes something.\
branch - the branch of the repository to do this in.\
repoRef - name of the repository branch or tag you want to request in.\
secondsBetweenChecks - seconds between each method call where extra files are checked for and deleted.\
projectCodeType - type of projectCode the method thinks was passed in.

**public AutonomousReplaceMissing createAutonomousReplace(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalLayout, string replaceCommitMessage = "replacing missing file", string updateCommitMessage = "updating file contents",  string branch = "main", string repoRef = "HEAD", int secondsBetweenChecks = 3, bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)**

Has a public filePathsReplaced string array for file paths of files that were added back by this and a filePathsUpdated string array for file paths that had their contents correct by this, as well as a bool variable called working which is false if this has stopped working and a notWorkingReason string variable containing why it has stopped working if working is false.\
hostUrl - GitLab Host url e.g "https://gitlab.com" \
projectCode - number ID of the project, URL-encoded path of the project, or not URL-encoded path of the project. If this is not a number (so a path of the project encoded or not) then please indicate the type of the project code in the project code type parameter. By default it is a number.\
token - project access token.\
package - the instance of this whole package passed in so it can queue for web requests etc.\
originalLayout - the original layout of the repository. Can be made using SaveRepository.\
replaceCommitMessage - the commit message that will be used every time this adds a file back.\
updateCommitMessage - the commit message that will be used every time a file's contents are updated by this.\
branch - the branch of the repository to do this in.\
repoRef - name of the repository branch or tag you want to request in.\
secondsBetweenChecks - seconds between each method call where extra files are checked for and deleted.\
encodeContents - if true does extra optional encoding which helps in scenarios where indentation and other formatting isn't translating through properly with normal strings.\
projectCodeType - type of projectCode the method thinks was passed in.

**public string CheckAnyAutonomous()**

Returns a string version of the catalogues holding all the currently running autonomous asynchronous objects (AutonomousDeleteExtra and AutonomousReplaceMissing).

**public void CancelAllAutonomous()**

cancels all currently running autonomous asynchronous objects (AutonomousDeleteExtra and AutonomousReplaceMissing).

**public RequestQueue GetRequestQueue()**

returns the current RequestQueue which handles which methods can send web requests currently.

### return deserialization object class types

        public class FileObj //The information returned from a file being read
        {
            public string blob_id;
            public string commit_id;
            public string content;
            public string content_sha256;
            public string encoding;
            public bool execute_filemode;
            public string file_name;
            public string file_path;
            public string last_commit_id;
            public string @ref;
            public int size;
        }

        public class RepositoryTreeObj // structure of the information GitLab returns when we ask for a repository tree.
        {
            public string id; //id of file/folder, doesn't seem to be available to use in get file requests.
            public string name; //name of the file/folder we are looking at
            public string type; //says if this is a tree or a blob object
            public string path; //path of the file/folder we are looking at
            public string mode; //file type e.g normal file, executable file, etc.
            
        }

        public class RepositoryCommitListObj // the objects that make up the commit list returned when we request a repository's commit list from GitLab. This contains the details of 1 commit.
        {
            public string author_email;
            public string author_name;
            public string authored_date;
            public string committed_date;
            public string committer_email;
            public string committer_name;
            public string created_at;
            public object extended_trailers; //git trailer objects
            public string id;
            public string message;
            public string[] parent_ids;
            public string short_id;
            public string title;
            public object trailers; //git trailer objects
            public string web_url;
        }

        public class UserObj //data from looking up at user. We retrieve the user's name from here.
        {
            public int id; 
            public string username;
            public string name; 
            public string state; 
            public bool locked;  
            public string avatar_url; 
            public string web_url;
            public string created_at;
            public string bio;
            public bool bot;
            public string location;
            public string email;
            public string public_email;
            public string linkedin;
            public string twitter;
            public string discord;
            public string github; 
            public string website_url;
            public string organization; 
            public string job_title;
            public string pronouns;
            public string work_information;
            public int followers;
            public int following; 
            public string local_time;
            public bool is_followed;
        }


        public class CommitObj
        {
            public string author_email;
            public string author_name;
            public string authored_date;
            public string committed_date;
            public string committed_email;
            public string committer_name;
            public string created_at;
            public string id;
            public PipelineObj last_pipeline;
            public string message;
            public string[] parent_ids;
            public string short_id;
            public Stats stats;
            public string status;
            public string title;
            public string web_url;
            
        }

        public class PipelineObj
        {
            public int id;
            public int iid;
            public int project_id;
            public string name; 
            public string sha;
            public string @ref;
            public string status;
            public string source;
            public string created_at;
            public string updated_at;
            public string web_url;
            public string before_sha;
            public bool tag;
            public string yaml_errors;
            public UserObj user;
            public string started_at;
            public string finished_at;
            public string committed_at;
            public int duration;
            public int queued_duration;
            public string coverage;
            public Detailed_Status detailed_status;
            public bool archived;
        }

        public class TestReportObj
        {
            public int total_time;
            public int total_count;
            public int success_count;
            public int failed_count;
            public int skipped_count;
            public int error_count;
            public JArray test_suites; //contains a list of testSuites JSON
        }
        public class TestSuites
        {
            public string name;
            public int total_time;
            public int total_count;
            public int success_count;
            public int failed_count;
            public int skipped_count;
            public int error_count;
            public JArray test_cases; //contains a list of testCases JSON
        }
        public class TestCases
        {
            public string status;
            public string name;
            public string classname;
            public int execution_time;
            public string system_output;
            public string stack_trace;
        }

        public class Detailed_Status
        {
            public string icon;
            public string text;
            public string label;
            public string group;
            public string tooltip;
            public bool has_details;
            public string details_path;
            public string illustration;
            public string favicon; //tab or bookmark icon

        }

        public class Stats //statistics about a single commit 
        {
            public int additions; //e.g number of additions in this commit
            public int deletions;
            public int total;
        }


        public class ResponseObj
        {
            public string responseMessage;
            public long responseCode;

            public ResponseObj(string message, long code)
            {
                responseMessage = message;
                responseCode = code;
            }
        }



