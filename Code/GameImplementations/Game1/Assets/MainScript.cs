using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GitLabUnityUI;

public class MainScript : MonoBehaviour
{   
    //public variables are provided from the Unity scene to the script. We don't have to create these but we can interact with them.
    public GameObject Avatar; //the character avatar
    public Sprite CharacterSprite; //sprites for the character avatar to display.
    public Sprite LoseSprite;
    public Sprite WinSprite;
    public GameObject StartPage; //the first page - the player simply presses start.
    public GameObject RepoInputPage; //the second page - the player inputs their repo for playing here.
    public TMP_InputField Url; //the host e.g gitlab student
    public TMP_InputField ProjectNum; //the project's identification number
    public TMP_InputField Auth; // a token giving the required access requests for the repository.
    public TMP_InputField PersonalToken; //input fields for the player to input their repository information and GitLab account access token on page 2.
    public TMP_Text RepoInputError; //a text field for us to return error messages for invalid repos.
    public GameObject[] Pages = new GameObject[7]; //a list holding the instruction page, the 5 rule pages, and the house submission page at the end.
    public GameObject Next; // gameObjects holding buttons to go to the next or previous page in the Pages object - we will disable these on the first and last pages.
    public GameObject Prev;
    public TMP_Text GreetUser; //text to hold the player's name from their GitLab account.
    public TMP_Text HouseResult; //text to hold whether the house submitted leads to a win or a loss.
    public TMP_Text BobTimer; //text to inform the player of what time the file "Bob" was last updated.
    public TMP_Text RepoTimer; //text to inform the player of what time the repository was last updated.
    public TMP_Text UnitTestResult;
    protected int pageNum; //page number we want to load from Pages.
    private string mainURL; //the repository information we want to save - private copies prevent users from changing the information and breaking the game.
    private string projectCode;
    private string token;
    private string personalToken;
    private GitLabUI.RepositoryTreeObj[] treeArr; //a variable to help read the GitLab repository's structure.
    
    // -- flags to signal when processes are complete and the next one can start. -- 
    // these can be activated by buttons to start processes.
    private bool requestStatus = false;
    private bool checkValidRepo = false;
    private bool personalTokenValid = false;
    private bool repositoryValid = false;
    private bool checkHouse = false;
    // -- --
    private long StatusCode; //code resulting from requests.
    private string bobPath; //path of bob file so we can find the time to display for BobTimer.
    private GitLabUI package = new GitLabUI();
    private Button prevButton; //variables to hold the button components of the prev and next button gameObjects.
    private Button nextButton;

    private SpriteRenderer spriteRender;

    // Start is called once at the start of the script.
    void Start()
    {
        prevButton = Prev.GetComponent<Button>();
        nextButton = Next.GetComponent<Button>(); //retrieve the button components from the game objects
        spriteRender = Avatar.GetComponent<SpriteRenderer>(); //retrieve the sprite component from the character object
    }

    // Update is called once per frame
    void Update()
    {
        if (checkValidRepo && requestStatus) //if we are checking the repository is valid AND the request has returned then
        {
            if (repositoryValid && personalTokenValid) //if both the repository is valid and the personal Gitlab token is valid
            {
                RepoInputPage.SetActive(false); //stop displaying this page
                pageNum = 0;
                Pages[pageNum].SetActive(true); //set up the 7 pages
                Prev.SetActive(true);
                Next.SetActive(true); //set up the next and prev buttons
                prevButton.interactable = false; //since we start the beginning of the 7 pages, prevent players from pressing the previous button.
            }
            else
            {
                RepoInputError.text = "Invalid Details - please try again"; //if anything was incorrect, return an error message on the game page.
            }
            checkValidRepo = false;
            requestStatus = false;
            personalTokenValid = false;
            repositoryValid = false; //after every attempt at checking the repository details, reset the flags so the next check can perform correctly.

        }
        else if (checkHouse && requestStatus) //if we're checking if the house is correct at the end (win or lose) AND the request has returned then
        {
            if (StatusCode == 200) //should be 200 because we know the repository is valid.
            {
                checkWin(); //start checking if the player won.
            }
            else
            {
                HouseResult.text = "Something has gone wrong and the repository is no longer accessible. Please restart the game";
            }
            requestStatus = false;
        }
    }

    public void OnNextPage() //if the next button is pressed
    {
        Pages[pageNum].SetActive(false); //stop displaying the current page
        pageNum = pageNum + 1; 
        Pages[pageNum].SetActive(true); //display the next page
        if (pageNum == (Pages.Length - 1)) //if we are on the last page, deactivate the next button
        {
            nextButton.interactable = false;
        }
        else if (pageNum == 1) //if we just left the first page, reactivate the prev button
        {
            prevButton.interactable = true;
        }
    }

