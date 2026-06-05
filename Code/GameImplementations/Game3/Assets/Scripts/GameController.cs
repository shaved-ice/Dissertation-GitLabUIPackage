using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System;
using GitLabUnityUI;

public class GameController : MonoBehaviour {
	
    // To be populated in the inspector
    public Sprite[] sprites; // The 3 node sprites
    public AudioSource sound1, sound2; // Capture sounds for Player 1 and 2
    public GameObject endPopup; // The victory box
    public GameObject[] progressBars;
    public Text[] nodesCount;
    public Text[] clustersCount;
    public GameObject currentPlayerBox;

    public List<GameObject> btns = new List<GameObject>(); //holds the nodes in the game
	

	// Game state
    private int playerScore;
    private int currPlayer = -1;
	private bool lockGame = false;
    private readonly float waitFactor = 0.08F;
    // -- Our variables --
    private string host1;
    private string code1;
    private string token1;
    private string host2 = "";
    private string code2 = "";
    private string token2 = "";
    private GameObject[] objects;
    private bool nextReqReady = true;
    private Encoding encoder = new UTF8Encoding();
    public InputRepoButton RepositorySubmission;
    //boardContents holds the base/default board we want to write onto a file in GitLab for playing
    //The @ allows the string to hold the newline characters we want as \n doesn't work for this scenario
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
    // -- end of our variables --
    private GitLabUI package = new GitLabUI();

    private void Awake()
    {
        //sprites = Resources.LoadAll<Sprite>("Sprites/Nodes");
    }

    // Use this for initialization
    void Start () {
        //getting repo information
        host1 = InputRepoButton.GetHost1();
        code1 = InputRepoButton.GetCode1();
        token1 = InputRepoButton.GetToken1();
        if (GameModel.IS_HUMAN) //if we are in 2 player mode then get the details of the second repository as well
        {
            host2 = InputRepoButton.GetHost2();
            code2 = InputRepoButton.GetCode2();
            token2 = InputRepoButton.GetToken2();
        }
        InitButtons(); //initate the buttons
		InitStartingNodes(); //initate the nodes in the unity game scene
        //create and add player 1 and 2 to the GameModel object
		GameModel.players.Clear();

        Player player1 = new Player
        {
            isHuman = true,
            team = 0,
            color = new Color(0f, 0f, 1.0f, 1.0f)
        };
        GameModel.players.Add(player1);

        Player player2 = new Player
        {
            isHuman = GameModel.IS_HUMAN,
            team = 1,
            color = new Color(1f, 0f, 0f, 1.0f),
            aiLevel = GameModel.AI_DIFFICULTY
        };
        GameModel.players.Add(player2);
        //initialises the first and last node (which are the starting nodes for both teams)
		Propagate(btns[0], player1.team,0); 
		Propagate(btns[btns.Count -1], player2.team,0);

		UpdateScore();

		currPlayer = player1.team;
    }

    void Update()
    {
        if (Input.GetKey("escape")) //if escape key is pressed, quit
            Application.Quit();
        if(nextReqReady){ //if the next request is ready, reset the flag and check if the repository we need to check has made a move.
            nextReqReady = false;
            checkForMove();
        }
    }

