using UnityEngine;
using TMPro;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using GitLabUnityUI;


public class MainScript : MonoBehaviour
{
    public GameObject[] Lives = new GameObject[3]; //the game objects holding the hearts display of the player's lives (3 hearts)
    public GameObject StartPage; //the first page - the player simply presses start.
    public GameObject RepoInputPage; //the second page - the player inputs their repo for playing here.
    public TMP_InputField Url;
    public TMP_InputField ProjectNum;
    public TMP_InputField Auth;
    public TMP_Text RepoInputError;
    public GameObject RulesPage; //the third page - the rules are explained here
    public GameObject GamePage; //the fourth page - displays mistakes made and lives when the player is playing the game
    public GameObject EndPage; //the last page - displays a win or loss
    public TMP_Text endText; //tells the player if they won or lost
    public TMP_Text TargetInstruction; //tells the player what file they are supposed to change. (This will be a target file randomly generated into the repository by the game)
    public TMP_Text Mistake; //tells the player any mistakes they made that caused them to lose a life.
    private string mainURL; //the repository information we want to save - private copies prevent users from changing the information and breaking the game.
    private string projectCode;
    private string token;
    private bool isRepoValid = false; //says if the repository we are checking is valid
    private string[] repoTree; //same as treeArr but we only keep the paths (no files names, etc.)
    private List<string> filePaths = new List<string>(); //saved paths of only the files of the original repository - for checking changes to the repository
    private List<string> fileContents = new List<string>(); //saved contents of only the files of the original repository - for restoring changes to the repository
    private List<string> directories = new List<string>(); //a list of the folders/directories in the original repository. For randomising target file generation location.
    private string targetName = ""; //since our target file for the player to edit is randomly generated, we save the name and path 
    private string targetPath = "";
    private int currentLife = 0; //if this reaches 3 we know all lives are gone and the player has lost
    
    // -- flags to signal when processes are complete and the next one can start. -- 
    private bool gamestarted = false;
    private bool checkDone = true;
    private bool win = false;
    private GitLabUI package = new GitLabUI();
    // -- --
    //This is because if two coroutines send webrequests at the same time (with some small exceptions), it will likely cause an error in one of the web requests being returned.

    // Update is called once per frame
    void Update()
    {
        if (gamestarted && checkDone && currentLife <3 && !win){ //if we started the game, the current repository check is complete, the player still has lives, and the player has not won yet
            checkDone = false; //set the flag to say the next check is not done
            checkWin(); //start the next check for if we won.
        }
        else if (win) //if the player won, go to the end page and display a win screen
        {
            GamePage.SetActive(false);
            EndPage.SetActive(true);
            endText.text = "You won!";
        }
        else if (currentLife == 3) //if the player lost all their lives, go to the end page and display a lose screen
        {
            GamePage.SetActive(false);
            EndPage.SetActive(true);
            endText.text = "Game Over\nYou lose!";
        }
    }

    public void startGame() //activates when we press the start button on the first page
    {
        StartPage.SetActive(false); //stop displaying the start page
        RepoInputPage.SetActive(true); //display the repository input page
    }

    public void loadGame() // starts the game after the player presses begin on the rules page
    {
        addTarget(); //starts the game by first adding the target file 
    }

    public void submitRepo() //button on repository input page to check if the repository given is valid
    {
        //extract the text from the input boxes at the time of repository submission
        //check if the given inputs are strings (because of security standards)
        //we also check if the text of Number.text is an integer value - to do this we try to parse Number.text into a number, this value is returned in NumberValue but we don't need it. 
        //the parsing function returns true if it was successful or false if it was not.
        if (package.ValidateRepositoryDetails(Url.text, ProjectNum.text, Auth.text)){
            mainURL = Url.text;
            projectCode = ProjectNum.text;
            token = Auth.text;
            requestSend();
        }
        else
        {
            RepoInputError.text = "Invalid Repo - please try again"; //let the player know the repository details were invalid
        }
    }

    // checkMissing() checks if we are missing any files from the original repository
    private Stack<string> checkMissing(List<string> original, List<string> newList) 
    {
        Stack<string> result = new Stack<string>();     
        foreach (string s in original)
        {
            if (!newList.Contains(s)) //if the new list doesn't have a file the original does, then that file is missing and we return it in our list
            {
                result.Push(s);
            }
        }
        return result;
    }

    //checkExtra() checks if the new list contains any files the original repository doesn't have
    private Stack<string> checkExtra(List<string> original, List<string> newList)
    {
        Stack<string> result = new Stack<string>();
        foreach (string s in newList)
        {
            if (!original.Contains(s)) //if the new list has a file the original list doesn't have, then the file is an extra and we return it in our list
            {   
                result.Push(s);
            }
        }
        return result;
    }

