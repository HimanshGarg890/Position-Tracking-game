
using UnityEngine;

public class animationPauser : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;

    private void ResetActionHappening()
    {
        player.actionHappening = false;
        animator.SetBool("actionHappening", false);
    }
}
