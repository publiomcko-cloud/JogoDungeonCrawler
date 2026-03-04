using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health targetHealth;
    public Image fillImage;

    private void Update()
    {
        if (targetHealth == null) return;

        float percent = (float)targetHealth.currentHP / targetHealth.maxHP;
        fillImage.fillAmount = percent;
    }
}