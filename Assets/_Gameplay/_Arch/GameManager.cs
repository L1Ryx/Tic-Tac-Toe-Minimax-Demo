using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Text Refs")]
    public TMP_Text statusText;
    public TMP_Text resetText;
    public TMP_Text tutorialText; 
    public TMP_Text thinkingText; 
    public TMP_Text levelHeaderText; 
    public TMP_Text nextText; 
    private Coroutine nextFadeCoroutine; // To control fade in/out



    private int currentLevel = 1;
    private int maxLevel = 2; // Number of levels you have

    private bool playerWon = false; // Track if the player won to allow ENTER
    private List<string> aiLogs = new List<string>();

    [SerializeField] private string resetPromptText = "Press SPACE to Reset"; // Editable prompt text
    private bool gameEnded = false; // Flag to know if game is over
    private Coroutine resetFadeCoroutine;

    public Button[] buttons; // Link all 9 buttons here in Inspector
    private int[] board; // 0 = empty, 1 = player, 2 = AI
    private bool playerTurn = true; // true = player, false = AI
    [Header("Settings")]
    [SerializeField] private float enemyMoveDelay = 1f; // Default 1 second

    [SerializeField] private Color playerWinColor = new Color(0.31f, 0.76f, 0.97f); // Light Blue
    [SerializeField] private Color enemyWinColor = new Color(1f, 0.54f, 0.5f); // Soft Coral Red
    [SerializeField] private float colorLerpDuration = 1f; // 1 second to lerp
    [SerializeField] private float resetFadeDuration = 1f; // Time for fade in/out



    [Header("Status Texts")]
    [SerializeField] private string yourTurnText = "Your turn.";
    [SerializeField] private string enemyTurnText = "Enemy's turn.";
    [SerializeField] private string playerWinText = "You win!";
    [SerializeField] private string enemyWinText = "You lose.";
    [SerializeField] private string drawText = "Draw.";

    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => PlayerMove(index));
        }

        InitializeGame(); // <- clean initialization
    }
    
    void InitializeGame()
    {
        Debug.Log($"Initializing Level {currentLevel}");
        

        // Reset board state
        board = new int[9];

        for (int i = 0; i < buttons.Length; i++)
        {
            var colors = buttons[i].colors;
            colors.normalColor = Color.white;
            colors.disabledColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            buttons[i].colors = colors;

            buttons[i].interactable = true;
        }

        playerTurn = true;
        gameEnded = false;
        playerWon = false;

        // Reset status text
        statusText.text = yourTurnText;
        Color statusColor = Color.white;
        statusColor.a = 1f;
        statusText.color = statusColor;
        thinkingText.text = "";
        UpdateLevelHeader();
        // Clear Next Text
        nextText.text = "";
        if (nextFadeCoroutine != null)
        {
            StopCoroutine(nextFadeCoroutine);
            nextFadeCoroutine = null;
        }
        Color nextColor = nextText.color;
        nextColor.a = 1f; // Reset alpha to visible fully
        nextText.color = nextColor;




        // Clear tutorial text (optional)
        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(true);
        }

        // Clear reset text and stop fade
        if (resetFadeCoroutine != null)
        {
            StopCoroutine(resetFadeCoroutine);
        }
        resetText.text = "";
        Color resetColor = resetText.color;
        resetColor.a = 1f;
        resetText.color = resetColor;
    }
    
    void UpdateLevelHeader()
    {
        switch (currentLevel)
        {
            case 1:
                levelHeaderText.text = "Tic-Tac-Toe v.s. Enemy Using Random Choice";
                break;
            case 2:
                levelHeaderText.text = "Tic-Tac-Toe v.s. Enemy Using Minimax (Depth = 2)";
                break;
            default:
                levelHeaderText.text = $"Tic-Tac-Toe v.s. Enemy (Level {currentLevel})";
                break;
        }
    }



    void PlayerMove(int index)
    {
        if (!playerTurn || board[index] != 0) return;

        HideTutorialIfVisible();


        board[index] = 1;
        UpdateButton(index, 1);
        playerTurn = false;

        if (CheckWin(1))
        {
            statusText.text = playerWinText;
            StartCoroutine(LerpTextColor(playerWinColor));
            Debug.Log("Player Wins!");
            DisableAllButtons();
            playerWon = true; // <- Mark that player won
            if (nextFadeCoroutine != null)
            {
                StopCoroutine(nextFadeCoroutine);
            }
            nextText.text = "Press ENTER to Face Next Enemy";
            nextFadeCoroutine = StartCoroutine(FadeInNextText());
            return;
        }
        




        if (CheckTie())
        {
            statusText.text = drawText;
            Debug.Log("It's a Tie!");
            DisableAllButtons();
            return;
        }

        statusText.text = enemyTurnText;
        Invoke(nameof(AIMove), enemyMoveDelay);
    }

    void HideTutorialIfVisible()
    {
        if (tutorialText != null && tutorialText.gameObject.activeSelf)
        {
            tutorialText.gameObject.SetActive(false);
        }
    }




    void AIMove()
    {
        int moveIndex = -1;

        if (currentLevel == 1)
        {
            moveIndex = GetRandomMove();
        }
        else if (currentLevel == 2)
        {
            moveIndex = GetMinimaxMove(depth: 2);
        }

        if (moveIndex != -1)
        {
            board[moveIndex] = 2;
            UpdateButton(moveIndex, 2);
        }

        if (CheckWin(2))
        {
            statusText.text = enemyWinText;
            StartCoroutine(LerpTextColor(enemyWinColor));
            Debug.Log("AI Wins!");
            DisableAllButtons();
            return;
        }

        if (CheckTie())
        {
            statusText.text = drawText;
            Debug.Log("It's a Tie!");
            DisableAllButtons();
            return;
        }

        playerTurn = true;
        statusText.text = yourTurnText;
    }


    int GetRandomMove()
    {
        List<int> availableIndices = new List<int>();

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
            return -1; // No moves left (shouldn't happen, but safety check)

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }
    
    int GetMinimaxMove(int depth)
    {
        aiLogs.Clear();
        aiLogs.Add($"Starting Minimax thinking at Level {currentLevel} with depth {depth}");

        int bestScore = int.MinValue;
        int bestMove = -1;
        List<int> bestMoves = new List<int>(); // To track moves tied for best

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                board[i] = 2; // AI move
                int score = Minimax(depth - 1, false, 2);
                board[i] = 0; // Undo move

                aiLogs.Add($"Considering move at square {i}: resulting score {score}");

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = i;

                    bestMoves.Clear();
                    bestMoves.Add(i);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(i);
                }
            }
        }

        if (bestMove != -1)
        {
            if (bestMoves.Count > 1)
            {
                aiLogs.Add($"Enemy chooses square {bestMove} because resulting score {bestScore} is one of the highest.");
            }
            else
            {
                aiLogs.Add($"Enemy chooses square {bestMove} because resulting score {bestScore} is highest.");
            }
        }
        else
        {
            aiLogs.Add("No valid move found.");
        }

        foreach (string log in aiLogs)
        {
            Debug.Log(log);
        }

        thinkingText.text = string.Join("\n->\n", aiLogs);

        return bestMove;

    }



    int Minimax(int depth, bool isMaximizing, int currentPlayer)
    {
        if (CheckWin(2))
            return 10;
        else if (CheckWin(1))
            return -10;
        else if (CheckTie())
            return 0;

        if (depth == 0)
            return 0; // Depth limit reached

        if (isMaximizing)
        {
            int bestScore = int.MinValue;

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 2; // AI move
                    int score = Minimax(depth - 1, false, 2);
                    board[i] = 0;

                    bestScore = Mathf.Max(score, bestScore);
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 1; // Player move
                    int score = Minimax(depth - 1, true, 1);
                    board[i] = 0;

                    bestScore = Mathf.Min(score, bestScore);
                }
            }
            return bestScore;
        }
    }





    IEnumerator LerpTextColor(Color targetColor)
    {
        Color startColor = statusText.color;
        float elapsedTime = 0f;

        while (elapsedTime < colorLerpDuration)
        {
            statusText.color = Color.Lerp(startColor, targetColor, elapsedTime / colorLerpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        statusText.color = targetColor; // Ensure exact final color
    }

    IEnumerator FadeInNextText()
    {
        Color color = nextText.color;
        color.a = 0f;
        nextText.color = color;

        float elapsed = 0f;
        while (elapsed < resetFadeDuration) // reuse resetFadeDuration
        {
            float t = elapsed / resetFadeDuration;
            color.a = Mathf.Lerp(0f, 1f, t);
            nextText.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = 1f;
        nextText.color = color;
    }

    IEnumerator FadeOutNextText()
    {
        Color color = nextText.color;
        color.a = 1f;
        nextText.color = color;

        float elapsed = 0f;
        while (elapsed < resetFadeDuration) // reuse resetFadeDuration
        {
            float t = elapsed / resetFadeDuration;
            color.a = Mathf.Lerp(1f, 0f, t);
            nextText.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = 0f;
        nextText.color = color;
        nextText.text = ""; // Clear after fade out
    }



    bool CheckTie()
    {
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                return false; // still empty squares, no tie
            }
        }
        return true; // no empty squares, tie
    }

    void UpdateButton(int index, int player)
    {
        ColorBlock colors = buttons[index].colors;

        Color playerColor;
        Color aiColor;

        ColorUtility.TryParseHtmlString("#4FC3F7", out playerColor); // Light Sky Blue
        ColorUtility.TryParseHtmlString("#FF8A80", out aiColor);     // Soft Coral Red

        if (player == 1)
        {
            colors.normalColor = playerColor;
            colors.disabledColor = playerColor;
            colors.highlightedColor = playerColor;
            colors.pressedColor = playerColor;
        }
        else if (player == 2)
        {
            colors.normalColor = aiColor;
            colors.disabledColor = aiColor;
            colors.highlightedColor = aiColor;
            colors.pressedColor = aiColor;
        }

        buttons[index].colors = colors;
        buttons[index].interactable = false;
    }

    void DisableAllButtons()
    {
        foreach (Button btn in buttons)
        {
            btn.interactable = false;
        }

        gameEnded = true;
        resetText.text = resetPromptText;

        if (resetFadeCoroutine != null)
        {
            StopCoroutine(resetFadeCoroutine);
        }
        resetFadeCoroutine = StartCoroutine(FadeInResetText());
    }



    void Update()
    {
        if (gameEnded)
        {
            if (playerWon && Input.GetKeyDown(KeyCode.Return)) // ENTER key
            {
                MoveToNextLevel();
            }
            else if (Input.GetKeyDown(KeyCode.Space)) // SPACE key
            {
                ResetGame();
            }
        }
    }
    
    void MoveToNextLevel()
    {
        currentLevel++;

        if (nextFadeCoroutine != null)
        {
            StopCoroutine(nextFadeCoroutine);
        }
        nextFadeCoroutine = StartCoroutine(FadeOutNextText());


        InitializeGame();
    }


    
    IEnumerator FadeInResetText()
    {
        Color color = resetText.color;
        color.a = 0f;
        resetText.color = color;

        float elapsed = 0f;
        while (elapsed < resetFadeDuration)
        {
            float t = elapsed / resetFadeDuration;
            color.a = Mathf.Lerp(0f, 1f, t);
            resetText.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = 1f;
        resetText.color = color;
    }

    IEnumerator FadeOutResetText()
    {
        Color color = resetText.color;
        color.a = 1f;
        resetText.color = color;

        float elapsed = 0f;
        while (elapsed < resetFadeDuration)
        {
            float t = elapsed / resetFadeDuration;
            color.a = Mathf.Lerp(1f, 0f, t);
            resetText.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        color.a = 0f;
        resetText.color = color;
        resetText.text = ""; // Clear the text after fade out
    }

    void ResetGame()
    {
        InitializeGame();
    }





    bool CheckWin(int player)
    {
        int[,] winConditions = new int[,]
        {
            {0,1,2}, {3,4,5}, {6,7,8}, // rows
            {0,3,6}, {1,4,7}, {2,5,8}, // columns
            {0,4,8}, {2,4,6}           // diagonals
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            if (board[winConditions[i,0]] == player &&
                board[winConditions[i,1]] == player &&
                board[winConditions[i,2]] == player)
            {
                return true;
            }
        }
        return false;
    }
}
