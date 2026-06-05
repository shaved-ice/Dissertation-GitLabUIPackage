using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using GitLabUnityUI;

public class InputRepoButton : MonoBehaviour
{
    //We can have a maximum of 2 repositories but we use the same input field to recieve them both.
    //we will save the first repository in Host1, Code1, etc. and the second repository in Host2, Code2, Token2
    // the private repository details are static so we can keep them inbetween scenes as this game loads in different scenes. 
    public TMP_InputField Url; //gives us the hosts
    static private string host1;
    static private string host2;
    public TMP_InputField ProjectNum; //gives us the codes
    static private string code1;
    static private string code2;
    public TMP_InputField Auth; //gives us the tokens
    static private string token1;
    static private string token2;
    public TMP_Text Error; //text message to return error messages for invalid repositories
    public TMP_Text Instructions; //lets the user know what to do
    // boardContents holds the base/default board we want to write onto a file in GitLab for playing,
    private string boardContents = @"xxxxxxxxxxxxxx 
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx
xxxxxxxxxxxxxx";

    private Encoding enc = new UTF8Encoding();
    private GitLabUI package = new GitLabUI();
    private int repoNum = 0;
    void Start()
    {
        if (GameModel.IS_HUMAN) //GameModel.IS_HUMAN is true if we are playing in 2 player mode.
        {
            Instructions.text = "Please input the details of the repository you want player 2 to play in.";
        }
    }

    public void OnSubmitRepo()
    {
        validateRepo();
    }
    //get functions for the host details so we can get this information from outside this script.
    static public string GetHost1()
    {
        return host1;
    }
    static public string GetCode1()
    {
        return code1;
    }
    static public string GetToken1()
    {
        return token1;
    }
    static public string GetHost2()
    {
        return host2;
    }
    static public string GetCode2()
    {
        return code2;
    }
    static public string GetToken2()
    {
        return token2;
    }

    //validateRepo() checks that we can read, write, etc. and do the necessary actions in the repository that we need for the game using the details of the repository given to us
    void validateRepo()
    {
        string host = Url.text; //save the repository details from the text input fields so they can't be changed
        string code = ProjectNum.text;
        string token = Auth.text;   
        //check if the given inputs are strings (because of security standards)
        //we also check if the text of ProjectNum.text is an integer value - to do this we try to parse ProjectNum.text into a number, this value is returned in NumberValue but we don't need it. 
        //the parsing function returns true if it was successful or false if it was not.
        if (package.ValidateRepositoryDetails(Url.text, ProjectNum.text, Auth.text))
        {
            bool boardExists = false;
            bool validRepo = false;
            if (package.GetRepositoryTrees(out GitLabUI.RepositoryTreeObj[] treeArr, host, code, token) == GitLabUI.Response.Success) //we let recursive = false because we only care about the front page for this game
            {
                validRepo = true;
                foreach (GitLabUI.RepositoryTreeObj t in treeArr) // For simplcity we assume this does not change between submission and player starting the actual game
                    {
                        if (t.name == "Board") //if there is already a file named "Board" we will replace its contents instead of creating a board file.
                        {
                            boardExists = true;
                            break;
                        }
                    }
            }
            else
            {
                validRepo = false;
            }
            if (boardExists && validRepo)
            {
                if (package.GetFile(out GitLabUI.ResponseObj response, out GitLabUI.FileObj _, host, code, token, "Board") == GitLabUI.Response.Success)
                {
                    if (response.responseMessage.Contains("\"message\":\"404 File Not Found\"")){
                        boardExists = false;
                    }
                }
                else
                {
                    validRepo = false;
                }
            }
            if (!boardExists && validRepo)
            {
                if (package.CreateFile(host, code, token, "Board", boardContents, "adding Board to repo") != GitLabUI.Response.Success)
                {
                    validRepo = false;
                }
            }
            else if (boardExists && validRepo)
            {
                if (package.UpdateFile(out GitLabUI.ResponseObj response, host, code, token, "Board", boardContents, "resetting board", "main", true) != GitLabUI.Response.Success)
                {
                    validRepo = false;
                }
            }
            if (validRepo && repoNum == 0 && GameModel.IS_HUMAN) //if we successfully got a valid repo and we are playing against a human then the repo we just got is repository 2.
            {
                //we transfer the repo details to our repository 2 details and restart the process to find repository 1.
                host2 = host;
                code2 = code;
                token2 = token;
                Error.text = ""; //reset error text
                Instructions.text = "Please input the details of the repository you want player 1 to play in."; //new instructions for the player
                repoNum = 1;
            }
            else if ((validRepo && repoNum == 1 && GameModel.IS_HUMAN) || (!GameModel.IS_HUMAN && validRepo)) //if we got our second repository (so repository 1) for 2 player mode or the first repository for 1 player mode then we are finished and can move on.
            {
                //we transfer the repo details to our repository 1 details
                host1 = host;
                code1 = code;
                token1 = token;
                SceneManager.LoadScene("MainBoard", LoadSceneMode.Single); //we load the next scene
            }
            else
            {
                Error.text = "Invalid Repo";
            }
        }
        else
        {
            Error.text = "Invalid Repo";
        }
    }

}