    //checkWin() checks for any lost lives, if the player has won, etc.
    private void checkWin() 
    {
        //first, lets check the repository tree structure and compare it to the old one
        string livesLostText = Mistake.text; //we add any new mistakes to the current mistake text so we need to get the string from it
        List<string> newRepoTree = new List<string>();
        //we get the repository tree 
        //List repository tree request
        package.GetRepositoryTrees(out GitLabUI.RepositoryTreeObj[] treeArr, mainURL, projectCode, token, true);
        foreach (GitLabUI.RepositoryTreeObj t in treeArr) 
            {
                newRepoTree.Add(t.path); //generate a repoTree similar to the one we saved from the original repository structure.
            }
        List<string> newFilePaths = new List<string>();
        List<string> newFileContents = new List<string>(); 
        foreach (string path in newRepoTree){ //for every item in the new repoTree, request the file so we can get the file contents (we exclude folders/directories)
            //Get file request
            package.GetFile(out GitLabUI.ResponseObj response, out GitLabUI.FileObj f, mainURL, projectCode, token, path);
            if (!response.responseMessage.Contains("\"message\":\"404 File Not Found\"")){ //this message appears in the text when we look up a non-file object
                string decoded = "";
                newFilePaths.Add(path);
                if (f.content != null){
                    byte[] code = Convert.FromBase64String(f.content);
                    UTF8Encoding u = new UTF8Encoding();
                    decoded = u.GetString(code);
                }
                newFileContents.Add(decoded); //if we confirmed this path is a file, add the contents to the new file contents
            }
        }
        //now that we have the contents and paths of both the original and new repositories, we check to see what has been deleted, added, or changed.
        Stack<string> filesToDelete = checkExtra(filePaths, newFilePaths); //we need to delete any extra files
        Stack<string> filesToReplace = checkMissing(filePaths, newFilePaths); //we need to add any files missing from the original
        while(filesToDelete.Count != 0 && currentLife < 3) //delete extra files
        {   
            livesLostText = livesLostText + "You added an extra file! You've lost a life.\n"; //add a mistake message and lose a life
            Lives[currentLife].SetActive(false);
            currentLife = currentLife + 1;
            if (currentLife == 3){ break;} //we can stop if we've already lost
            string deletePath = filesToDelete.Pop();
            //Delete file request
            package.DeleteFile(mainURL, projectCode, token, deletePath, "removing extra files");
        }
        while(filesToReplace.Count != 0 && currentLife < 3) //add missing files
        {
            livesLostText = livesLostText + "You deleted something! You've lost a life.\n"; //add a mistake message and lose a life
            Lives[currentLife].SetActive(false);
            currentLife = currentLife + 1;
            if (currentLife == 3){ break;} //we can stop if we've already lost
            //get the path and contents of the file we need to replace
            string replacePath = filesToReplace.Pop();
            int replaceIndex = filePaths.IndexOf(replacePath);
            string replaceContent = fileContents[replaceIndex];
            //then send a create file request to GitLab
            //Create file request
            package.CreateFile(mainURL, projectCode, token, replacePath, replaceContent, "replacing previous files");

        }

        // now we should check for any changed file contents.
        foreach(string path in newFilePaths) //for every file in our new repository (which should be structured the same as the original due to the deleting and replacing we did above)
        {
            if (currentLife == 3) {break;} //we can stop if we've already lost
            if (filePaths.Contains(path))  //for every file in both trees
            {
                int indexOld = filePaths.IndexOf(path);
                int indexNew = newFilePaths.IndexOf(path);
                //use the index to get the contents of both files and compare them UNLESS it is the target file since that is intended to be changed
                if (fileContents[indexOld] != newFileContents[indexNew] && path != targetPath) 
                {
                    livesLostText = livesLostText + "You changed file contents! You've lost a life.\n"; //add a mistake message and lose a life
                    Lives[currentLife].SetActive(false);
                    currentLife = currentLife + 1;
                    if (currentLife == 3) {break;} //we can stop if we've already lost
                    // make a request to replace the changed file contents with the original file contents
                    package.UpdateFile(mainURL, projectCode, token, path, fileContents[indexOld], "fixing file contents");
                }
                //if the file that had its contents changed was the target file, let the update function know the player won.
                else if (fileContents[indexOld] != newFileContents[indexNew] && path == targetPath)
                {
                    win = true;
                }
            }
        }
        Mistake.text = livesLostText; //update the mistake text on the page
        checkDone = true; //let update know we finished the check.

    }

    //addTarget() adds the target file to be changed by the player into the repository.
    private void addTarget() 
    {
        System.Random rand = new System.Random();
        int randomNumber = rand.Next(0,directories.Count); //choose a random directory/folder to put our target file in
        targetPath = directories[randomNumber] + targetName; //create the target path from our directory path and chosen target file name
        //we need to put this information in this formData package to add it to our web request.
        //in the below web request we ask GitLab to create our target file in the chosen path inside the repository in the above branch, with the above commit message. The file will say "target" inside.
        //Create file request
        package.CreateFile(mainURL, projectCode, token, targetPath, "target", "creating target");
        filePaths.Add(targetPath); //add targetPath to our original repository saved structure so it is not detected as a change in the mistakes.
        fileContents.Add("target");
        RulesPage.SetActive(false); //stop displaying the rules page
        GamePage.SetActive(true); //start displaying the game page.
        foreach (GameObject h in Lives) //set each heart in the health bar to be visible 
        {
            h.SetActive(true);
        }
        TargetInstruction.text = "Please change the contents of the file named: " + targetName; //tell the player what they have to do.
        gamestarted = true; //tell the update function the game has started
    }