    //checkForMove() checks if the "Board" file has changed to make an appropriate move and if it has, reflects the move in the unity game scene
    // if it has made an invalid move (e.g trying to turn 2 or more nodes) then reset the board to the default board contents
    private void checkForMove() 
    {
        bool error = false;
        int moveCount = 0;
        string host;
        string code;
        string token;
        if (currPlayer == 1 && GameModel.IS_HUMAN) //if it is in 2 player mode and we are on a player == 1 interation then we need to look at repository 2
        {
            host = host2;
            code = code2;
            token = token2;
        }
        else //look at repository 1
        {
            host = host1;
            code = code1;
            token = token1;
        }
        //get the contents of the Board file.
        //Get file request
        string decoded = "";
        if (package.GetFile(out GitLabUI.FileObj f, host, code, token, "Board") == GitLabUI.Response.Success)
        {
            if (f.content != null){ //if it's null the rest of this code will throw an error
                byte[] encoded = Convert.FromBase64String(f.content);
                UTF8Encoding u = new UTF8Encoding();
                decoded = u.GetString(encoded);
                //The board formatting from GitLab is complicated so we break it down manually.
                char[] decoded2 = decoded.ToCharArray(); //turn the board of x's into an array of characters 
                char[] decoded3 = new char[168]; //turn array of characters into an array of 14 x 12 (the ingame board size) characters by removing the extra newline etc. characters
                //note: identical files read from GitLab have different newline spacings depending on if it was submitted by a human or from calling the GitLab API
                int valToSkip = 14; //The first value we skip is 14 because this is the end of the first line of x's
                bool skipNextLine = false; //we need a flag because at the end of every line is 2 empty characters that we don't need
                int currentPos = 0;
                for (int i = 0; i < decoded2.Length; i++)
                {
                    if (i == valToSkip) //if we're at a skip value, increment to the next skip value which will be in 16 lines because of the 14 x's + the two empty characters we are currently skipping
                    {
                        valToSkip = valToSkip + 16;
                        skipNextLine = true; //let the for loop know to skip a second empty character
                        continue; //skip to the next loop
                    }
                    else if (skipNextLine) //if we're skipping a second empty character
                    {
                        skipNextLine = false; //reset the flag
                        continue; //skip to the next loop
                    }
                    else //if we're not skipping then copy the current value in our decoded3's next open space
                    {
                        if (currentPos == 168) //there's too many valid characters! Something is wrong and we should abort.
                        {
                            error = true;
                            break;
                        }
                        decoded3[currentPos] = decoded2[i];
                        currentPos = currentPos + 1; //increment currentPos to the next open space
                    }
                }
                if (!error) //if all went well, we should have our gameboard result from GitLab
                {
                    int movePos = 0;
                    //detect how many "moves" were made on the board
                    for (int i = 0; i < decoded3.Length; i++)
                    {
                        if (decoded3[i] != 'x') //'x' is the default value so anything else counts as a move
                        {
                            moveCount = moveCount + 1; //if we have more than one move then it is invalid
                            movePos = i;
                            if (moveCount > 1)
                            {
                                error = true;
                                break; //more than one move is invalid so we stop here
                            }
                        }
                    }
                    if (!error && moveCount == 1) //if exactly 1 move was made
                    {
                        GameObject clicked = objects[movePos]; //get the corresponding node object 
                        //if the current node to be clicked is the correct player's and the current player is supposed to be a human
                        if (currPlayer == clicked.GetComponent<Node>().owner && GameModel.players[currPlayer].isHuman) {
                            ClickNode(clicked); //call the function to act like the node was clicked in the unity game scene
                        }
                    }
                    
                }

            }
        }
        else
        {
            throw new Exception("can't read repository");
        }
        if (error || moveCount > 0) //if we had an error or changes to the board were made, update the board with the default board contents to reset the board.
            {
                //note: since we encoded the contents, we add a base64 encoding note to the formData 
                //Update file request
                if (package.UpdateFile(host, code, token, "Board", boardContents, "resetting board", "main", true) != GitLabUI.Response.Success)
                {
                    throw new Exception("can't access repository");
                }
            }
            nextReqReady = true; //let update know the request is complete and it can start the next one
        
    }

    //InitButtons() initialises the buttons into the btns array
    void InitButtons() 
    {
        objects = GameObject.FindGameObjectsWithTag("PuzzleButton");

        for(int i = 0; i < objects.Length; i++)
        {
            btns.Add(objects[i]);
            btns[i].GetComponent<Button>().image.sprite = sprites[0];

			//Add click listener
			btns[i].GetComponent<Button>().onClick.AddListener(ClickNodeEvent);

			//Shuffle the nodes
			Button btn = btns[i].GetComponent<Button>();
			int randomDirection = (int) UnityEngine.Random.Range(0f, 4f);

			btns[i].GetComponent<Node>().direction = randomDirection;
			btn.transform.Rotate(Vector3.forward * randomDirection * -90);
        }
    }

    //InitStartingNodes() sets the initial random directions of the starting nodes
    void InitStartingNodes() 
    {
        //Init starting nodes
        // Node 0
        int zeroDirection = btns[0].GetComponent<Node>().direction;
        btns[0].GetComponent<Node>().direction = 0;
        btns[0].transform.Rotate(Vector3.forward * (4- zeroDirection) * -90);

        // Last Node
        int lastDirection = btns[btns.Count -1].GetComponent<Node>().direction;
        btns[btns.Count - 1].GetComponent<Node>().direction = 2;
        btns[btns.Count - 1].transform.Rotate(Vector3.forward * (2 - lastDirection) * -90);
    }
    
    //ClickNodeEvent() handles when a player clicks a node in the unity game scene
    public void ClickNodeEvent() 
    {
        GameObject clicked = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        if (currPlayer == clicked.GetComponent<Node>().owner && GameModel.players[currPlayer].isHuman) {
            ClickNode(clicked);
        }
    }