    public void OnPrevPage() //if the prev button is pressed
    {
        Pages[pageNum].SetActive(false); //stop displaying the current page
        pageNum = pageNum - 1;
        Pages[pageNum].SetActive(true); //start displaying the previous page
        if (pageNum == 0) //if we are on the first page, deactivate the prev button
        {
            prevButton.interactable = false;
        }
        else if (pageNum == (Pages.Length - 2)) //if we just left the last page, reactivate the next button
        {
            nextButton.interactable = true;
        }
    }
    public void OnStartGame() //start button at the beginning
    {
        StartPage.SetActive(false);
        RepoInputPage.SetActive(true); //stop displaying start screen and start displaying repository input screen
    }
    public void OnSubmitRepo() //if we submit the repository on the repository input page
    {
        requestStatus = false; //variable to let us know when the request to check the repo is valid has returned
        //extract the text from the input boxes at the time of repo submission
        //check if the given inputs are strings (because of security standards)
        //we also check if the text of Number.text is an integer value - to do this we try to parse Number.text into a number, this value is returned in NumberValue but we don't need it. 
        //the parsing function returns true if it was successful or false if it was not.
        if (package.ValidateRepositoryDetails(Url.text, ProjectNum.text, Auth.text)){
            mainURL = Url.text; //save the repository information in our private values
            projectCode = ProjectNum.text;
            token = Auth.text;
            personalToken = PersonalToken.text;
            requestSend(); //start the coroutine to check if the repository details are valid
        }
        else
        {
            requestStatus = true; //if the inputs are incorrect then we leave the personalTokenValid and repositoryValid flags false and inform the update function that the request to check repository validity is complete
        }
        checkValidRepo = true; //tell the update function that the check valid repo process has started
    }

    public void OnSubmitHouse() //if the submit button on the win/lose page is pressed
    {
        checkHouse =  true; //signal to the update function that the checkHouse process started
        requestSend(); //retrieve the repository structure 
    }

    //checkWin() checks if the player won (so if the house they submit breaks any of the rules then they lose, otherwise they win.)
    private void checkWin()
    {
        // --variables checking for if the player met a rule condition.--

        bool bobInBedroom = false;
        bool toiletInBedroom = false;
        bool stovesOutsideKitchen = false;
        bool sinksInCloset = false;
        int bedroomCount = 0;
        // -- --
        string fileName;
        string houseInvalid = "";
        foreach (GitLabUI.RepositoryTreeObj t in treeArr) //for every tree in the repository (folders have their own paths so a folder Bathroom will have a path "Bathroom" but a file Bob will also have a path "Bathroom/Bob")
        {
            fileName = package.GetFileName(t);
            // rule 1
            if (fileName.Contains("toilet") && package.CheckElementInPath(t, "bedroom")) //if the file/folder is named toilet and the file path leading to it has a component containing the substring "bedroom"
            {
                toiletInBedroom = true; //we found atleast 1 bedroom with a toilet, rule 1 is fulfilled
            }
            // rule 2
            if (fileName.Contains("stove") && !package.CheckElementInPath(t, "kitchen") ) //if the file/folder is named stove and the path does not contain the substring "kitchen"
            {
                stovesOutsideKitchen = true; //we found a stove outside of a kitchen, rule 2 is broken
            }
            // rule 3
            if (fileName.Contains("sink") && package.CheckElementInPath(t, "closet")) //if the file/folder is named sink and the path contains the substring "closet"
            {
                sinksInCloset = true; //we found a sink inside a closet, rule 3 is broken
            }
            // rule 4
            if (fileName.Contains("bob") && package.CheckElementInPath(t, "bedroom") ) //if the file/folder we are looking at is named Bob and the path contains the substring "bedroom"
            {
                bobInBedroom = true; //we don't account for multiple bobs, just as long as one is in a bedroom then rule 4 is fulfilled
                bobPath = t.path; //for the BobTimer calculation
            }
            // rule 5
            if (fileName.Contains("bedroom")) //using contains instead of equals means we can account for Bedroom1, Bedroom2, Bob's Bedroom etc.
            {
                bedroomCount = bedroomCount + 1; //counting the number of bedrooms we find.
            }
        }
        //now we collapse everything into 1 win or lose check.
        bool overall = bobInBedroom && toiletInBedroom && !stovesOutsideKitchen && !sinksInCloset;

        //for each flag or count, we change the error message to one for the first rule broken
        if (!toiletInBedroom)
        {
            houseInvalid = "There should be atleast 1 bedroom containing a toilet!";
        } 
        else if (stovesOutsideKitchen)
        {
            houseInvalid = "Stoves should not be outside of kitchens!";
        }
        else if (sinksInCloset)
        {
            houseInvalid = "Closets shouldn't contain sinks!";
        }
        else if (!bobInBedroom)
        {
            houseInvalid = "Bob should be in a bedroom!";
        }
        else if (bedroomCount < 1)
        {
            houseInvalid = "The house needs more bedrooms!";
            overall = false;
        }

        //now we update the result
        if (overall) // if the player won, update the character sprite to the one for winning and change the text to the winning text.
        {
            HouseResult.text = "Bob loves the House! You win!!!";
            spriteRender.sprite = WinSprite;
            getExtraScores(); //begin the web calls to get the Bob and repository update times to display,
        }
        else //if the player lost, update the character sprite to the one for losing and change the text to the losing text.
        {
            HouseResult.text = "You broke one of Bob's rules: " + houseInvalid;
            spriteRender.sprite = LoseSprite;
        }
        checkHouse = false; //signal that we have completed this house check.

    }

