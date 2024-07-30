using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionBoard : MonoBehaviour
{
    public int width = 6;
    public int height = 8;
    public float spacingX;
    public float spacingY;
    
    public GameObject[] potionPrefabs;
    public Node[,] potionBoard;
    public GameObject potionBoardGO;

    public List<GameObject> potionsToDestroy = new();
    public GameObject potionParent;

    [SerializeField]
    private Potion selectedPotion;

    [SerializeField]
    private bool isProcessingMove;

    [SerializeField]
    List<Potion> potionsToRemove = new();


    //layoutArray
    public ArrayLayout arrayLayout;
    public static PotionBoard Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeBoard();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject.GetComponent<Potion>())
            {
                if (isProcessingMove)
                    return;

                Potion potion = hit.collider.gameObject.GetComponent<Potion>();
                // Debug.Log("I have a clicked a potion it is: " + potion.gameObject);

                SelectPotion(potion);
            }
        }
    }

    void InitializeBoard()
    {
        DestroyPotions();
        potionBoard = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);
                if (arrayLayout.rows[y].row[x])
                {
                    potionBoard[x, y] = new Node(false, null);
                }
                else
                {
                    int randomIndex = Random.Range(0, potionPrefabs.Length);

                    GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                    potion.transform.SetParent(potionParent.transform);
                    potion.GetComponent<Potion>().SetIndicies(x, y);
                    potionBoard[x, y] = new Node(true, potion);
                    potionsToDestroy.Add(potion);
                }
            }
        }
        
        if (CheckBoard())
        {
            // Debug.Log("We have matches let's re-create the board");
            InitializeBoard();
        }
        // else
        // {
        //     Debug.Log("There are no matches, it's time to start the game!");
        // }
    }

    private void DestroyPotions()
    {
        if (potionsToDestroy != null)
        {
            foreach (GameObject potion in potionsToDestroy)
            {
                Destroy(potion);
            }
            potionsToDestroy.Clear();
        }
    }

    public bool CheckBoard()
    {
        if (GameManager.Instance.isGameEnded)
            return false;
        // Debug.Log("Checking Board");
        bool hasMatched = false;

        potionsToRemove.Clear();

        foreach(Node nodePotion in potionBoard)
        {
            if (nodePotion.potion != null)
            {
                nodePotion.potion.GetComponent<Potion>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (potionBoard[x,y].isUsable)
                {
                    Potion potion = potionBoard[x, y].potion.GetComponent<Potion>();

                    if(!potion.isMatched)
                    {
                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3)
                        {
                            MatchResult superMatchedPotions = SuperMatch(matchedPotions);

                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);

                            foreach (Potion pot in superMatchedPotions.connectedPotions)
                                pot.isMatched = true;

                            hasMatched = true;
                        }
                    }
                }
            }
        }

        return hasMatched;
    }

    public IEnumerator ProcessTurnOnMatchedBoard(bool _subtractMoves)
    {
        foreach (Potion potionToRemove in potionsToRemove)
        {
            potionToRemove.isMatched = false;
        }

        RemoveAndRefill(potionsToRemove);
        GameManager.Instance.ProcessTurn(potionsToRemove.Count, _subtractMoves);
        yield return new WaitForSeconds(0.4f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(false));
        }
    }

    private void RemoveAndRefill(List<Potion> _potionsToRemove)
    {
        foreach (Potion potion in _potionsToRemove)
        {
            int _xIndex = potion.xIndex;
            int _yIndex = potion.yIndex;

            Destroy(potion.gameObject);

            potionBoard[_xIndex, _yIndex] = new Node(true, null);
        }

        for (int x=0; x < width; x++)
        {
            for (int y=0; y <height; y++)
            {
                if (potionBoard[x, y].potion == null)
                {
                    // Debug.Log("The location X: " + x + " Y: " + y + " is empty, attempting to refill it.");
                    RefillPotion(x, y);
                }
            }
        }
    }

    private void RefillPotion(int x, int y)
    {
        int yOffset = 1;

        while (y + yOffset < height && potionBoard[x,y + yOffset].potion == null)
        {
            // Debug.Log("The potion above me is null, but i'm not at the top of the board yet, so add to my yOffset and try again. Current Offset is: " + yOffset + " I'm about to add 1.");
            yOffset++;
        }

        if (y + yOffset < height && potionBoard[x, y + yOffset].potion != null)
        {
            

            Potion potionAbove = potionBoard[x, y + yOffset].potion.GetComponent<Potion>();
            
            Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, potionAbove.transform.position.z);
            // Debug.Log("I've found a potion when refilling the board and it was in the location: [" + x + "," + (y + yOffset) + "] we have moved it to the location: [" + x + "," + y + "]");
            
            potionAbove.MoveToTarget(targetPos);
            potionAbove.SetIndicies(x, y);
            
            potionBoard[x, y] = potionBoard[x, y + yOffset];
            potionBoard[x, y + yOffset] = new Node(true, null);
        }

        if (y + yOffset == height)
        {
            // Debug.Log("I've reached the top of the board without finding a potion");
            SpawnPotionAtTop(x);
        }

    }

    private void SpawnPotionAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);
        int locationToMoveTo = 8 - index;
        // Debug.Log("About to spawn a potion, ideally i'd like to put it in the index of: " + index);
        //get a random potion
        int randomIndex = Random.Range(0, potionPrefabs.Length);
        GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
        newPotion.transform.SetParent(potionParent.transform);
        //set indicies
        newPotion.GetComponent<Potion>().SetIndicies(x, index);
        //set it on the potion board
        potionBoard[x, index] = new Node(true, newPotion);
        //move it to that location
        Vector3 targetPosition = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMoveTo, newPotion.transform.position.z);
        newPotion.GetComponent<Potion>().MoveToTarget(targetPosition);
    }

    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = 99;
        for (int y = 7; y >= 0; y--)
        {
            if (potionBoard[x,y].potion == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }

    #region MatchingLogic
    private MatchResult SuperMatch(MatchResult _matchedResults)
    {
        if (_matchedResults.direction == MatchDirection.Horizontal || _matchedResults.direction == MatchDirection.LongHorizontal)
        {
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();
                CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPotions);

                if (extraConnectedPotions.Count >= 2)
                {
                    // Debug.Log("I have a super Horizontal Match");
                    extraConnectedPotions.AddRange(_matchedResults.connectedPotions);

                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction
            };
        }
        else if (_matchedResults.direction == MatchDirection.Vertical || _matchedResults.direction == MatchDirection.LongVertical)
        {
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                List<Potion> extraConnectedPotions = new();
                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPotions);

                if (extraConnectedPotions.Count >= 2)
                {
                    // Debug.Log("I have a super Vertical Match");
                    extraConnectedPotions.AddRange(_matchedResults.connectedPotions);
                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction
            };
        }
        return null;
    }

    MatchResult IsConnected(Potion potion)
    {
        List<Potion> connectedPotions = new();
        PotionType potionType = potion.potionType;

        connectedPotions.Add(potion);

        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);
        if (connectedPotions.Count == 3)
        {
            // Debug.Log("I have a normal horizontal match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Horizontal
            };
        }
        else if (connectedPotions.Count > 3)
        {
            // Debug.Log("I have a Long horizontal match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongHorizontal
            };
        }
        connectedPotions.Clear();
        connectedPotions.Add(potion);

        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);
        CheckDirection(potion, new Vector2Int(0,-1), connectedPotions);

        if (connectedPotions.Count == 3)
        {
            // Debug.Log("I have a normal vertical match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Vertical
            };
        }
        else if (connectedPotions.Count > 3)
        {
            // Debug.Log("I have a Long vertical match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongVertical
            };
        } else
        {
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.None
            };
        }
    }

    void CheckDirection(Potion pot, Vector2Int direction, List<Potion> connectedPotions)
    {
        PotionType potionType = pot.potionType;
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (potionBoard[x,y].isUsable)
            {
                Potion neighbourPotion = potionBoard[x, y].potion.GetComponent<Potion>();

                if(!neighbourPotion.isMatched && neighbourPotion.potionType == potionType)
                {
                    connectedPotions.Add(neighbourPotion);

                    x += direction.x;
                    y += direction.y;
                }
                else
                {
                    break;
                }
                
            }
            else
            {
                break;
            }
        }
    }
    #endregion

    #region Swapping Potions

    public void SelectPotion(Potion _potion)
    {
        if (selectedPotion == null)
        {
            Debug.Log(_potion);
            selectedPotion = _potion;
        }
        else if (selectedPotion == _potion)
        {
            selectedPotion = null;
        }
        else if (selectedPotion != _potion)
        {
            SwapPotion(selectedPotion, _potion);
            selectedPotion = null;
        }
    }
    private void SwapPotion(Potion _currentPotion, Potion _targetPotion)
    {
        if (!IsAdjacent(_currentPotion, _targetPotion))
        {
            return;
        }

        DoSwap(_currentPotion, _targetPotion);

        isProcessingMove = true;

        StartCoroutine(ProcessMatches(_currentPotion, _targetPotion));
    }
    private void DoSwap(Potion _currentPotion, Potion _targetPotion)
    {
        GameObject temp = potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion;

        potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion = potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion;
        potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion = temp;

        //update indicies.
        int tempXIndex = _currentPotion.xIndex;
        int tempYIndex = _currentPotion.yIndex;
        _currentPotion.xIndex = _targetPotion.xIndex;
        _currentPotion.yIndex = _targetPotion.yIndex;
        _targetPotion.xIndex = tempXIndex;
        _targetPotion.yIndex = tempYIndex;

        _currentPotion.MoveToTarget(potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion.transform.position);
        _targetPotion.MoveToTarget(potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion.transform.position);
        
        Vector2 direction = new Vector2(_targetPotion.xIndex - _currentPotion.xIndex, _targetPotion.yIndex - _currentPotion.yIndex);
        GameManager.Instance.player.Move(direction);
    }

    private IEnumerator ProcessMatches(Potion _currentPotion, Potion _targetPotion)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        }
        else
        {
            DoSwap(_currentPotion, _targetPotion);
        }
        isProcessingMove = false;
    }


    private bool IsAdjacent(Potion _currentPotion, Potion _targetPotion)
    {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }


    #endregion

}

public class MatchResult
{
    public List<Potion> connectedPotions;
    public MatchDirection direction;
}

public enum MatchDirection
{
    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Super,
    None
}


