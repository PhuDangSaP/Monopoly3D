using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }
    public bool isStarted;

    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>();
    [SerializeField] private float turnTimeLimit = 30f;
    private float currentTurnTime;
    private List<ulong> playerIds = new List<ulong>(); 
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private int maxTurns = 20;
    private int turnCount = 0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentPlayerTurn.OnValueChanged += OnTurnChanged;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
           
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {

        isStarted = false;
        StartCoroutine(StartGameAfterCountdown(5));
    }
    private void Update()
    {
        if (!isStarted || !IsServer) return;

        if (currentTurnTime > 0)
        {
            currentTurnTime -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTurnTime <= 0)
            {
                //NextTurnServerRpc();
            }
        }
        CheckEndGame();   
    }

    private void CheckEndGame()
    {
        if (turnCount >= maxTurns)
        {
            Debug.Log("Game Over: Max turns reached!");
            EndGame();
            return;
        }

        int activePlayers = 0;
        foreach (var playerId in playerIds)
        {
            var playerManager = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerManager>();
            if (playerManager != null && !playerManager.GetIsBankrupt())
            {
                activePlayers++;
            }
        }
        if (activePlayers <= 1)
        {
            Debug.Log("Game Over: Only one player left!");
            EndGame();
        }
    }

    private void EndGame()
    {
        ulong winnerId = 0;
        int maxMoney = int.MinValue;

        foreach (var playerId in playerIds)
        {
            var playerManager = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerManager>();
            if (playerManager != null)
            {
                int playerMoney = playerManager.GetMoney(); 
                if (playerMoney > maxMoney)
                {
                    maxMoney = playerMoney;
                    winnerId = playerId;
                }
            }
        }
        if (maxMoney > int.MinValue)
        {     
            AnnounceWinnerClientRpc(winnerId);
        }
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(ulong winnerId)
    {
        string winnerName = "Unknown";
        var players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in players)
        {
            var playerManager = player.GetComponent<PlayerManager>();
            if (playerManager != null && playerManager.OwnerClientId == winnerId)
            {
                winnerName = playerManager.name; // Giả sử PlayerManager có thuộc tính name
                break;
            }
        }

        if (winnerId == NetworkManager.Singleton.LocalClientId)
        {
            GameObject card = UIManager.Instance.YouWin;
            card.SetActive(true);
        }
        else
        {
            GameObject card = UIManager.Instance.GameOver;
            card.SetActive(true);

        }
    }
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            SyncPlayerIdsClientRpc();
        }
    }
    public void Init()
    {
        //SyncPlayerIdsServerRpc();

        Debug.Log("Current player index: " + currentPlayerTurn.Value);
        Debug.Log("------------------------------------------------------");
        Debug.Log("List player: " + playerIds.Count);
        foreach (var id in playerIds)
        {
            string name = "";
            var pl = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in pl)
            {
                if (player.GetComponent<PlayerManager>().OwnerClientId == id)
                {
                    name = player.GetComponent<PlayerManager>().name;
                    break;
                }
            }

            Debug.Log("Playerid of " + name + ": " + id);

        }
        Debug.Log("------------------------------------------------------");

    }
    [ServerRpc(RequireOwnership = false)]
    public void SyncPlayerIdsServerRpc()
    {
        Debug.Log("Server sync");
        SyncPlayerIdsClientRpc();
    }



    [ClientRpc]
    private void SyncPlayerIdsClientRpc()
    {
        Debug.Log("client init");
        playerIds.Clear();
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var playerManager = player.GetComponent<PlayerManager>();
            if (playerManager != null)
            {
                playerIds.Add(playerManager.OwnerClientId);
            }
        }
        playerIds = playerIds.OrderBy(id => id).ToList();
    }


    private IEnumerator StartGameAfterCountdown(int seconds)
    {
        TextMeshProUGUI countdown = UIManager.Instance.CountDown.GetComponentInChildren<TextMeshProUGUI>();
        while (seconds > 0)
        {
            if (countdown != null)
            {
                countdown.text = "Countdown: " + seconds;
            }
            yield return new WaitForSeconds(1);
            seconds--;
        }
        isStarted = true;
        if (countdown != null)
        {
            countdown.text = "Game Started!";
        }
        yield return new WaitForSeconds(2f);
        UIManager.Instance.CountDown.SetActive(false);
    }
    private void OnTurnChanged(int previousValue, int newValue)
    {
        UpdateTurnUI();
        if (IsMyTurn())
        {
            EnablePlayerActions();
        }
        else
        {
            DisablePlayerActions();
        }
    }

    private void EnablePlayerActions()
    {
        UIManager.Instance.ButtonRollDices.interactable = true;
    }
    private void DisablePlayerActions()
    {
        UIManager.Instance.ButtonRollDices.interactable = false;
    }
    [ServerRpc(RequireOwnership = false)]
    public void NextTurnServerRpc()
    {
        if (!isStarted) return;

        DiceManager.GetInstace().ResetAction();
        currentPlayerTurn.Value = (currentPlayerTurn.Value + 1) % playerIds.Count;
        currentTurnTime = turnTimeLimit;

        turnCount++;
        NetworkManager.Singleton.ConnectedClients[playerIds[currentPlayerTurn.Value]].PlayerObject.GetComponent<PlayerManager>().CheckInJail();
        NetworkManager.Singleton.ConnectedClients[playerIds[currentPlayerTurn.Value]].PlayerObject.GetComponent<PlayerManager>().CheckBankrupt();

        PlayerTurnNotificationClientRpc(playerIds[currentPlayerTurn.Value]);
    }

    [ClientRpc]
    private void PlayerTurnNotificationClientRpc(ulong playerId)
    {
        Debug.Log("Notificationclietnrpc");
        if (NetworkManager.Singleton.LocalClientId == playerId)
        {
            // Thông báo cho player hiện tại
        }
        else
        {
            // Thông báo cho các player còn lại
        }
        UpdateTurnUI();
    }

    public bool IsMyTurn()
    {
        if(playerIds.Count==0)
        {
            SyncPlayerIdsServerRpc();
        }
        return currentPlayerTurn.Value < playerIds.Count &&
               playerIds[currentPlayerTurn.Value] == NetworkManager.Singleton.LocalClientId;
    }
    private void UpdateTurnUI()
    {
        if (turnText != null)
        {
            var currentPlayerId = playerIds[currentPlayerTurn.Value];

            var players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                if (player.GetComponent<PlayerManager>().OwnerClientId == currentPlayerId)
                {
                    turnText.text = IsMyTurn() ?
                                    "Your Turn!" :
                                    $"{player.GetComponent<PlayerManager>().name}'s Turn";
                    break;
                }
            }


        }
    }
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(currentTurnTime)}s";
        }
    }

    public void Test()
    {
        Debug.Log("Current Player index " + currentPlayerTurn.Value);
    }
    public ulong GetCurrentPlayerId()
    {
        return playerIds[currentPlayerTurn.Value];
    }
}
