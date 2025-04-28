using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TMP_Text statusText;
    public TMP_Text resetText;
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
        board = new int[9];

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => PlayerMove(index));
        }

        statusText.text = yourTurnText;
    }

    void PlayerMove(int index)
    {
        if (!playerTurn || board[index] != 0) return;

        board[index] = 1;
        UpdateButton(index, 1);
        playerTurn = false;

        if (CheckWin(1))
        {
            statusText.text = playerWinText;
            StartCoroutine(LerpTextColor(playerWinColor));
            Debug.Log("Player Wins!");
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

        statusText.text = enemyTurnText;
        Invoke(nameof(AIMove), enemyMoveDelay); // <<<<< DELAYED enemy move
    }




    void AIMove()
    {
        List<int> availableIndices = new List<int>();

        // Find all available squares
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count > 0)
        {
            // Pick a random available square
            int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            board[randomIndex] = 2;
            UpdateButton(randomIndex, 2);
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
        if (gameEnded && Input.GetKeyDown(KeyCode.Space))
        {
            ResetGame();
        }
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
        for (int i = 0; i < board.Length; i++)
        {
            board[i] = 0;
        }

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

        // Reset Status Text
        statusText.text = yourTurnText;
        Color statusColor = Color.white;
        statusColor.a = 1f;
        statusText.color = statusColor;

        // Fade out Reset Text
        if (resetFadeCoroutine != null)
        {
            StopCoroutine(resetFadeCoroutine);
        }
        resetFadeCoroutine = StartCoroutine(FadeOutResetText());

        gameEnded = false;
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
