using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Button ButtonRollDices;
    public GameObject CardBuyProperty;
    public GameObject CardBuyBackProperty;
    public GameObject CardChance;
    public GameObject CardTax;
    public GameObject CardChest;
    public GameObject CardGoToJail;
    public GameObject CardJail;
    public GameObject CountDown;
    public GameObject PlayerInfo;
    public GameObject Bankrupt;
    public GameObject YouWin;
    public GameObject GameOver;
    private void Awake()
    {
        Instance = this;
    }


    public void CloseCardBuyProperty()
    {
        CardBuyProperty.SetActive(false);
        TurnManager.Instance.NextTurnServerRpc();
    }
    public void CloseCardBuyBackProperty()
    {
        CardBuyBackProperty.SetActive(false);
        TurnManager.Instance.NextTurnServerRpc();
    }
    public void ExitToLobbyScene()
    {
        SceneManager.LoadScene("LobbyScene");
    }    
}
