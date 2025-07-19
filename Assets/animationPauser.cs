using UnityEngine;

public class animationPauser : MonoBehaviour
{
    [SerializeField] private Player player;

    private void ResetActionHappening()
    {
        player.actionHappening = false;
    }
}
