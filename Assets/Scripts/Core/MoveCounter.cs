using UnityEngine;
using TMPro;

public class MoveCounter : MonoBehaviour
{
    // reference to ui text component that displays moves
    [SerializeField] private TextMeshProUGUI moveText;
    // maximum number of moves allowed
    [SerializeField] private int maxMoves = 15;

    // current number of moves remaining
    private int currentMoves;

    // initialize moves on start
    private void Start()
    {
        ResetMoves();
    }

    // reset moves back to maximum value
    public void ResetMoves()
    {
        currentMoves = maxMoves;
        UpdateMoveText();
    }

    // decrease moves by one if any remaining
    public void UseMove()
    {
        if (currentMoves > 0)
        {
            currentMoves--;
            UpdateMoveText();
        }
    }

    // update the ui text with current moves
    private void UpdateMoveText()
    {
        if (moveText != null)
        {
            moveText.text = $"{currentMoves}";
        }
    }

    // get the number of moves left
    public int GetRemainingMoves()
    {
        return currentMoves;
    }

    // check if there are any moves available
    public bool HasMovesLeft()
    {
        return currentMoves > 0;
    }

    // set new maximum moves and reset
    public void SetMaxMoves(int moves)
    {
        maxMoves = moves;
        ResetMoves();
    }
}