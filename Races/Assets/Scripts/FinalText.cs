using TMPro;
using UnityEngine;

public class FinalText : MonoBehaviour
{
    public TextMeshProUGUI finalText;

    void Update()
    {
        finalText.text = GameData.finalText;
    }
}
