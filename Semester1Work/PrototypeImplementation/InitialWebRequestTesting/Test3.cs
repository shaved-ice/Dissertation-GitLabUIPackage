using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Test3 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(callWeb("https://gitlab-student.macs.hw.ac.uk/api/v4/projects"));
    }

    IEnumerator callWeb(string uri)
    {
        using (UnityWebRequest webby = UnityWebRequest.Get("https://gitlab-student.macs.hw.ac.uk/api/v4/projects/43029"))
        {
           yield return webby.SendWebRequest();
           Debug.Log(webby.downloadHandler.text);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}