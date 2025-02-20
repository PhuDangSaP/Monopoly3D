using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyGame : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    public TMP_InputField playerNameInput;
    public TMP_InputField lobbyNameInput;
    public TMP_InputField maxPlayersInput;

    public Transform contentLobby; // nơi chứa danh sách lobby
    public GameObject lobbyItemPrefab;

    public Transform contentPlayer; // nơi chứa danh sách player trong lobby
    public GameObject playerItemPrefab;

    public GameObject CreatePlayerUI;
    public GameObject ListLobbiesUI;
    public GameObject CreateLobbyUI;
    public GameObject InLobbyUI;
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }
    public async void CreatePlayer()
    {
        string playerName = playerNameInput.text;
        InitializationOptions options = new InitializationOptions();
        options.SetProfile(playerName);
        await UnityServices.InitializeAsync(options);
        if (!AuthenticationService.Instance.IsSignedIn)
        {


            AuthenticationService.Instance.SignedIn += () =>
                   {
                       Debug.Log("Signed in" + AuthenticationService.Instance.PlayerId + " " + AuthenticationService.Instance.Profile);
                   };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
           
        } 
        CreatePlayerUI.SetActive(false);
            ListLobbiesUI.SetActive(true);
            ListLobbies();


    }
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 2f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
                ListPlayersInLobby();

            }
        }
    }
    public async void CreateLobby()
    {
        try
        {
            string lobbyName = lobbyNameInput.text;
            int maxPlayers = Convert.ToInt32(maxPlayersInput.text);


            string relayCode = await RelayManager.Instance.StartHostWithRelay(maxPlayers - 1);

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        KEY_RELAY_JOIN_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member, relayCode)
                    }
                }
            };



            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            hostLobby = lobby;
            joinedLobby = hostLobby;
            Debug.Log("Created lobby" + lobby.Name + " " + lobby.MaxPlayers);
            ListLobbies();
            InLobbyUI.SetActive(true);
            CreateLobbyUI.SetActive(false);
            ListPlayersInLobby();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public void ListPlayersInLobby()
    {
        foreach (Transform child in contentPlayer)
        {
            Destroy(child.gameObject);
        }
        foreach (var player in joinedLobby.Players)
        {
            string playerName = player.Data["PlayerName"].Value;
            GameObject item = Instantiate(playerItemPrefab, contentPlayer);
            item.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = playerName;
        }
    }
    public async void ListLobbies()
    {
        try
        {
            foreach (Transform child in contentLobby)
            {
                Destroy(child.gameObject);
            }
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                GameObject item = Instantiate(lobbyItemPrefab, contentLobby);
                item.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
                item.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

                item.GetComponent<Button>().onClick.AddListener(() => OnLobbyItemClick(lobby));


            }

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public void OnLobbyItemClick(Lobby selectedLobby)
    {
        InLobbyUI.SetActive(true);
        InLobbyUI.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
        JoinLobbyById(selectedLobby.Id);
    }
    public async void JoinLobbyById(string id)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(id, options);
            joinedLobby = lobby;


            string relayCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            bool relayJoinSuccess = await RelayManager.Instance.StartClientWithRelay(relayCode);



            ListPlayersInLobby();
            //NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            joinedLobby = lobby;
            ListPlayersInLobby();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public void ToggleCreateLobbyUI()
    {
        CreateLobbyUI.SetActive(!CreateLobbyUI.activeSelf);
    }

    public async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private Player GetPlayer()
    {
        return new Player()
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject( PlayerDataObject.VisibilityOptions.Public,AuthenticationService.Instance.Profile )}
                    }
        };
    }
    public async void LeaveLobby()
    {
        try
        {
            if (joinedLobby != null)
            {
                if (joinedLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                }

                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                InLobbyUI.SetActive(false);
                ListLobbiesUI.SetActive(true);
                joinedLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Error while leaving lobby: {e.Message}");
        }
    }
    public async void ExitLobby()
    {
        try
        {
            if (joinedLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            NetworkManager.Singleton.Shutdown();
            InLobbyUI.SetActive(false);
            ListLobbiesUI.SetActive(true);

            joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Error while exiting lobby: " + e);
        }
    }
    public void StartGame()
    {
        //if (!NetworkManager.Singleton.IsServer)
        //{
        //    NetworkManager.Singleton.StartHost();
        //}

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