    // getTimes() get the last update time of the entire repository and of the bob file (if one is found) and displays them on the win/lose page.
    private void getExtraScores() // IEnumerator signals that a coroutine is used for webrequests so it can pause until the request is done. 
    {
        //first, we get the repository update time. 
        GitLabUI.RepositoryCommitListObj newestCommit = null;
        GitLabUI.PipelineObj latestCommitPipeline = null;
        //make sure the url size is restricted so it can't be the length of 200 characters etc.
        //List repository commits request
        if (package.GetRepositoryCommits(out GitLabUI.RepositoryCommitListObj[] commitList, mainURL, projectCode, token) == GitLabUI.Response.Success)
        {
            newestCommit = commitList[0];
        }
        if (newestCommit != null) //if the last request successfully gave us a latest commit
        {
            //Retrieve a commit request
            if (package.GetCommit(out GitLabUI.CommitObj latestCommit, mainURL, projectCode, token, newestCommit.id) == GitLabUI.Response.Success)
            {
                string day = latestCommit.committed_date.Remove(10); //committed_date gives the the day and time, we can cut off pieces of this text to give us the day and time separately.
                string time = latestCommit.committed_date.Remove(0, 11);
                time = time.Remove(8);
                RepoTimer.text = "Time of Completion: " + time + " on " + day; //now we can just display the date and time of the last repository update on the win/lose page.
                //For getting unit test results we need to extract the pipeline from the latest commit
                latestCommitPipeline = latestCommit.last_pipeline;
            }
        }
        // now we can add the Bob file's update time.
        //url encoding mostly affects special characters such as "\" so file paths that consist of only the file name are the same as their encoded version.
        GitLabUI.FileObj f = null;
        //Get file request
        if (package.GetFile(out GitLabUI.ResponseObj response, out f, mainURL, projectCode, token, bobPath) != GitLabUI.Response.Success)
        {
            if (response.responseMessage.Contains("\"message\":\"404 File Not Found\""))
            {
                BobTimer.text = "Time Bob was last moved: Can't access Bob because he is a folder";
            }
        }
        if (f != null) //if we successfully got the bob file
        {
            //Retrieve a commit request
            if (package.GetCommit(out GitLabUI.CommitObj c, mainURL, projectCode, token, f.last_commit_id) == GitLabUI.Response.Success)
            {
                string day = c.committed_date.Remove(10);
                string time = c.committed_date.Remove(0, 11); //we get the day and time information
                time = time.Remove(8);
                BobTimer.text = "Time Bob was last moved: " + time + " on " + day; //display the bob file's last update day and time information
            }
        }

        if (latestCommitPipeline != null){ 
            //here we request the test report for the last_pipeline connected to the lastest commit
            //Retrieve a test report for a pipeline request
            if (package.GetPipelineUnitTestReport(out GitLabUI.TestReportObj u, mainURL, projectCode, token, latestCommitPipeline.id) == GitLabUI.Response.Success)
            {
                UnitTestResult.text = "Bob appreciated the extra " + u.success_count.ToString() + " unit test passes!";
            }
        }
        else //if the latest commit pipeline is null it likely means there is no pipeline set up for the repository
        {
            UnitTestResult.text = "a pipeline would make this house perfect!";
        }

        //Here we look at whether there are any unit tests that have failed.
    }

    private void requestSend() //asks a repository for its repository tree structure.
    {
        //List repository tree request
        if (package.GetRepositoryTrees(out GitLabUI.ResponseObj response, out treeArr, mainURL, projectCode, token, true) == GitLabUI.Response.Success)
        {
            repositoryValid = true;
        }
        StatusCode = response.responseCode;
        //Retrieve the current user request
        if (package.GetUser(out GitLabUI.UserObj profile, mainURL, personalToken) == GitLabUI.Response.Success)
        {
            GreetUser.text = "Dear " + profile.name + ","; //write their name in a greeting on the win/lose page.
            personalTokenValid = true; // let update know the personal token was valid.
        }
        else
        {
            personalTokenValid = false;
        }
        requestStatus = true; //signal to update() function that the request has finished processing
    }
}
