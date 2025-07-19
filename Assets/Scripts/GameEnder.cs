using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class GameEnder : MonoBehaviour
{

    [SerializeField] Player player1;
    [SerializeField] Player2 player2;
    [SerializeField] Canvas endText;
    public void EndGame()
    {
        player1.GameOver();
        player2.GameOver();

        if (player1.gameObject.GetComponent<HealthSystem>().GetCurrentHealth() <= 0)
        {
            
            endText.GetComponentInChildren<TextMeshProUGUI>().text = "GAME OVER! Player 2 wins!";
            endText.gameObject.SetActive(true);
        }
        else
        {
            endText.GetComponentInChildren<TextMeshProUGUI>().text = "GAME OVER! Player 1 wins!";
            endText.gameObject.SetActive(true);
        }
    }
}