    //ClickNode(GameObject clicked) rotates a clicked node
    void ClickNode(GameObject clicked) 
    {
		if (!lockGame) {
			if (currPlayer == clicked.GetComponent<Node> ().owner) {
				lockGame = true;

				int nextDirection = (clicked.GetComponent<Node> ().direction + 1) % 4;
				clicked.GetComponent<Node> ().direction = nextDirection;
				clicked.GetComponent<Button> ().transform.Rotate (Vector3.forward * -90);

                int maxDepth = Propagate(clicked, currPlayer,0);

                StartCoroutine(WaitForAnimationThenContinue(clicked, maxDepth));
			}
		}
    }

    //WaitForAnimationThenContinue(GameObject clicked, int maxDepth) plays the animation for the current move then increments things in preparation for the next move to be made
    IEnumerator WaitForAnimationThenContinue(GameObject clicked, int maxDepth)
    {
        yield return new WaitForSecondsRealtime((maxDepth-1) * waitFactor);
        UpdateScore();
        CheckVictory();

        currPlayer = (currPlayer + 1) % GameModel.players.Count;

        // Update the current player box display
        Text currentPlayerText = currentPlayerBox.GetComponentInChildren<Text>();
        currentPlayerText.text = "Player " + (currPlayer + 1);
        currentPlayerText.color = GameModel.players[currPlayer].color;

        lockGame = false;
        StartCoroutine(PlayAI());
    }

    //UpdateScore() updates the scores by counting the nodes and updating the score, the progress bar, the cluster score and accounts for single and 2 player mode
    void UpdateScore() {
		int[] nodesInt = new int[nodesCount.Length];
        int[] clustersInt = new int[clustersCount.Length];

        // Count nodes per player
        foreach (GameObject candidateNode in btns) {
			int currentOwner = candidateNode.GetComponent<Node> ().owner;
			if (currentOwner != -1) {
                nodesInt[currentOwner] = nodesInt[currentOwner] + 1;
			}
		}

        // Update nodes count
        for (int i = 0; i < nodesInt.Length; i++)
        {
            string nodeText = " nodes";
            if(nodesInt[i] <= 1)
            {
                nodeText = " node";
            }
            nodesCount[i].GetComponent<Text>().text = nodesInt[i].ToString() + nodeText;
        }

        // Progress bars
        for (int i = 0; i < progressBars.Length; i++)
        {
            //int parentHeight = progressBars[i].GetComponentInParent<Transform>().height;
            float percent = (float)nodesInt[i] / (float)btns.Count;
            progressBars[i].GetComponent<Slider>().value = percent;
        }

        // Clusters score
		for (int i = 0; i < GameModel.players.Count; i++)
        {
            clustersInt[i] = TilesHelper.CountClusters(i, btns); //counts number of clusters in the tile map for owner i

            string clusterText = " clusters";
            if (clustersInt[i] <= 1)
            {
                clusterText = " cluster";
            }

            clustersCount[i].GetComponent<Text>().text = clustersInt[i].ToString() + clusterText;
        }

		if (GameModel.IS_HUMAN) {
			playerScore = nodesInt [0] * clustersInt [0];
		} else {
            // Score vs. AI depends on the difficulty
			playerScore = nodesInt [0] * clustersInt [0] * (GameModel.AI_DIFFICULTY + 1);
		}
	}
 
    //CheckVictory() checks that one player has gained control of all the other player's nodes and displays a win or lose message if a win has occured.
    void CheckVictory()
    {
        // Check that every node is either neutral or belongs to the current player.
        foreach(GameObject obj in btns)
        {
            int currentOwner = obj.GetComponent<Node>().owner;
            if (currentOwner >= 0 && currentOwner != currPlayer)
            {
                return;
            }
        }
        string outcomeMessage = "";
        //special outcome messages are made for GitLab when the original includes escape characters e.g \n because issues can't take those
		if (GameModel.IS_HUMAN) {
			endPopup.GetComponentsInChildren<Text> () [1].text = "Player " + (currPlayer + 1) + " wins!";
            outcomeMessage = "Player " + (currPlayer + 1) + " wins!";
		} else {
			if (currPlayer == 0) {
				endPopup.GetComponentsInChildren<Text>()[1].text = "You win!\nYou have eliminated the virus threat\nScore: " + playerScore;
                outcomeMessage = "You win! You have eliminated the virus threat. Score: " + playerScore;
			} else {
				endPopup.GetComponentsInChildren<Text>()[1].text = "The virus infection has spread\nThe data center is lost\nYou are fired :(";
                outcomeMessage = "You lose! The virus infection has spread and the data center is lost. You are fired :("; 
			}
		}
        EndGame(outcomeMessage);
        endPopup.SetActive(true);

        
    }

