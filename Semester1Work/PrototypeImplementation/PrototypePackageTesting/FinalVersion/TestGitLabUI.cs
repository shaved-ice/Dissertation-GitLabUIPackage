using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System;

namespace TestGitLabUIGameCreator
{
    public class TestGitLabUIClass
    {
        private GameObject g = new GameObject("g", typeof(coroutineCaller));
        private coroutineCaller caller;

        public TestGitLabUIClass()
        {
            caller = g.AddComponent(typeof(coroutineCaller)) as coroutineCaller;
        }
        public class interactObj
        {
            public long StatusCode;
            public string contents;
            public string[] contentFiles;
            public bool readingComplete;

            public interactObj()
            {
                StatusCode = 0;
                contents = "";
                readingComplete = false;
            }

            public IEnumerator yieldReadFile(string mainURL, string projectCode, string fileName)
            { // for finding specific files, we need a URL-encoded path. / is replaced by %2F and . is replaced by %2E.
                using (UnityWebRequest webReq = UnityWebRequest.Get(mainURL + "/api/v4/projects/" + projectCode + "/repository/files/" + fileName + "?ref=HEAD"))
                {
                    yield return webReq.SendWebRequest();
                    FileReturn f = Newtonsoft.Json.JsonConvert.DeserializeObject<FileReturn>(webReq.downloadHandler.text);
                    byte[] code = Convert.FromBase64String(f.content + ""); 
                    UTF8Encoding u = new UTF8Encoding(); //we use UTF8 due to gitlab seeming to have a preference for it but ASCII would work here as well.
                    contents = u.GetString(code);
                    StatusCode = webReq.responseCode;
                    readingComplete = true;
                }
            }

            public IEnumerator yieldReadTree(string mainURL, string projectCode)
            {
                using (UnityWebRequest webReq = UnityWebRequest.Get(mainURL + "/api/v4/projects/" + projectCode + "/repository/tree" + "?ref=HEAD"))
                {
                    yield return webReq.SendWebRequest();
                    TreeObj[] treeArr = Newtonsoft.Json.JsonConvert.DeserializeObject<TreeObj[]>(webReq.downloadHandler.text);
                    contentFiles = new string[treeArr.Length];
                    int count = 0;
                    foreach (TreeObj t in treeArr)
                    {
                        contentFiles[count] = t.name;
                        count = count + 1;
                    }
                    StatusCode = webReq.responseCode;
                    readingComplete = true;
                }

            }

            public IEnumerator yieldCreateFile(string mainURL, string projectCode, string filePath, string contents, string privateToken)
            {
                Action a = new Action("create", filePath, contents);
                Action[] c = new Action[1];
                c[0] = a;
                Payload p = new Payload("main", "writing file to repository", c);
                string jsonSubmit = Newtonsoft.Json.JsonConvert.SerializeObject(p);

                using (UnityWebRequest webReq = UnityWebRequest.Post(mainURL + "/api/v4/projects/" + projectCode + "/repository/commits", jsonSubmit, "application/json"))
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", privateToken);
                    yield return webReq.SendWebRequest();
                    StatusCode = webReq.responseCode;
                    readingComplete = true;
                }
            }

            public IEnumerator yieldRewriteFile(string mainURL, string projectCode, string filePath, string contents, string privateToken)
            {
                Action a = new Action("update", filePath, contents);
                Action[] c = new Action[1];
                c[0] = a;
                Payload p = new Payload("main", "rewriting file contents", c);
                string jsonSubmit = Newtonsoft.Json.JsonConvert.SerializeObject(p);

                using (UnityWebRequest webReq = UnityWebRequest.Post(mainURL + "/api/v4/projects/" + projectCode + "/repository/commits", jsonSubmit, "application/json"))
                {
                    webReq.SetRequestHeader("PRIVATE-TOKEN", privateToken);
                    yield return webReq.SendWebRequest();
                    StatusCode = webReq.responseCode;
                    readingComplete = true;
                }
            }
        }

        public class coroutineCaller : MonoBehaviour
        {
            public void ReadFileCall(interactObj reader, string mainURL, string projectCode, string filePath)
            {
                StartCoroutine(reader.yieldReadFile(mainURL, projectCode, filePath));
            }
            public void ReadFileTree(interactObj reader, string mainURL, string projectCode)
            {
                StartCoroutine(reader.yieldReadTree(mainURL, projectCode));
            }
            public void CreateFileCall(interactObj reader, string mainURL, string projectCode, string filePath, string contents, string privateToken)
            {
                StartCoroutine(reader.yieldCreateFile(mainURL, projectCode, filePath, contents, privateToken));
            }
            public void RewriteFileCall(interactObj reader, string mainURL, string projectCode, string filePath, string contents, string privateToken)
            {
                StartCoroutine(reader.yieldRewriteFile(mainURL, projectCode, filePath, contents, privateToken));
            }
        }
        public interactObj ReadFile(string mainURL, string projectCode, string filePath)
        {
            interactObj fileRead = new interactObj();
            caller.ReadFileCall(fileRead, mainURL, projectCode, filePath);
            return fileRead;
        }
        public interactObj ReadTree(string mainURL, string projectCode)
        {
            interactObj treeRead = new interactObj();
            caller.ReadFileTree(treeRead, mainURL, projectCode);
            return treeRead;
        }
        public interactObj CreateFile(string mainURL, string projectCode, string filePath, string contents, string privateToken)
        {
            interactObj fileCreate = new interactObj();
            caller.CreateFileCall(fileCreate, mainURL, projectCode, filePath, contents, privateToken);
            return fileCreate;
        }
        public interactObj RewriteFile(string mainURL, string projectCode, string filePath, string contents, string privateToken)
        {
            interactObj fileCreate = new interactObj();
            caller.RewriteFileCall(fileCreate, mainURL, projectCode, filePath, contents, privateToken);
            return fileCreate;
        }
    }
    
    public class TreeObj
    {
        public string id;
        public string name;
        public string type;
        public string path;
        public string mode;
    }

    public class FileReturn
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
        public string reference;
        public int size;
    }
}
