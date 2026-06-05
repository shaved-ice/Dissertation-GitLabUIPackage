using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text.Json;
using System.IO;
//using Newtonsoft.Json;
using Palmmedia.ReportGenerator.Core.Common;

public class Test4 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(callWeb("https://gitlab-student.macs.hw.ac.uk/api/v4/projects"));
    }

    IEnumerator callWeb(string uri)
    { // for finding specific files, we need a URL-encoded path. / is replaced by %2F and . is replaced by %2E.
        using (UnityWebRequest webReq = UnityWebRequest.Get("https://gitlab-student.macs.hw.ac.uk/api/v4/projects/43029/repository/files/Info?ref=HEAD"))
        {
            yield return webReq.SendWebRequest();
            //Debug.Log(webReq.downloadHandler.text); //text returns the data as a UTF8 string which is exactly what we need for the JSON parser
            //JsonConvert.DeserializeObject<FileReturn>(webReq.downloadHandler.text);
            // JsonSerializer j = new JsonSerializer();
            // j.Deserialize<FileReturn>(webReq.downloadHandler.text);
            FileReturn f = FileReturn.ReadJSON(webReq.downloadHandler.text);
            Debug.Log(webReq.downloadHandler.text);
            Debug.Log(f);
            Debug.Log("blob_id: " + f.blob_id);
            Debug.Log("commit_id: " + f.commit_id);
            Debug.Log("content: " + f.content);
            Debug.Log("content_sha256: " + f.content_sha256);
            Debug.Log("encoding: " + f.encoding);
            Debug.Log("execute_filemode: " + f.execute_filemode);
            Debug.Log("file_name: " + f.file_name);
            Debug.Log("file_path: " + f.file_path);
            Debug.Log("last_commit_id: " + f.last_commit_id);
            Debug.Log("reference: " + f.reference); //I'm considering manually replacing the ref: in the given json to allow me access to this
            Debug.Log("size: " + f.size);
        }
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

        public FileReturn() { }
        public static FileReturn ReadJSON(string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FileReturn>(s);
        }
    }
}
