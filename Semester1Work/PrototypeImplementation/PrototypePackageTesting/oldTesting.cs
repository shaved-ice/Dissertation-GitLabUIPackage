using UnityEngine;
using TestGitLabUIGameCreator;
using System.Collections;
using System.Threading;
using System;

public class oldTesting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int callToRun = 0;
        TimeSpan timer = new TimeSpan(0, 0, 10);
        TestGitLabUIGameCreator.TestGitLabUIClass tester = new TestGitLabUIGameCreator.TestGitLabUIClass();
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r = tester.ReadFile("https://gitlab-student.macs.hw.ac.uk", "43029", "Info");
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r2 = tester.CreateFile("https://gitlab-student.macs.hw.ac.uk", "43029", "NewFile5", "writing one sentence in my file.", "glpat-Lmq6njT8pKXyM10tx700Jm86MQp1OjNqYgk.01.0z0e5vail");
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r3 = tester.RewriteFile("https://gitlab-student.macs.hw.ac.uk", "43029", "NewFile", "changing this file3", "glpat-Lmq6njT8pKXyM10tx700Jm86MQp1OjNqYgk.01.0z0e5vail");
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj[] arr = { r, r2 };
        StartCoroutine(waiter(r3, callToRun, arr, 0));
        StartCoroutine(waiter(r, callToRun, arr, 10));
        StartCoroutine(waiter(r2, callToRun, arr, 10));
    }

    IEnumerator waiter(TestGitLabUIGameCreator.TestGitLabUIClass.interactObj reader, int callToRun, TestGitLabUIGameCreator.TestGitLabUIClass.interactObj[] arr, int timeToSleep)
    {
        yield return new WaitForSeconds(3);
        Thread.Sleep(timeToSleep);
        if (reader.readingComplete)
        {
            Debug.Log(reader.contents);
            Debug.Log(reader.StatusCode);
            // Debug.Log(callToRun + " completed.");
            // Debug.Log(reader.StatusCode + " with " + callToRun);
            // callToRun = callToRun + 1;
            // if (callToRun < 3)
            // {
            //     Debug.Log("starting to run coroutine " + callToRun);
            //     StartCoroutine(waiter(arr[callToRun], callToRun, arr));
            // }
        }
        else
        {
            Debug.Log("wait");
            StartCoroutine(waiter(reader, callToRun, arr, 0));
        }
    }
}
