using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 

    public GameObject backgroundPanel;  
    public GameObject victoryPanel;
    public GameObject losePanel;

    // public int goal; 
    public int moves; 
    public int points; 

    public bool isGameEnded;

    public TMP_Text pointsTxt;
    public TMP_Text movesTxt;
    // public TMP_Text goalTxt;
    
    //new
    public Transform goalTransform; 
    public Player player;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(int _moves)
    {
        moves = _moves;
        // goal = _goal;
    }
    void Update()
    {
        pointsTxt.text = "Points: " + points.ToString();
        movesTxt.text = "Moves: " + moves.ToString();
        // goalTxt.text = "Goal: " + goal.ToString();
        if (Vector2.Distance(player.transform.position, goalTransform.position) < 1f)
        {
            Debug.Log(Vector2.Distance(player.transform.position, goalTransform.position));
            Victory();
        }
    }

    public void ProcessTurn(int _pointsToGain, bool _subtractMoves)
    {
        points += _pointsToGain;
        if (_subtractMoves)
            moves--;

        // if (points >= goal)
        // {
        //     //won
        //     isGameEnded = true;
        //     //victory screen
        //     backgroundPanel.SetActive(true);
        //     victoryPanel.SetActive(true);
        //     PotionBoard.Instance.potionParent.SetActive(false);
        //     return;
        // }
        
        if (moves == 0)
        {
            //lose
            isGameEnded = true;
            backgroundPanel.SetActive(true);
            losePanel.SetActive(true);
            PotionBoard.Instance.potionParent.SetActive(false);
            return;
        }
        
    }
    
    private void Victory()
    {
        // won
        isGameEnded = true;
        backgroundPanel.SetActive(true);
        victoryPanel.SetActive(true);
        PotionBoard.Instance.potionParent.SetActive(false);
    }

    //button to change scene winning
    public void WinGame()
    {
        SceneManager.LoadScene(0);
    }

    //button to change scene losing
    public void LoseGame()
    {
        SceneManager.LoadScene(0);
    }
}
