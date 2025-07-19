using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;
    [SerializeField] private GameEnder gameEnder;

    [SerializeField] private Image healthBarImage;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (currentHealth <= 0)
        {
            EndGame();
        }
    }
    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " is dead!");
            // Optional: destroy, disable, play animation, etc.
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = (float) currentHealth / maxHealth;
        }
            
    }

    private void EndGame()
    {
        gameEnder.EndGame();


    }
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
