using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonScript : MonoBehaviour {

	public void StartGame() { //takes you to the repository input scene from the menu when you press start
		SceneManager.LoadScene("RepoInput", LoadSceneMode.Single);
	}

	public void GoToMenu() //lets you go back to the main menu from the main game scene
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

	public void RestartGame() //loads repository input scene when you restart the game.
	{
		SceneManager.LoadScene("RepoInput", LoadSceneMode.Single);
	}
}
