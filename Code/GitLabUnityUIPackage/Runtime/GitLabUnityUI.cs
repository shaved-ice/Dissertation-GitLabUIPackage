using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace GitLabUnityUI
{
    public class GitLabUI
    {
        public enum Response{
            Success, 
            Error,
            InvalidDetails,
            Cancelled
        }

        public enum ProjectCodeType
        {
            Integer,
            Path,
            EncodedPath
        }
        private Encoding encoder = new UTF8Encoding();
        public Dictionary<string, Dictionary<string, List<AutonomousDeleteExtra>>> AutoDeleteCatalogue = new Dictionary<string, Dictionary<string, List<AutonomousDeleteExtra>>>();
        public Dictionary<string, Dictionary<string, List<AutonomousReplaceMissing>>> AutoReplaceCatalogue = new Dictionary<string, Dictionary<string, List<AutonomousReplaceMissing>>>();
        //package size restriction values. Maximum sizes of these headers/Urls
        public int URISize = 150;
        public int hostUrlSize = 40;
        public int projectCodeSize = 20;
        public int tokenSize = 60;
        public int commitMessageSize = 200;
        public int contentSize = 300;
        public int branchSize = 20;
        public int refSize = 15;
        public int filePathSize = 50;
        public int issueTitleSize = 20;
        public int commitIdSize = 50;
        public int issueDescriptionSize = 200;
        public int personalAccessTokenSize = 60;
        public int dateSize = 40;
        public int millisecondsToWait = 100;
        public int pipelineIdSize = 50;
        private RequestQueue requestQueue;

        public GitLabUI()
        {
            requestQueue = new RequestQueue();
        }

        //This package contains mostly abstractions of the GitLab API Requests.
        //They ask for permission to send requests on the requestQueue, send their requests, wait for reponses and return information accordingly.
        //There are different overloads for different request types changing things like input parameters or asking for more out parameters.

        public bool CheckElementInPath(RepositoryTreeObj fileObj, string element, bool standaloneElement=false, bool caseSensitive = false)
        {
            string path = fileObj.path;
            if (!caseSensitive)
            {
                element = element.ToLower();
                path = path.ToLower();
            }
            string[] segmentedPath = path.Split("/"); //splits up the file path
            if (!standaloneElement) //if we only care for it containing the element e.g bedroom1 counts for an input of "bedroom"
            {
                return Array.Exists(segmentedPath, segment => segment.Contains(element));
            }
            else //if we care for it being the element e.g bedroom1 does NOT count for an input of "bedroom" but bedroom does.
            {
                return Array.Exists(segmentedPath, segment => segment.Equals(element));
            }
        }

        public bool CheckElementInPath(string filePath, string element, bool standaloneElement=false, bool caseSensitive = false)
        {
            string path = filePath;
            if (!caseSensitive)
            {
                element = element.ToLower();
                path = path.ToLower();
            }
            string[] segmentedPath = path.Split("/"); //splits up the file path
            if (!standaloneElement) //if we only care for it containing the element e.g bedroom1 counts for an input of "bedroom"
            {
                return Array.Exists(segmentedPath, segment => segment.Contains(element));
            }
            else //if we care for it being the element e.g bedroom1 does NOT count for an input of "bedroom" but bedroom does.
            {
                return Array.Exists(segmentedPath, segment => segment.Equals(element));
            }
        }

        public string GetFileName(RepositoryTreeObj fileObj)
        {
            return fileObj.name;
        }

        public string GetFileName(string filePath) //returns the file name from the file path given
        {
            string[] pathPieces = filePath.Split("/");
            return pathPieces[pathPieces.Length - 1]; 
        }

        public bool CheckForFileAtLocation(string hostUrl, string projectCode, string token, string filePath, string repoRef = "HEAD", bool excludeDirectories = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer) //checks for a file in a repository at a given location
        {
            Response response = GetFile(out ResponseObj extraResponse, out FileObj _, hostUrl, projectCode, token, filePath, repoRef, projectCodeType);
            if (response == Response.InvalidDetails || response == Response.Error)
            {
                return false;
            }
            if (excludeDirectories && extraResponse.responseMessage.Contains("\"message\":\"404 File Not Found\"")) //returns false if the file is found but is a directory AND we specified to excludeDirectories
            {
                return false;
            }
            return true; //returns true if the file is found
                
        }

        public bool CheckForFile(string hostUrl, string projectCode, string token, string fileName, string repoRef = "HEAD", bool excludeDirectories = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            //get repository structure so we can see all the names of files and directories
            Response response = GetRepositoryTrees(out ResponseObj _, out RepositoryTreeObj[] repoTree, hostUrl, projectCode, token, true, repoRef, projectCodeType);
            if (response == Response.InvalidDetails || response == Response.Error)
            {
                return false;
            }
            foreach(RepositoryTreeObj t in repoTree)
            {
                if (t.name == fileName) //if we have found a file that matches the file name given
                {
                    if (excludeDirectories) //if excludeDirectories then check if it is a file and not a directory
                    {
                        Response response2 = GetFile(out ResponseObj extraResponse2, out FileObj _, hostUrl, projectCode, token, t.path, repoRef, projectCodeType);
                        if (response2 == Response.Success && !extraResponse2.responseMessage.Contains("\"message\":\"404 File Not Found\""))
                        {
                            return true; //if it is a file then return true, if it is not a file then keep looking
                        }
                    }
                    else
                    {
                        return true; //return true if we don't care about if it is a directory
                    }
                }
            }
            return false; //return false if nothing is found
        }

        public Response CheckFileChanged(out string state, string hostUrl, string projectCode, string token, string filePath, string originalfileContent, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer) //check if a file changed
        {
            if (!waitForPermission("save repository state at " + hostUrl + " in project:" + projectCode))
            {
                state = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, originalfileContent, "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                state = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath) + "?ref=" + repoRef;
            if (uri.Length > URISize)
            {
                state = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri)) //get file request
            {
                if (token != "")
                    {
                        webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                    }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (originalfileContent != Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text).content)
                    {
                        state = "contents changed";
                    }
                    else
                    {
                        state = "unchanged";
                    }
                }
                else
                {
                    state = "deleted/not accessible";
                }
            }
            requestQueue.Dequeue();
            return Response.Success;
        }

        public Response CheckForChanges(out List<string> filesAdded, out List<string> filesDeleted, out List<string> filesChanged, string hostUrl, string projectCode, string token, RepositoryStateObj originalLayout, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer) //check if a repository changed
        {
            if (!waitForPermission("save repository state at " + hostUrl + " in project:" + projectCode))
            {
                filesChanged = null;
                filesDeleted = null;
                filesAdded = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                filesChanged = null;
                filesDeleted = null;
                filesAdded = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            RepositoryTreeObj[] treeArr;
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + "&recursive=true";
            if (uri.Length > URISize)
            {
                filesChanged = null;
                filesDeleted = null;
                filesAdded = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri)) //get repository structure tree
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        treeArr = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        treeArr = null;
                    }
                }
                else
                {
                    filesChanged = null;
                    filesDeleted = null;
                    filesAdded = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
            List<string> newFilePaths = new List<string>();
            List<string> newFileContents = new List<string>();
            foreach(RepositoryTreeObj tree in treeArr) //for each file in the tree, save it in the new file lists if it is a file
            {
                uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(tree.path) + "?ref=" + repoRef;
                if (uri.Length > URISize)
                {
                    filesChanged = null;
                    filesDeleted = null;
                    filesAdded = null;
                    requestQueue.Dequeue();
                    return Response.InvalidDetails;
                }
                using (UnityWebRequest webReq = UnityWebRequest.Get(uri)) //get request to check if it is a file or not
                {
                    if (token != "")
                    {
                        webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                    }
                    webReq.SendWebRequest();
                    waitForRequestCompletion(webReq);
                    if (webReq.responseCode == 200 && !webReq.downloadHandler.text.Contains("\"message\":\"404 File Not Found\""))
                    {
                        newFilePaths.Add(tree.path);
                        if(webReq.downloadHandler.text != null){ //encoder.GetString(Convert.FromBase64String(newContent))
                            string c = Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text).content;
                            newFileContents.Add(encoder.GetString(Convert.FromBase64String(c)));
                        }
                        else
                        {
                            newFileContents.Add("");
                        }
                        
                    }
                }
            }
            //we made a list of files because directories are automatically included when you delete/add files inside them
            //first check for extra files
            filesAdded = new List<string>();
            foreach(string p in newFilePaths) 
            {
                if (!originalLayout.filePaths.Contains(p)){
                    filesAdded.Add(p); //if the new repository has files the original didn't, then we know it was added
                }
            }
            List<string> filesToCheck = new List<string>(); //list of paths that have not been deleted so we should check their contents
            filesDeleted = new List<string>();
            foreach(string p2 in originalLayout.filePaths)
            {
                if (!newFilePaths.Contains(p2))
                {
                    filesDeleted.Add(p2); //if the old repository had files the new one doesn't, then we know it was deleted
                }
                else
                {
                    filesToCheck.Add(p2); //if nothing changed, then we should check the contents for content changes
                }
            }
            int indexInOriginal;
            int indexInNew;
            filesChanged = new List<string>();
            foreach(string p3 in filesToCheck)
            {
                indexInOriginal = originalLayout.filePaths.IndexOf(p3);
                indexInNew = newFilePaths.IndexOf(p3);
                if (newFileContents[indexInNew] != originalLayout.fileContents[indexInOriginal]) //if the new contents don't match the old contents, then we know the contents changed
                {
                    filesChanged.Add(p3);
                }
            }
            requestQueue.Dequeue();
            return Response.Success;
        }

        public Response SaveRepository(out RepositoryStateObj contents, string hostUrl, string projectCode, string token, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            //In this method, we will manually recreate other methods to remove unecessary repeition of variable validation etc.
            if (!waitForPermission("save repository state at " + hostUrl + " in project:" + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            //get repository structure tree
            RepositoryTreeObj[] paths;
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + "&recursive=true";
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        paths = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                        requestQueue.Dequeue();
                        return Response.Success;
                    }
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
            RepositoryStateObj returnVal = new RepositoryStateObj();
            string filePath;
            string fileContent;
            FileObj file;
            foreach (RepositoryTreeObj treeObj in paths) //save the file paths and file contents of the file paths that are confirmed to be files and not directories (because directories are automatically added or removed when the files inside are added or removed)
            {
                filePath = treeObj.path;
                uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath) + "?ref=" + repoRef;
                if (uri.Length > URISize)
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.InvalidDetails;
                }
                using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
                {
                    if (token != "")
                    {
                        webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                    }
                    webReq.SendWebRequest();
                    waitForRequestCompletion(webReq);
                    if (webReq.responseCode == 200 && !webReq.downloadHandler.text.Contains("\"message\":\"404 File Not Found\""))
                    {
                        if(webReq.downloadHandler.text != null){
                            file = Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text);
                            fileContent = file.content;
                            fileContent = encoder.GetString(Convert.FromBase64String(fileContent));
                        }
                        else
                        {
                            fileContent = "";
                        }
                        returnVal.AddFile(filePath, fileContent);
                    }
                }
            }
            contents = returnVal;
            requestQueue.Dequeue();
            return Response.Success;
            
        }

        public Response GetUser(out UserObj contents, string hostUrl, string personalAccessToken)
        {
            if (!waitForPermission("Get User from personalAccessToken"))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!(personalAccessToken is string) || ((personalAccessToken.Length > personalAccessTokenSize) && personalAccessTokenSize != -1) || !(ValidateRequestDetails(hostUrl, "", "", "", "", "", "")))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/user";
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (personalAccessToken != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", personalAccessToken);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<UserObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }
        public Response GetUser(out ResponseObj response, out UserObj contents, string hostUrl, string personalAccessToken)
        {
            if (!waitForPermission("Get User from personalAccessToken"))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!(personalAccessToken is string) || ((personalAccessToken.Length > personalAccessTokenSize) && personalAccessTokenSize != -1) || !(ValidateRequestDetails(hostUrl, "", "", "", "", "", "")))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/user";
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (personalAccessToken != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", personalAccessToken);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<UserObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }
        //takes in string for pipelineId
        public Response GetPipelineUnitTestReport(out TestReportObj contents, string hostUrl, string projectCode, string token, string pipelineId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get PipelineTestReport from pipeline ID at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!((pipelineId is string) && (int.TryParse(pipelineId, out int throwaway))) || ((pipelineId.Length > pipelineIdSize) && pipelineIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/pipelines/" + pipelineId + "/test_report";
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<TestReportObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }
        //takes in int for pipeline Id
        public Response GetPipelineUnitTestReport(out TestReportObj contents, string hostUrl, string projectCode, string token, int pipelineId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get PipelineTestReport from pipeline ID at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            //ID is given as int so we don't need to check
            if (((pipelineId.ToString().Length > pipelineIdSize) && pipelineIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/pipelines/" + pipelineId + "/test_report";
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<TestReportObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetPipelineUnitTestReport(out ResponseObj response, out TestReportObj contents, string hostUrl, string projectCode, string token, string pipelineId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get PipelineTestReport from pipeline ID at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!((pipelineId is string) && (int.TryParse(pipelineId, out int throwaway))) || ((pipelineId.Length > pipelineIdSize) && pipelineIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/pipelines/" + pipelineId + "/test_report";
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<TestReportObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetPipelineUnitTestReport(out ResponseObj response, out TestReportObj contents, string hostUrl, string projectCode, string token, int pipelineId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get PipelineTestReport from pipeline ID at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            //ID is given as int so we don't need to check
            if (((pipelineId.ToString().Length > pipelineIdSize) && pipelineIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/pipelines/" + pipelineId + "/test_report";
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<TestReportObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetCommit(out CommitObj contents, string hostUrl, string projectCode, string token, string commitId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get a commit from commitId at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!(commitId is string) || ((commitId.Length > commitIdSize) && commitIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "","") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/commits/" + commitId;
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<CommitObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetCommit(out ResponseObj response, out CommitObj contents, string hostUrl, string projectCode, string token, string commitId, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get a commit from commitId at " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!(commitId is string) || ((commitId.Length > commitIdSize) && commitIdSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "","") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/commits/" + commitId;
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<CommitObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetRepositoryCommits(out RepositoryCommitListObj[] contents, string hostUrl, string projectCode, string token, string repoRef=null, string startDate = null, string endDate = null, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get Repository Commits from " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            string ref_temp = repoRef;
            string tempStart = startDate;
            string tempEnd = endDate;
            if (repoRef == null)
            {
                ref_temp = "";
            }
            if (startDate == null)
            {
                tempStart = "";
            }
            if (endDate == null)
            {
                tempEnd = "";
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", ref_temp) || !(ValidateProjectCode(projectCode, projectCodeType)) || (tempStart.Length > dateSize && dateSize != -1) || !(startDate is string || startDate == null) || (tempEnd.Length > dateSize && dateSize != -1) || !(endDate is string || endDate == null))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/commits/";
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                if (repoRef != null){
                    webReq.SetRequestHeader("ref_name", repoRef);
                }
                if (startDate != null)
                {
                    webReq.SetRequestHeader("since", startDate);
                }
                if (endDate != null)
                {
                    webReq.SetRequestHeader("until", endDate);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        JArray commitList = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(webReq.downloadHandler.text);
                        contents = commitList.ToObject<RepositoryCommitListObj[]>();
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetRepositoryCommits(out ResponseObj response, out RepositoryCommitListObj[] contents, string hostUrl, string projectCode, string token, string repoRef=null, string startDate = null, string endDate = null, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Get Repository Commits from " + hostUrl + " in project " + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            string ref_temp = repoRef; //refTemp is only for use in validating request details
            string tempStart = startDate;
            string tempEnd = endDate;
            if (repoRef == null)
            {
                ref_temp = "";
            }
            if (startDate == null)
            {
                tempStart = "";
            }
            if (endDate == null)
            {
                tempEnd = "";
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", ref_temp) || !(ValidateProjectCode(projectCode, projectCodeType)) || (tempStart.Length > dateSize && dateSize != -1) || !(startDate is string) || (tempEnd.Length > dateSize && dateSize != -1) || !(endDate is string))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/commits/";
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                if (repoRef != null){
                    webReq.SetRequestHeader("ref_name", repoRef);
                }
                if (startDate != null)
                {
                    webReq.SetRequestHeader("since", startDate);
                }
                if (endDate != null)
                {
                    webReq.SetRequestHeader("until", endDate);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null)
                    {
                        JArray commitList = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(webReq.downloadHandler.text);
                        contents = commitList.ToObject<RepositoryCommitListObj[]>();
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response UpdateFile(string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="updating file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("update file in " + hostUrl + " , project:" + projectCode))
            {
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, content, commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData;
            if (encodeContents)
            {
                formData = "{ \"branch\": \""+branch+"\", \"commit_message\": \""+commitMessage+"\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(encoder.GetBytes(content)) + "\"}";
            }
            else
            {
                formData = "{ \"branch\": \""+branch+"\", \"commit_message\": \""+commitMessage+"\", \"content\": \"" + content + "\"}";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Put(uri, formData))
            {
                webReq.uploadHandler.contentType = "application/json";
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response UpdateFile(out ResponseObj response, string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="updating file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("update file in " + hostUrl + " , project:" + projectCode))
            {
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, content, commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData;
            if (encodeContents)
            {
                formData = "{ \"branch\": \""+branch+"\", \"commit_message\": \""+commitMessage+"\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(encoder.GetBytes(content)) + "\"}";
            }
            else
            {
                formData = "{ \"branch\": \""+branch+"\", \"commit_message\": \""+commitMessage+"\", \"content\": \"" + content + "\"}";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Put(uri, formData))
            {
                webReq.uploadHandler.contentType = "application/json";
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }
        public Response GetRepositoryTrees(out RepositoryTreeObj[] contents, string hostUrl, string projectCode, string token, bool recursive = false, string repoRef="HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("getRepositoryTree of " + hostUrl + " , project:" + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string recursiveText = "";
            if (recursive)
            {
                recursiveText = "&recursive=true";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + recursiveText;
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetRepositoryTrees(out ResponseObj response, out RepositoryTreeObj[] contents, string hostUrl, string projectCode, string token, bool recursive = false, string repoRef="HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("getRepositoryTree of " + hostUrl + " , project:" + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, "", "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string recursiveText = "";
            if (recursive)
            {
                recursiveText = "&recursive=true";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + recursiveText;
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response CreateIssue(string hostUrl, string projectCode, string token, string title, string description="", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Create Issue " + title + " in " + hostUrl + " , project:" + projectCode))
            {
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            string formData = "{ \"title\": \""+title+"\", \"description\": \"" + description + "\"}";
            if (!(title is string) || ((title.Length > issueTitleSize) && issueTitleSize != -1) || !(description is string) || ((description.Length > issueDescriptionSize) && issueDescriptionSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/issues";
            if (uri.Length > URISize)
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Post(uri, formData, "application/json"))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 201)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response CreateIssue(out ResponseObj response, string hostUrl, string projectCode, string token, string title, string description="", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("Create Issue " + title + " in " + hostUrl + " , project:" + projectCode))
            {
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!(title is string) || ((title.Length > issueTitleSize) && issueTitleSize != -1) || !(description is string) || ((description.Length > issueDescriptionSize) && issueDescriptionSize != -1) || !ValidateRequestDetails(hostUrl, token, "", "", "", "", "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData = "{ \"title\": \""+title+"\", \"description\": \"" + description + "\"}";
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/issues";
            if (uri.Length > URISize)
            {
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Post(uri, formData, "application/json"))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 201)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response DeleteFile(string hostUrl, string projectCode, string token, string filePath, string commitMessage="deleting file", string branch = "main", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("delete item at filePath in " + hostUrl + " , project:" + projectCode))
            {
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, "", commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\"}";
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Delete(uri)){
                if (token != "")
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                byte[] data = encoder.GetBytes(formData);
                webReq.uploadHandler = new UploadHandlerRaw(data); 
                webReq.uploadHandler.contentType = "application/json";
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200 || webReq.responseCode == 204)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response DeleteFile(out ResponseObj response, string hostUrl, string projectCode, string token, string filePath, string commitMessage="deleting file", string branch = "main", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("delete item at filePath in " + hostUrl + " , project:" + projectCode))
            {
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, "", commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\"}";
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Delete(uri)){
                if (token != "")
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                byte[] data = encoder.GetBytes(formData);
                webReq.uploadHandler = new UploadHandlerRaw(data); 
                webReq.uploadHandler.contentType = "application/json";
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 200 || webReq.responseCode == 204)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetFile(out FileObj contents, string hostUrl, string projectCode, string token, string filePath, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("get item at filePath in " + hostUrl + " , project:" + projectCode))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath) + "?ref=" + repoRef;
            if (uri.Length > URISize)
            {
                contents = null;
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != "")
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if(webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text);
                    }
                    else
                    {
                        contents = null;
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response GetFile(out ResponseObj response, out FileObj contents, string hostUrl, string projectCode, string token, string filePath, string repoRef = "HEAD", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("get file in " + hostUrl + " , project:" + projectCode))
            {
                contents = null;
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, "", "", "", repoRef) || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                contents = null;
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath) + "?ref=" + repoRef;
            if (uri.Length > URISize)
            {
                contents = null;
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
            {
                if (token != "")
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 200)
                {
                    if (webReq.downloadHandler.text != null){
                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text);
                        response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                    }
                    else
                    {
                        contents = null;
                        response = new ResponseObj(webReq.downloadHandler.text + " code given but downloadHandler text was null", webReq.responseCode);
                    }
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    contents = null;
                    requestQueue.Dequeue();
                    response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                    return Response.Error;
                }
            }
        }


        public Response CreateFile(string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="create file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("create file in " + hostUrl + " , project:" + projectCode))
            {
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, content, commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData;
            if (encodeContents)
            {
                formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(encoder.GetBytes(content)) + "\"}";
            }
            else 
            {
                formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\", \"content\": \"" + content + "\"}";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Post(uri, formData, "application/json"))
            {
                if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                if (webReq.responseCode == 201)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public Response CreateFile(out ResponseObj response, string hostUrl, string projectCode, string token, string filePath, string content, string commitMessage="create file", string branch="main", bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            if (!waitForPermission("create file in " + hostUrl + " , project:" + projectCode))
            {
                response = new ResponseObj("this method was cancelled from the request queue", 0);
                requestQueue.Dequeue();
                return Response.Cancelled;
            }
            if (!ValidateRequestDetails(hostUrl, token, filePath, content, commitMessage, branch, "") || !(ValidateProjectCode(projectCode, projectCodeType)))
            {
                response = new ResponseObj("invalid details were given", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            string formData;
            if (encodeContents)
            {
                formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(encoder.GetBytes(content)) + "\"}";
            }
            else 
            {
                formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\", \"content\": \"" + content + "\"}";
            }
            string uri = hostUrl + "/api/v4/projects/" + EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(filePath);
            if (uri.Length > URISize)
            {
                response = new ResponseObj("URI was too large", 0);
                requestQueue.Dequeue();
                return Response.InvalidDetails;
            }
            using (UnityWebRequest webReq = UnityWebRequest.Post(uri, formData, "application/json"))
            {
            if (token != ""){
                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                }
                webReq.SendWebRequest();
                waitForRequestCompletion(webReq);
                response = new ResponseObj(webReq.downloadHandler.text, webReq.responseCode);
                if (webReq.responseCode == 201)
                {
                    requestQueue.Dequeue();
                    return Response.Success;
                }
                else
                {
                    requestQueue.Dequeue();
                    return Response.Error;
                }
            }
        }

        public bool ValidateRepositoryDetails(string hostUrl, string projectCode, string token = "", ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {   
            return checkType(hostUrl, token, "", "", "", "", "") && checkLength(hostUrl, token, "", "", "", "", "") && ValidateProjectCode(projectCode, projectCodeType);
        }

        private bool ValidateRequestDetails(string hostUrl, string token, string filePath, string content, string commitMessage, string branch, string repoRef)
        {
            return checkType(hostUrl, token, filePath, content, commitMessage, branch, repoRef) && checkLength(hostUrl, token, filePath, content, commitMessage, branch, repoRef);
        }

        private bool ValidateProjectCode(string projectCode, ProjectCodeType type) //validates project code depending on the type
        {
            //if project code is not a string OR (project code is too big AND projectCodeSize is not set to -1)
            if (!(projectCode is string || (projectCode.Length > projectCodeSize && projectCodeSize != -1)))
            {
                return false;
            }
            if (type == ProjectCodeType.Integer)
            {
                if (int.TryParse(projectCode, out int throwaway))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else //this is for encoded / normal path but it acts like a catch-all
            {
                return true;
            }
        }
        private string EncodeProjectCode(string projectCode, ProjectCodeType type) //encodes project code depending on the type
        {
            if (type == ProjectCodeType.Path)
            {
                return HttpUtility.UrlEncode(projectCode);
            }
            else //this is for encoded path but it acts like a catch-all
            {
                return projectCode;
            }
        }

        private bool checkLength(string hostUrl, string token, string filePath, string content, string commitMessage,  string branch, string repoRef)
        {
            return ((hostUrl.Length <= hostUrlSize || hostUrlSize == -1)
            && (token.Length <= tokenSize || tokenSize == -1)
            && (filePath.Length <= filePathSize || filePathSize == -1)
            && (content.Length <= contentSize || contentSize == -1)
            && (commitMessage.Length <= commitMessageSize || commitMessageSize == -1)
            && (branch.Length <= branchSize || branchSize == -1)
            && (repoRef.Length <= refSize || refSize == -1));
        }

        private bool checkType(string hostUrl, string token, string filePath, string content, string commitMessage, string branch, string repoRef)
        {
            return((hostUrl is string) && (token is string) && 
            (filePath is string) && (content is string) && (commitMessage is string) && 
            (branch is string) && (repoRef is string)); 
        }
        private void waitForRequestCompletion(UnityWebRequest web) //manual wait for the web request to complete 
        {
            while (!web.isDone) 
            {
                Thread.Sleep(millisecondsToWait);
            }
            return;
        }

        private bool waitForPermission(string identity) 
        {
            RequestID id = requestQueue.Enqueue(identity); //submit permission request to queue 
            while (requestQueue.Peek() != id.getIDNumber() && requestQueue.HasId(id) == true) //wait for this to have permission to send web requests
            {
                Thread.Sleep(millisecondsToWait);
            }
            return requestQueue.HasId(id);
            //return to place this was called when we gain permission.
            //dequeue permission queue to give up permission
        }

        public AutonomousDeleteExtra createAutonomousDelete(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalLayout, string commitMessage = "deleting added file",  string branch = "main", string repoRef = "HEAD", int secondsBetweenChecks = 3, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            return new AutonomousDeleteExtra(hostUrl, projectCode, token, package, originalLayout, AutoDeleteCatalogue, commitMessage, branch, repoRef, secondsBetweenChecks, projectCodeType);
        }

        public AutonomousReplaceMissing createAutonomousReplace(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalLayout, string replaceCommitMessage = "replacing missing file", string updateCommitMessage = "updating file contents",  string branch = "main", string repoRef = "HEAD", int secondsBetweenChecks = 3, bool encodeContents = false, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
        {
            return new AutonomousReplaceMissing(hostUrl, projectCode, token, package, originalLayout, AutoReplaceCatalogue, replaceCommitMessage, updateCommitMessage, branch, repoRef, encodeContents, secondsBetweenChecks, projectCodeType);
        }

        private void cleanUpDeleteCatalogue() //removes all unneeded entries from the delete "running" catalogue
        {
            foreach(var hosts in AutoDeleteCatalogue)
                {
                    foreach(var projects in hosts.Value)
                    {
                        if (projects.Value.Count == 0 || projects.Value == null)
                        {
                            hosts.Value.Remove(projects.Key);
                        }
                    }
                    if (hosts.Value.Count == 0 || hosts.Value == null)
                    {
                        AutoDeleteCatalogue.Remove(hosts.Key);
                    }
                }
        }
        private string StringifyDeleteCatalogue() //returns string version oo the delete "running" catalogue
            {
                string print = "Extra files being deleted at:";
                foreach(var hosts in AutoDeleteCatalogue)
                {
                    print = print + hosts.Key + "[";
                    foreach(var projects in hosts.Value)
                    {
                        print = print + projects.Key + "[";
                        foreach(var AutoDelete in projects.Value)
                        {
                            print = print + AutoDelete + ",";
                        }
                        print.Remove(print.Length-1);
                        print = print + "],";
                    }
                    print.Remove(print.Length-1);
                    print = print + "],";
                }
                print.Remove(print.Length-1);
                return print;
            }

        private string StringifyReplaceCatalogue() //returns string version oo the replace "running" catalogue
        {
            string print = "Missing files being replaced at:";
                foreach(var hosts in AutoReplaceCatalogue)
                {
                    print = print + hosts.Key + "[";
                    foreach(var projects in hosts.Value)
                    {
                        print = print + projects.Key + "[";
                        foreach(var AutoDelete in projects.Value)
                        {
                            print = print + AutoDelete + ",";
                        }
                        print.Remove(print.Length-1);
                        print = print + "],";
                    }
                    print.Remove(print.Length-1);
                    print = print + "],";
                }
                print.Remove(print.Length-1);
                return print;
        }


        private void cleanUpReplaceCatalogue() //removes all unneeded entries from the replace "running" catalogue
        {
            foreach(var hosts in AutoReplaceCatalogue)
                {
                    foreach(var projects in hosts.Value)
                    {
                        if (projects.Value.Count == 0 || projects.Value == null)
                        {
                            hosts.Value.Remove(projects.Key);
                        }
                    }
                    if (hosts.Value.Count == 0 || hosts.Value == null)
                    {
                        AutoReplaceCatalogue.Remove(hosts.Key);
                    }
                }
        }
        public Dictionary<string, Dictionary<string, List<AutonomousDeleteExtra>>>  GetDeleteCatalogue()
        {
            return AutoDeleteCatalogue;
        }
        public Dictionary<string, Dictionary<string, List<AutonomousReplaceMissing>>> GetReplaceCatalogue()
        {
            return AutoReplaceCatalogue;
        }
        
        
        public string GetStringAutoDeleteCatalogue() //returns cleaned version of delete catalogue as a string
        {
            cleanUpDeleteCatalogue();
            return StringifyDeleteCatalogue();
        }

        public string GetStringAutoReplaceCatalogue() //returns cleaned version of replace catalogue as a string
        {
            cleanUpReplaceCatalogue();
            return StringifyReplaceCatalogue();
        }

        public string CheckAnyAutonomous()
        {
            // we only need to clean up the catalogues when presenting them to the user as the system would not get confused by the extra mess.
            return ("Delete Catalogue: " + GetStringAutoDeleteCatalogue() + " Replace Catalogue: " + GetStringAutoReplaceCatalogue());
        }

        public RequestQueue GetRequestQueue(){
            return requestQueue;
        }



        public void CancelAllAutonomous() //clears all running catalogues so they know to stop running
        {
            AutoDeleteCatalogue.Clear();
            AutoReplaceCatalogue.Clear();
        }

        public void DebugLogQueue()
        {
            Debug.Log(requestQueue.getQueueAsString());
        }
        //object classes to deserialize the returns of web requests
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

        public class RequestQueue //having the queue in an object like this makes it easier to display it to users
        {
            private int nextID;
            private int firstID;
            private Queue<RequestID> queue;
            private object mxLock;
            public RequestQueue()
            {
                mxLock = new object();
                nextID = 0;
                firstID = int.MaxValue;
                queue = new Queue<RequestID>();
            }
            public int getNextID()
            {
                return nextID;
            }
            public int Peek()
            {
                return queue.Peek().getIDNumber();
            }
            public Queue<RequestID> getQueue()
            {
                return queue;
            }

            public string getQueueAsString()
            {
                lock (mxLock)
                {
                    string returnVal = "Queue: ";
                    foreach (RequestID r in queue)
                    {
                        returnVal = returnVal + r.getIDAsString() + " ,";
                    }
                    return returnVal;
                }
            }
            public RequestID Enqueue(string identityMessage)
            {
                lock (mxLock) //makes sure only one thread can enqueue permission at a time
                {
                    if (nextID == firstID)
                    {
                        throw new InvalidOperationException("number of requests waiting in queue exceeded range of integer IDs available - are you sure your requests are completing?");
                    }
                    else
                    {
                        RequestID returnVal = new RequestID(identityMessage, nextID);
                        queue.Enqueue(returnVal); //enqueues next permission ID into request queue
                        if (nextID == int.MaxValue) //increment nextID for the next time this method is called.
                        {
                            nextID = 0;
                        }
                        else
                        {
                            nextID = nextID + 1;
                        }
                        return returnVal;
                    }
                }
            }
            public void Dequeue()
            {
                lock (mxLock) //makes sure only one thread can dequeue permission at a time
                {
                    if (firstID == int.MaxValue)
                    {
                        firstID = 0;
                    }
                    else
                    {
                        firstID = firstID + 1;
                    }
                    queue.Dequeue();
                }
            }

            public void Clear()
            {
                queue.Clear();
            }
            
            public bool HasId(RequestID id)
            {
                return queue.Contains(id);
            }
        }

        public class RequestID
        {
            private int id;
            private string identity;
            public RequestID(string idMessage, int IdNumber)
            {
                identity = idMessage;
                id = IdNumber;
            }
            public int getIDNumber()
            {
                return id;
            }
            public string getIDMessage()
            {
                return identity;
            }
            public string getIDAsString()
            {
                return "ID:"+id.ToString() + "|" + identity;
            }
        }

        public class RepositoryStateObj //holds all the files (and their contents) in a repository (except for directories as they are added or removed when the files inside them are added or removed)
        {
            public List<string> filePaths;
            public List<string> fileContents;

            public RepositoryStateObj()
            {
                filePaths = new List<string>();
                fileContents = new List<string>();
            }

            public RepositoryStateObj(List<string> paths, List<string> contents)
            {
                filePaths = new List<string>(paths);
                fileContents = new List<string>(contents);
            }

            public void AddFile(string filePath, string fileContent)
            {
                filePaths.Add(filePath);
                fileContents.Add(fileContent);
            }

            public bool CheckValid()
            {
                if(filePaths.Count != fileContents.Count)
                {
                    return false;
                }
                return true;
            }
        }

        public class AutonomousDeleteExtra //checks if any files have been added from an original state and deletes them if they have
        {
            private RepositoryStateObj originalRepositoryState;
            private string hostUrl;
            private string projectCode;
            private string branch;
            private string commitMessage;
            private ProjectCodeType projectCodeType;
            private string token;
            private string repoRef;
            private string formData;
            private int secondsBetweenChecks;
            public List<string> filepathsDeleted;
            private GitLabUI package;
            public bool working;
            public string notWorkingReason = "";
            private List<string> newPathList = new List<string>();
            
            public Dictionary<string, Dictionary<string, List<AutonomousDeleteExtra>>> AutoDeleteCatalogue;

            public AutonomousDeleteExtra(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalState, Dictionary<string, Dictionary<string, List<AutonomousDeleteExtra>>> catalogue, string commitMessage = "deleting added file", string branch = "main", string repoRef = "HEAD", int secondsBetweenChecks = 3, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
            {
                this.hostUrl = hostUrl;
                this.projectCode = projectCode;
                this.projectCodeType = projectCodeType;
                this.token = token;
                this.package = package;
                this.repoRef = repoRef;
                this.secondsBetweenChecks = secondsBetweenChecks;
                this.branch = branch;
                this.commitMessage = commitMessage;
                AutoDeleteCatalogue = catalogue;
                originalRepositoryState = originalState;
                if (this.package.ValidateRequestDetails(hostUrl, token, "", "", commitMessage, branch, repoRef) && this.package.ValidateProjectCode(projectCode, projectCodeType))
                {
                    AddingNew();
                    formData =  "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + commitMessage + "\"}";
                    filepathsDeleted = new List<string>();
                    working = true;
                    CheckExtra().Forget();
                }
                else
                {
                    formData = null;
                    working = false;
                    notWorkingReason = "invalid repository details given";
                }
            }

            public string GetAutoAsString() //for easier debugging
            {
                if (this.checkExists())
                {
                    return "Delete Auto for " + hostUrl + " at project " + projectCode + ", index " + AutoDeleteCatalogue[hostUrl][projectCode].IndexOf(this);
                }
                else
                {
                    return "Delete Auto for " + hostUrl + " at project " + projectCode + " is not currently running";
                }
            }

            public void Stop() //removes this from the "running" catalogue which stops the async functionality
            {
                try
                {
                    AutoDeleteCatalogue[hostUrl][projectCode].RemoveAll(x => x == this);
                }
                catch {}
            }

            private bool checkExists() //checks if this is in the "running" catalogue
            {
                try
                {
                    if (AutoDeleteCatalogue[hostUrl][projectCode].IndexOf(this) == -1)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            private void AddingNew() //adds this to the "running" catalogue
            {
                bool failed = false;
                if (!AutoDeleteCatalogue.TryGetValue(hostUrl, out Dictionary<string, List<AutonomousDeleteExtra>> val1)){
                    Dictionary<string, List<AutonomousDeleteExtra>> dictionary2 = new Dictionary<string, List<AutonomousDeleteExtra>>();
                    AutoDeleteCatalogue.Add(hostUrl, dictionary2);
                    failed = true;
                }
                if (!AutoDeleteCatalogue[hostUrl].TryGetValue(projectCode, out List<AutonomousDeleteExtra> val2) || failed)
                {
                    List<AutonomousDeleteExtra> list = new List<AutonomousDeleteExtra>();
                    AutoDeleteCatalogue[hostUrl].Add(projectCode, list);
                }
                AutoDeleteCatalogue[hostUrl][projectCode].Add(this);
            }

            private async UniTaskVoid CheckExtra() //async functionality that keeps running.
            {
                if (working && checkExists() && !Application.exitCancellationToken.IsCancellationRequested){
                    RequestID id = package.requestQueue.Enqueue("checking for extra files at " + hostUrl + " in: " + projectCode);
                    await UniTask.WaitUntil(() => (package.requestQueue.Peek() == id.getIDNumber()) || (package.requestQueue.HasId(id) == false));
                    if (package.requestQueue.HasId(id) == true){
                        RepositoryTreeObj[] contents = null;
                        string uriGetTree = hostUrl + "/api/v4/projects/" + package.EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + "&recursive=true";
                        if (uriGetTree.Length > package.URISize)
                        {
                            working = false;
                            notWorkingReason = "Get repository tree uri length went over limit";
                        }
                        //Get repository structure tree
                        else {
                            using (UnityWebRequest webReq = UnityWebRequest.Get(uriGetTree))
                            {   
                                if (token != ""){
                                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                }
                                try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                catch {}
                                if (webReq.responseCode == 200)
                                {
                                    if (webReq.downloadHandler.text != null){
                                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                                    }
                                }
                                else
                                {
                                    working = false;
                                    notWorkingReason = "get repository tree request was was not successful";
                                }
                            }
                        }
                        if (working){
                            newPathList.Clear();
                            string uri;
                            foreach (RepositoryTreeObj t in contents) //save the file paths that lead to files (not directories)
                            {
                                uri = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) +"/repository/files/" + HttpUtility.UrlEncode(t.path) + "?ref=HEAD";
                                if (uri.Length > package.URISize)
                                {
                                    working = false;
                                    notWorkingReason = "Get file uri length went over limit";
                                    break;
                                }
                                else {
                                    using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
                                    {
                                        if (token != "")
                                        {
                                            webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                        }
                                        try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                        catch {}
                                        if (webReq.responseCode == 200 && !webReq.downloadHandler.text.Contains("\"message\":\"404 File Not Found\""))
                                        {
                                            newPathList.Add(t.path);
                                        }
                                    }
                                }
                            }
                            if (newPathList.Count != 0 && working){ //if it is empty then we know for a fact that there is nothing to delete
                                foreach(string path in newPathList)
                                {
                                    if (!originalRepositoryState.filePaths.Contains(path) && working) //if this path wasn't in the original repository then we delete it
                                    {   
                                        uri = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(path);
                                        if (uri.Length > package.URISize)
                                        {
                                            working = false;
                                            notWorkingReason = "delete uri length went over limit";
                                            break;
                                        }
                                        else {
                                            using (UnityWebRequest webReq = UnityWebRequest.Delete(uri))
                                            {
                                                if (token != "")
                                                {
                                                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                                }
                                                byte[] data = package.encoder.GetBytes(formData);
                                                webReq.uploadHandler = new UploadHandlerRaw(data); 
                                                webReq.uploadHandler.contentType = "application/json";
                                                try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                                catch {}
                                                if (webReq.responseCode == 200 || webReq.responseCode == 204)
                                                {
                                                    filepathsDeleted.Add(path);
                                                }
                                                else
                                                {
                                                    working = false;
                                                    notWorkingReason = "delete file request was not successful";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (working) //start the method again after a delay
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(secondsBetweenChecks));
                        package.requestQueue.Dequeue();
                        CheckExtra().Forget();
                    }
                    else
                    {
                        package.requestQueue.Dequeue();
                        Stop();
                    }
                }
            }
        }
        public class AutonomousReplaceMissing //constantly checks for changes from a given original state and creates files to replace deleted ones or updates files to have their old contents.
        {
            private RepositoryStateObj originalRepositoryState;
            private string hostUrl;
            private string projectCode;
            private ProjectCodeType projectCodeType;
            private string token;
            private string replaceCommitMessage;
            private string updateCommitMessage;
            private string repoRef;
            private string branch;
            private int secondsBetweenChecks;
            private bool encodeContents;
            public List<string> filepathsReplaced;
            public List<string> filepathsUpdated;
            private GitLabUI package;
            public bool working;
            public string notWorkingReason = "";
            private List<string> newPathList = new List<string>();
            private List<string> newContentList = new List<string>();
            
            public Dictionary<string, Dictionary<string, List<AutonomousReplaceMissing>>> AutoReplaceCatalogue;
            //creates files to replace missing ones from an original repository state
            //also updates files to fix contents from an original repository state
            public AutonomousReplaceMissing(string hostUrl, string projectCode, string token, GitLabUI package, RepositoryStateObj originalState, Dictionary<string, Dictionary<string, List<AutonomousReplaceMissing>>> catalogue, string replaceCommitMessage = "replacing added file", string updateCommitMessage = "updating file contents", string branch = "main", string repoRef = "HEAD", bool encodeContents = false, int secondsBetweenChecks = 3, ProjectCodeType projectCodeType = ProjectCodeType.Integer)
            {
                this.hostUrl = hostUrl;
                this.projectCode = projectCode;
                this.projectCodeType = projectCodeType;
                this.token = token;
                this.package = package;
                this.encodeContents = encodeContents;
                this.repoRef = repoRef;
                this.branch = branch;
                this.replaceCommitMessage = replaceCommitMessage;
                this.updateCommitMessage = updateCommitMessage;
                this.secondsBetweenChecks = secondsBetweenChecks;
                AutoReplaceCatalogue = catalogue;
                originalRepositoryState = originalState;
                if (this.package.ValidateRequestDetails(hostUrl,  token, "", "", replaceCommitMessage, branch, repoRef) && this.package.ValidateProjectCode(projectCode, projectCodeType) && updateCommitMessage is string && updateCommitMessage.Length <= package.commitMessageSize)
                {
                    AddingNew(); //adds it to the "running" catalogue
                    filepathsReplaced = new List<string>(); //lists of all file paths that are replaced or updated
                    filepathsUpdated = new List<string>();
                    working = true; //if this is ever false we stop the functionality of this auto replacer
                    CheckMissing().Forget(); //starts the async thread for this
                }
                else
                {
                    working = false;
                    notWorkingReason = "invalid repository details given";
                }
            }

            public string GetAutoAsString() //for easier debugging
            {
                if (this.checkExists())
                {
                    return "Replace Auto for " + hostUrl + " at project " + projectCode + ", index " + AutoReplaceCatalogue[hostUrl][projectCode].IndexOf(this);
                }
                else
                {
                    return "Replace Auto for " + hostUrl + " at project " + projectCode + " is not currently running";
                }
            }
            public void Stop() //removes this from the "running" catalogue which the async functionality checks to know if it should be running still
            {
                try
                {
                    AutoReplaceCatalogue[hostUrl][projectCode].RemoveAll(x => x == this);
                }
                catch {}
            }

            private bool checkExists() //checks if this is in the "running" catalogue
            {
                try
                {
                    if (AutoReplaceCatalogue[hostUrl][projectCode].IndexOf(this) == -1)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            private void AddingNew() //adds this to the "running" catalogue
            {
                bool failed = false;
                if (!AutoReplaceCatalogue.TryGetValue(hostUrl, out Dictionary<string, List<AutonomousReplaceMissing>> _)){
                    AutoReplaceCatalogue.Add(hostUrl, new Dictionary<string, List<AutonomousReplaceMissing>>());
                    failed = true;
                }
                if (!AutoReplaceCatalogue[hostUrl].TryGetValue(projectCode, out List<AutonomousReplaceMissing> _) || failed)
                {
                    AutoReplaceCatalogue[hostUrl].Add(projectCode, new List<AutonomousReplaceMissing>());
                }
                AutoReplaceCatalogue[hostUrl][projectCode].Add(this);
            }

            private async UniTaskVoid CheckMissing() //the async functionality of this auto replacer
            {
                if (working && checkExists() && !Application.exitCancellationToken.IsCancellationRequested){
                    RequestID id = package.requestQueue.Enqueue("checking for missing files at " + hostUrl + " in: " + projectCode);
                    await UniTask.WaitUntil(() => package.requestQueue.Peek() == id.getIDNumber() || (package.requestQueue.HasId(id) == false)); //wait until this has the permission in the permission queue
                    if (package.requestQueue.HasId(id) == true) {
                        RepositoryTreeObj[] contents = {};
                        string uriGetTree = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) + "/repository/tree" + "?ref=" + repoRef + "&recursive=true";
                        if (uriGetTree.Length > package.URISize)
                        {
                            working = false;
                            notWorkingReason = "Get repository tree uri length went over limit";
                        }
                        else {
                            using (UnityWebRequest webReq = UnityWebRequest.Get(uriGetTree)) //get the repository structure tree
                            {
                                if (token != ""){
                                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                }
                                try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                catch {}
                                if (webReq.responseCode == 200)
                                {

                                    if (webReq.downloadHandler.text != null){
                                        contents = Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryTreeObj[]>(webReq.downloadHandler.text);
                                    }
                                }
                                else
                                {
                                    working = false;
                                    notWorkingReason = "get repository tree request was was not successful";
                                }
                            }
                        }
                        newPathList.Clear();
                        newContentList.Clear();
                        string newContent;
                        //extract the paths from each RepositoryTreeObj to make a new file list
                        foreach(RepositoryTreeObj t in contents) //for each file, check if it is a file or a directory and save the file paths and file contents of only the files
                        {
                            string uri = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) +"/repository/files/" + HttpUtility.UrlEncode(t.path) + "?ref=HEAD";
                            if (uri.Length > package.URISize)
                            {
                                working = false;
                                notWorkingReason = "Get file uri length went over limit";
                            }
                            else {
                                using (UnityWebRequest webReq = UnityWebRequest.Get(uri))
                                {
                                    if (token != "")
                                    {
                                        webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                    }
                                    try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                    catch {}
                                    if (webReq.responseCode == 200 && !webReq.downloadHandler.text.Contains("\"message\":\"404 File Not Found\""))
                                    {
                                        newPathList.Add(t.path);
                                        if (webReq.downloadHandler.text != null)
                                        {
                                            newContent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileObj>(webReq.downloadHandler.text).content;
                                            newContentList.Add(package.encoder.GetString(Convert.FromBase64String(newContent)));
                                        }
                                        else
                                        {
                                            newContentList.Add("");
                                        }
                                    }
                                }
                            }
                        }
                        if (contents != null && working)
                        {
                            bool replaced = false;
                            int indexOfPath;
                            foreach(string path in originalRepositoryState.filePaths) //for each file path in the original state
                            {
                                replaced = false;
                                indexOfPath = originalRepositoryState.filePaths.IndexOf(path);
                                if (!newPathList.Contains(path) && working) //if the repository doesn't contain the original file, we must replace it
                                {   
                                    string formData;
                                    if (encodeContents) //optional encode contents
                                    {
                                        formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + replaceCommitMessage + "\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(package.encoder.GetBytes(originalRepositoryState.fileContents[indexOfPath])) + "\"}";
                                    }
                                    else 
                                    {
                                        formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + replaceCommitMessage + "\", \"content\": \"" + originalRepositoryState.fileContents[indexOfPath] + "\"}";
                                    }
                                    string uri = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(path);
                                    if (uri.Length > package.URISize)
                                    {
                                        working = false;
                                        notWorkingReason = "Create file uri length went over limit";
                                    }
                                    else {
                                        using (UnityWebRequest webReq = UnityWebRequest.Post(uri, formData, "application/json"))
                                        {
                                            if (token != ""){
                                                webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                            }
                                            try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                            catch {}
                                            if (webReq.responseCode != 201)
                                            {
                                                working = false;
                                                notWorkingReason = "unable to create files to replace missing ones";
                                            }
                                            else
                                            {
                                                filepathsReplaced.Add(path);
                                                replaced = true;
                                            }
                                        }
                                    }
                                }
                                int index;
                                int index2;
                                if (!replaced && working)
                                {
                                    index = newPathList.IndexOf(path);
                                    index2 = originalRepositoryState.filePaths.IndexOf(path);
                                    if (newContentList[index] != originalRepositoryState.fileContents[index2]) //if the new contents don't match the old contents then update the file with the old contents
                                    {
                                        string formData;
                                        if (encodeContents) 
                                        {
                                            formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + updateCommitMessage + "\", \"encoding\": \"base64\", \"content\": \"" + Convert.ToBase64String(package.encoder.GetBytes(originalRepositoryState.fileContents[indexOfPath])) + "\"}";
                                        }
                                        else 
                                        {
                                            formData = "{ \"branch\": \"" + branch + "\", \"commit_message\": \"" + updateCommitMessage + "\", \"content\": \"" + originalRepositoryState.fileContents[indexOfPath] + "\"}";
                                        }
                                        string uri = hostUrl + "/api/v4/projects/" + this.package.EncodeProjectCode(projectCode, projectCodeType) + "/repository/files/" + HttpUtility.UrlEncode(path);
                                        if (uri.Length > package.URISize)
                                        {
                                            working = false;
                                            notWorkingReason = "Update file uri length went over limit";
                                        }
                                        else {
                                            using (UnityWebRequest webReq = UnityWebRequest.Put(uri, formData))
                                            {
                                                if (token != "")
                                                {
                                                    webReq.SetRequestHeader("PRIVATE-TOKEN", token);
                                                }
                                                webReq.uploadHandler.contentType = "application/json";
                                                try {await webReq.SendWebRequest().WithCancellation(Application.exitCancellationToken);}
                                                catch {}
                                                if (webReq.responseCode != 200)
                                                {
                                                    working = false;
                                                    notWorkingReason = "unable to update files";
                                                }
                                                else
                                                {
                                                    filepathsUpdated.Add(path);
                                                }
                                            }
                                        }
                                    }
                                }

                            }

                        }
                    }
                    if (working) //start the method again after a delay
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(secondsBetweenChecks));
                        package.requestQueue.Dequeue();
                        CheckMissing().Forget();
                    }
                    else
                    {
                        package.requestQueue.Dequeue(); 
                        Stop();
                    }
                }
            }
        }
    }
}
