using UnityEngine;
using TestGitLabUIGameCreator;
using System.Collections;
using System;

public class TestingforPackage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TestGitLabUIGameCreator.TestGitLabUIClass tester = new TestGitLabUIGameCreator.TestGitLabUIClass();
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r = tester.ReadFile("https://gitlab-student.macs.hw.ac.uk", "43029", "Info");
        StartCoroutine(Waiter(r, 3, tester, "readFile"));
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r2 = tester.ReadTree("https://gitlab-student.macs.hw.ac.uk", "43029");
        StartCoroutine(WaitForFile(r2, "example", tester));
        TestGitLabUIGameCreator.TestGitLabUIClass.interactObj r3 = tester.CreateFile("https://gitlab-student.macs.hw.ac.uk", "43029", "NewFile", "new sentence for testing.", "glpat-Lmq6njT8pKXyM10tx700Jm86MQp1OjNqYgk.01.0z0e5vail");
        StartCoroutine(Waiter(r3, 1, tester, "createFile"));

    }

    IEnumerator Waiter(TestGitLabUIGameCreator.TestGitLabUIClass.interactObj reader, int callToRun, TestGitLabUIGameCreator.TestGitLabUIClass t, string name)
    {
        while (!reader.readingComplete)
        {
            yield return new WaitForSeconds(3);
        }
        Debug.Log(name + " contents: " + reader.contents);
        Debug.Log(name + " status: " + reader.StatusCode);
        if (callToRun < 2)
        {
            callToRun = callToRun + 1;
            TestGitLabUIGameCreator.TestGitLabUIClass.interactObj rNew = t.RewriteFile("https://gitlab-student.macs.hw.ac.uk", "43029", "ChangeMe", "changing text", "glpat-Lmq6njT8pKXyM10tx700Jm86MQp1OjNqYgk.01.0z0e5vail");
            StartCoroutine(Waiter(rNew, callToRun, t, "updateFile"));
        }
    }

    IEnumerator WaitForFile(TestGitLabUIGameCreator.TestGitLabUIClass.interactObj reader, string wantedFile,  TestGitLabUIGameCreator.TestGitLabUIClass tester)
    {
        while (!reader.readingComplete)
        {
            yield return new WaitForSeconds(3);
        }
        Debug.Log("reading repository: ");
        Boolean fileinside = false;
        foreach (string x in reader.contentFiles)
        {
            if (x.Equals(wantedFile))
            {
                fileinside = true;
            }
            Debug.Log("file: " + x);
        }
        if (!fileinside)
        {
            Debug.Log(wantedFile + " not found, trying again");
            yield return new WaitForSeconds(8);
            TestGitLabUIGameCreator.TestGitLabUIClass.interactObj newR = tester.ReadTree("https://gitlab-student.macs.hw.ac.uk", "43029");
            StartCoroutine(WaitForFile(newR, wantedFile, tester));
        }
        else
        {
            Debug.Log("file " + wantedFile + " found.");
        }
    }
}
