using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System;


public class PostTest1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(callWeb("https://gitlab-student.macs.hw.ac.uk/api/v4/projects"));
    }

    IEnumerator callWeb(string uri)
    { // for finding specific files, we need a URL-encoded path. / is replaced by %2F and . is replaced by %2E.
        Action a = new Action("create", "PutInfo", "Hello Hello Hello!!! This is a new file.");
        Action b = new Action("update", "Info", "This \tshould be inside \n the Info file. $$%%& ");
        Action[] c = new Action[2];
        c[0] = a;
        c[1] = b;
        Payload p = new Payload("main", "First commit to my test repository.", c);
        string jsonSubmit = Newtonsoft.Json.JsonConvert.SerializeObject(p);
        //string jsonSubmit = JsonUtility.ToJson(p);
        Debug.Log(jsonSubmit);
        
        using (UnityWebRequest webReq = UnityWebRequest.Post("https://gitlab-student.macs.hw.ac.uk/api/v4/projects/43029/repository/commits", jsonSubmit, "application/json"))
        {
            webReq.SetRequestHeader("PRIVATE-TOKEN", "glpat-Lmq6njT8pKXyM10tx700Jm86MQp1OjNqYgk.01.0z0e5vail");
            //Debug.Log(webReq.GetRequestHeader("PRIVATE-TOKEN"));
            yield return webReq.SendWebRequest();

            if (webReq.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(webReq.error);
            }
            else
            {
                Debug.Log("success?");
            }
        }
    }
}

public class Action
{
    public string action;
    public string file_path;
    public string content;

    public Action(string a, string f, string c)
    {
        action = a;
        file_path = f;
        content = c;
    }
}

public class Payload
{
    public string branch;
    public string commit_message;
    public Action[] actions;

    public Payload(string b, string c, Action[] a)
    {
        branch = b;
        commit_message = c;
        actions = a;
    }
}
