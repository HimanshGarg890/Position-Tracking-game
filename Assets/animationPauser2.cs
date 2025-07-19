using UnityEngine;

public class animationPauser2 : MonoBehaviour
{
    [SerializeField] private Player2 player;
    [SerializeField] private Animator animator;

    private void ResetActionHappening()
    {
        player.actionHappening = false;
        animator.SetBool("actionHappening", false);
    }
}