    //saveFileContents() saves the file contents of the original repository so we can check for changes later
    private void saveFileContents() 
    {   
        foreach (string path in repoTree){ //for every file in the repository tree
            //Get file request
            package.GetFile(out GitLabUI.ResponseObj response, out GitLabUI.FileObj f, mainURL, projectCode, token, path);
            if (!response.responseMessage.Contains("\"message\":\"404 File Not Found\""))
            {
                filePaths.Add(path); //we want to save all the filepaths of the files in the repository so we can check they still exist later.
                string decoded = "";
                if (f.content != null){
                    byte[] code = Convert.FromBase64String(f.content);
                    UTF8Encoding u = new UTF8Encoding();
                    decoded = u.GetString(code);
                }
                fileContents.Add(decoded); //we also save the contents so we can check the contents.
            }
        }
        //we activate the next page once this is complete to avoid players starting the game before we are ready
    }

    //requestSend() checks if the repository is valid and if it is, calls saveFileContents()
    private void requestSend()
    {
        bool invalidRepo = false;
        //get the original repoTree list (a data structure holding the details of the path, name etc. of every file/folder in the repository)
        //List repository tree request
        if (package.GetRepositoryTrees(out GitLabUI.RepositoryTreeObj[] treeArr, mainURL, projectCode, token, true) == GitLabUI.Response.Success){
            repoTree = new string[treeArr.Length];
            int count = 0;
            foreach (GitLabUI.RepositoryTreeObj t in treeArr) // For simplcity we assume this does not change between submission and player starting the actual game
            {
                repoTree[count] = t.path;
                count = count + 1;
            }
        }
        else
        {
            invalidRepo = true; //if unsuccessful then let the rest of the script know
        }

        bool newTarget = false;
        int targetNum = 0;
        string[] splitPath;
        if (!invalidRepo){ //if we have been successful so far then we need to find a name for our target file
            while (!newTarget) // code to find a name for the file we will create because we want it to be unique in the repository. 
            // if the repository already contains a file named "Target", look for a "Target1", "Target2", etc. until one that isn't in the repository is found. 
            {
                if (targetNum == 0)
                {
                    targetName = "Target";
                }
                else
                {
                    targetName = "Target" + targetNum.ToString(); 
                }
                bool matchTargetName = false;
                foreach (string p in repoTree)
                {
                    splitPath = p.Split("/"); //split a path e.g folder1/file1 into [folder1, file1]
                    if (splitPath.Last() == targetName)
                    {
                        matchTargetName = true; //this flag lets us know we found a match and we can't use this file name
                    }
                }
                if (matchTargetName)
                {
                    targetNum = targetNum + 1;
                }
                else
                {
                    newTarget = true; //if we found a new target we can move on to the next part of this function
                }
            }
            string directoryPath;
            int partOfStringToRemove;
            //to randomise what folder our file is created in, we need a list of all possible folders/directories
            foreach (string p in repoTree) //for every file look at its path and see if it is inside a new directory
            {
                splitPath = p.Split("/");
                if (splitPath.Length > 1) //this means it is inside a directory
                {   
                    // we want to remove the "/filename" section from the path so we take the path length and remove the file name length as well as the extra "/" before it
                    partOfStringToRemove = p.Length - splitPath[splitPath.Length - 1].Length;
                    directoryPath = p.Remove(partOfStringToRemove); //removes everything in and after the position partOfStringToRemove
                    if (!directories.Contains(directoryPath)) //if this is a new directory, add it to our list of unique directories.
                    {
                        directories.Add(directoryPath);
                    }
                }
            }
            directories.Add(""); //this directory accounts for if we enter no folders and just place it in the initial repository page.
            bool successfulCreate = false;
            //we need to test our ability to write a file so we create a practice target file with our target name in the initial repository page.
            //Create file request
            if (package.CreateFile(mainURL, projectCode, token, targetName, "target", "testing repo validity") == GitLabUI.Response.Success)
            {
                successfulCreate = true;
            }
            else
            {
                successfulCreate = false;
                isRepoValid = false;
            }
            if (successfulCreate) // if the create file request was successful, we should clean up and delete that file. 
            {
                //Delete file request
                if (package.DeleteFile(mainURL, projectCode, token, targetName, "testing repo validity (cleanup)") == GitLabUI.Response.Success)
                {
                    isRepoValid = true;
                }
                else
                {
                    isRepoValid = false;
                }
            }
            if (isRepoValid) //if everything went well and repository is valid we can move on
                {
                    saveFileContents();
                    RepoInputPage.SetActive(false);
                    RulesPage.SetActive(true);
                }
            else
                {
                    RepoInputError.text = "Invalid Repo - please try again"; //if the repository is invalid in any way, let the user know
                }
        }
        else
        {
            RepoInputError.text = "Invalid Repo - please try again"; //if the repository is invalid in any way, let the user know 
        }
    }
}
