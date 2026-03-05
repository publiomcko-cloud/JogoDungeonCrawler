using UnityEngine;
using UnityEngine.UI; // Necessário para acessar os componentes Graphic e Image

public class HealthBar : MonoBehaviour
{
    public Health targetHealth;      
    public Image fillImage;           

    void Start()
    {
        // 1. Pega todos os elementos visuais desta barra (fundo, preenchimento, bordas, etc.)
        Graphic[] uiElements = GetComponentsInChildren<Graphic>();
        
        foreach (Graphic ui in uiElements)
        {
            // 2. Cria uma cópia do material padrão da UI para não afetar outras interfaces do jogo
            Material materialOnTop = new Material(ui.material);
            
            // 3. Força o Teste de Profundidade (ZTest) para 'Always' (valor 8).
            // Isso diz à Unity: "Não importa o que esteja na frente, desenhe isso por cima!"
            materialOnTop.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            
            // 4. Aplica o novo material mágico
            ui.material = materialOnTop;
        }
    }

    void Update()
    {
        if (targetHealth == null || fillImage == null) return;
        
        if (targetHealth.maxHP <= 0) return; 

        // Atualiza a vida normalmente com proteção matemática
        float percent = (float)targetHealth.currentHP / targetHealth.maxHP;
        fillImage.fillAmount = Mathf.Clamp01(percent);
    }
}