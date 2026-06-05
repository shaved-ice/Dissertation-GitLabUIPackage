using UnityEngine;
using UnityEngine.Networking;


public class Test1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UnityWebRequest webby = UnityWebRequest.Get("https://www.my-server.com");
        Debug.Log(webby.downloadHandler.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