    //EndGame(string message) take an outcome message and makes a issue in the GitLab repo(s) containing that message
    void EndGame(string message)
    {
        //send the outcome message by making an issue in the repository
        //Create issue request
        if (package.CreateIssue(host1, code1, token1, "GameOutcome", message) != GitLabUI.Response.Success)
        {
            throw new Exception("couldn't write an issue");
        }
        if (GameModel.IS_HUMAN) //if we are in 2 player mode also send the message to repository 2
        {
            //Create issue request
            if (package.CreateIssue(host2, code2, token2, "GameOutcome", message) != GitLabUI.Response.Success)
            {
                throw new Exception("couldn't write an issue");
            }
        }
    }

    //PlayAI() uses another script to choose a move depending on difficulty and then plays it if the game is in singleplayer mode
    IEnumerator PlayAI()
    {
		if(!GameModel.players[currPlayer].isHuman)
        {
			GameObject toPlay = GameModel.players[currPlayer].PlaySomething(btns); //play something chooses a move from different AIs depending on difficulty
            if (toPlay != null) {
                yield return new WaitForSeconds(0.9f);
                ClickNode(toPlay);
            }
        }
    }

    //Propagate(GameObject source, int newOwner, int PreviousConverted) initialises a node with its new owner, neighbours, and direction. It updates surrounding nodes if they are a cluster and calculates values like max depth.
	int Propagate(GameObject source, int newOwner, int PreviousConverted)
    {
		int maxDepth = 1;

		//Rotate node
        source.GetComponent<Node>().ChangeOwner(newOwner);

        StartCoroutine(WaitForPlay(source, newOwner, PreviousConverted));

        // Update the target node
        int targetDirection = source.GetComponent<Node>().direction;
        GameObject targetNeighbor = TilesHelper.GetNeighbor(source, targetDirection, btns); //GetNeighbour returns coordinates based off the number direction you gave e.g 0 = north of the node you gave
        if(targetNeighbor != null)
        {
            if(targetNeighbor.GetComponent<Node>().owner != newOwner)
            {
                maxDepth = Mathf.Max(maxDepth+1, Propagate(targetNeighbor, newOwner, PreviousConverted + 1));
            }
        }

        // Update neighbors pointing to currentNode
        for(int neighDirection = 0; neighDirection < 4; neighDirection++)
        {
            GameObject neighbor = TilesHelper.GetNeighbor(source, neighDirection, btns);
            if (neighbor != null)
            {
                if (neighbor.GetComponent<Node>().owner != newOwner) { 
                    if (neighbor.GetComponent<Node>().direction == (neighDirection + 2) % 4)
                    {
                        maxDepth = Mathf.Max(maxDepth+1, Propagate(neighbor, newOwner, PreviousConverted + 1));
                    }
                }
            }
        }

		return Mathf.Max(maxDepth, PreviousConverted+1);
    }

    //WaitForPlay(GameObject source, int newOwner, int PreviousConverted) plays a noise and displays some particles
    IEnumerator WaitForPlay(GameObject source, int newOwner, int PreviousConverted)
    {
        yield return new WaitForSecondsRealtime(PreviousConverted* waitFactor);
        source.GetComponent<Button>().image.sprite = sprites[newOwner+1];
        // Set arrow color
        //Color playerColorAlpha = new Color(GameModel.players[newOwner].color.r, GameModel.players[newOwner].color.g, GameModel.players[newOwner].color.b*2, 0.5f);
        //source.GetComponentsInChildren<Image>()[1].color = playerColorAlpha;

        float randomPitch = UnityEngine.Random.Range(0.8f, 1.2f);
        AudioSource sound;
        if (newOwner == 1)
        {
            sound = sound1;
        } else
        {
            sound = sound2;
        }
        sound.pitch = randomPitch;
        sound.Play(); //plays a sound

        // Show particles
        ParticleSystem.MainModule particlesSettings = source.GetComponent<ParticleSystem> ().main;
        //Color playerColor = GameModel.players[newOwner].color;
        //Color particleColor = new Color(playerColor.r, playerColor.g, playerColor.b);
        //particleColor.a = 0.5F;
        //particlesSettings.startColor = new ParticleSystem.MinMaxGradient(particleColor);
		source.GetComponent<ParticleSystem>().Play(); //plays a particle animation
    }

}
