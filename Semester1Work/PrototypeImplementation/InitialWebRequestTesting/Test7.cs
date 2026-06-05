using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System;


public class Test7 : MonoBehaviour
{
    //bool returnReqFlag = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        callWeb("https://gitlab-student.macs.hw.ac.uk/api/v4/projects");
    }

    public void callWeb(string uri)
    { // for finding specific files, we need a URL-encoded path. / is replaced by %2F and . is replaced by %2E.
        using (UnityWebRequest webReq = UnityWebRequest.Get("https://gitlab-student.macs.hw.ac.uk/api/v4/projects/43029/repository/files/Info?ref=HEAD"))
        {
            //UnityWebRequestAsyncOperation waiter = webReq.SendWebRequest();
            webReq.SendWebRequest();
            //now we need to wait for the response

            //Debug.Log(webReq.downloadHandler.text); //text returns the data as a UTF8 string which is exactly what we need for the JSON parser
            //JsonConvert.DeserializeObject<FileReturn>(webReq.downloadHandler.text);
            // JsonSerializer j = new JsonSerializer();
            // j.Deserialize<FileReturn>(webReq.downloadHandler.text);
            FileReturn f = Newtonsoft.Json.JsonConvert.DeserializeObject<FileReturn>(webReq.downloadHandler.text);
            //FileReturn f = FileReturn.ReadJSON(webReq.downloadHandler.text);
            byte[] code = Convert.FromBase64String(f.content);
            UTF8Encoding u = new UTF8Encoding(); //we use UTF8 due to gitlab seeming to have a preference for it but ASCII would work here as well.
            string decoded = u.GetString(code);
            //string decoded = Convert.(code);
            //string decoded = BitConverter.ToString(code);
            Debug.Log(decoded);
            Debug.Log(webReq.responseCode);
            //returnReqFlag = false;
        }
    }
    private void trig(AsyncOperation op)
    {
        //returnReqFlag = true;
    }

    private IEnumerator Waiting()
    {
        yield return new WaitForSeconds(5);
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
