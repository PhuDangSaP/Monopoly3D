using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject CardBuyProperty;
    public GameObject CountDown;

    private void Awake()
    {
        Instance = this;
    }


    public void CloseCardBuyProperty()
    {
        CardBuyProperty.SetActive(false);
    }
}
