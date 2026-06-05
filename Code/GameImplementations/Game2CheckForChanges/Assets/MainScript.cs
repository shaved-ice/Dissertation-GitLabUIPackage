using UnityEngine;
using TMPro;
using System.Linq;
using GitLabUnityUI;
using System.Collections.Generic;

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
    private List<string> directories = new List<string>(); //a list of the folders/directories in the original repository. For randomising target file generation location.
    private string targetName = ""; //since our target file for the player to edit is randomly generated, we save the name and path 
    private string targetPath = "";
    private int currentLife = 0; //if this reaches 3 we know all lives are gone and the player has lost
    
    // -- flags to signal when processes are complete and the next one can start. -- 
    private bool gamestarted = false;
    private bool checkDone = true;
    private bool win = false;
    private GitLabUI package = new GitLabUI();
    private GitLabUI.RepositoryStateObj repoSaveState;
    // -- --


    // Update is called once per frame
    void Update()
    {
        if (gamestarted && checkDone && currentLife <3 && !win){ //if we started the game, the current repository check is complete, the player still has lives, and the player has not won yet
            checkDone = false; //set the flag to say the next check is not done
            checkWin(); //start the next check for if we won.
        }
        else if (win) //if the player won, go to the end page and display a win screen
        {
            package.CancelAllAutonomous(); //don't forget to cleanup the autos
            GamePage.SetActive(false);
            EndPage.SetActive(true);
            endText.text = "You won!";
        }
        else if (currentLife == 3) //if the player lost all their lives, go to the end page and display a lose screen
        {
            package.CancelAllAutonomous(); //don't forget to cleanup the autos
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

    //checkWin() checks for any lost lives, if the player has won, etc.
    private void checkWin() 
    {
        //first, lets check the repository tree structure and compare it to the old one
        string livesLostText = Mistake.text; //we add any new mistakes to the current mistake text so we need to get the string from it
        package.CheckForChanges(out List<string> filesAdded, out List<string> filesDeleted, out List<string> filesChanged, mainURL, projectCode, token, repoSaveState);
        while (filesAdded.Count > 0 && currentLife < 3)
        {
            package.DeleteFile(mainURL, projectCode, token, filesAdded[0]);
            filesAdded.Remove(filesAdded[0]); //remove the first file from the list
            livesLostText = livesLostText + "You added an extra file! You've lost a life.\n"; //add a mistake message and lose a life
            Lives[currentLife].SetActive(false);
            currentLife = currentLife + 1;
            if (currentLife == 3){ break;} //we can stop if we've already lost
        }
        while (filesDeleted.Count > 0 && currentLife < 3)
        {
            package.CreateFile(mainURL, projectCode, token, filesDeleted[0], repoSaveState.fileContents[repoSaveState.filePaths.IndexOf(filesDeleted[0])]); //content is grabbed by getting the content at the same index as the file path
            filesDeleted.Remove(filesDeleted[0]);
            livesLostText = livesLostText + "You deleted something! You've lost a life.\n"; //add a mistake message and lose a life
            Lives[currentLife].SetActive(false);
            currentLife = currentLife + 1;
            if (currentLife == 3){ break;} //we can stop if we've already lost
        }
        while (filesChanged.Count > 0 && currentLife < 3 && !win)
        {
            if (filesChanged[0] != targetPath){
                package.UpdateFile(mainURL, projectCode, token, filesChanged[0], repoSaveState.fileContents[repoSaveState.filePaths.IndexOf(filesChanged[0])]); //we get content by finding the index of the file path changed and looking for that same index in the repoSaveState contents list.
                filesChanged.Remove(filesChanged[0]);
                livesLostText = livesLostText + "You changed file contents! You've lost a life.\n"; //add a mistake message and lose a life
                Lives[currentLife].SetActive(false);
                currentLife = currentLife + 1;
                if (currentLife == 3) {break;} //we can stop if we've already lost
            }
            else
            {
                win = true;
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
        repoSaveState.AddFile(targetPath, "target");
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
                    package.SaveRepository(out repoSaveState, mainURL, projectCode, token);
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
