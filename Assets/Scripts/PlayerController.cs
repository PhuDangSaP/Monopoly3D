using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    private int currentTileIndex = 0;
    private bool isMoving = false;
    private int money = 0;
    private bool isInJail = false;
    private int turnsInJail = 0;
    private bool isBankrupt = false;
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private Material[] playerHouseMaterials;

    public string name { get; private set; }
    private TextMeshProUGUI moneyUI;
    private void Awake()
    {
        currentTileIndex = 0;
        money = 1000;
        Debug.Log(AuthenticationService.Instance.Profile);
        name = AuthenticationService.Instance.Profile.ToString();

        SceneManager.sceneLoaded += SceneLoaded;

    }
    public void MovePlayer(int steps) // đc gọi từ server
    {
        Debug.Log("move");
        int targetTileIndex = (currentTileIndex + steps) % BoardManager.GetInstance().GetCellDataLength();
        isMoving = true;
        //StartCoroutine(MoveToTile(targetTileIndex));
        StartCoroutine(MoveToTileNew(targetTileIndex));
    }

    private IEnumerator MoveToTileNew(int targetCellIndex)
    {
        while (currentTileIndex != targetCellIndex)
        {
            if (currentTileIndex == BoardManager.GetInstance().GetCellDataLength() - 1)
            {
                Go();
            }
            currentTileIndex = (currentTileIndex + 1) % BoardManager.GetInstance().GetCellDataLength();
            Vector2 cellPos = BoardManager.GetInstance().GetCellData(currentTileIndex).position;
            Vector3 nextCellPosition = new Vector3(cellPos.x, transform.position.y, cellPos.y);

            transform.position = nextCellPosition; ;
            MovePlayerClientRpc(nextCellPosition, currentTileIndex); // Đồng bộ vị trí với client

            yield return new WaitForSeconds(0.1f);
        }
        isMoving = false;
        HandleEvent();
    }
    [ClientRpc]
    private void MovePlayerClientRpc(Vector3 newPosition, int newCurrentTileIndex)
    {
        if (!IsServer)
        {
            transform.position = newPosition;
            currentTileIndex = newCurrentTileIndex;
        }
    }

    private IEnumerator MoveToTile(int targetCellIndex)
    {
        Debug.Log("Move to: " + targetCellIndex);
        while (currentTileIndex != targetCellIndex)
        {
            if (currentTileIndex == BoardManager.GetInstance().GetCellDataLength() - 1)
            {
                Go();
            }
            currentTileIndex = (currentTileIndex + 1) % BoardManager.GetInstance().GetCellDataLength();
            Vector2 cellPos = BoardManager.GetInstance().GetCellData(currentTileIndex).position;
            Vector3 nextCellPosition = new Vector3(cellPos.x, transform.position.y, cellPos.y);

            while (Vector3.Distance(transform.position, nextCellPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, nextCellPosition, moveSpeed * Time.deltaTime);

                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }
        isMoving = false;
        HandleEvent();
    }
    public bool GetIsMoving()
    {
        return isMoving;
    }
    public void HandleEvent()
    {
        DiceManager.GetInstace().ResetAction();
        CellData data = BoardManager.GetInstance().GetCellData(currentTileIndex);
        ulong currentPlayerId = TurnManager.Instance.GetCurrentPlayerId();
        switch (data.type)
        {
            case CellType.GO:
                TurnManager.Instance.NextTurnServerRpc();
                break;
            case CellType.GOTOJAIL:
                SoundManager.PlaySound(SoundManager.Sound.GotoJail);
                GotoJailClientRpc(currentPlayerId);
                break;
            case CellType.JAIL:
                SoundManager.PlaySound(SoundManager.Sound.GotoJail);
                JailClientRPC(currentPlayerId);
                break;
            case CellType.FREEPARKING:
                TurnManager.Instance.NextTurnServerRpc();
                break;
            case CellType.CHANCE:
                HandleChanceClientRpc();
                break;
            case CellType.TAX:
                TurnManager.Instance.NextTurnServerRpc();
                PayTaxClientRpc(data.name, data.price);
                break;
            case CellType.CHEST:
                TurnManager.Instance.NextTurnServerRpc();
                ChestClientRpc(currentPlayerId, data.name);
                break;
            case CellType.PROPERTY:
                HandlePropertyEventServerRpc(currentTileIndex);
                break;
        }
        Debug.Log(data.type);
    }
    [ServerRpc(RequireOwnership = false)]
    private void HandlePropertyEventServerRpc(int tileIndex, ServerRpcParams rpcParams = default)
    {
        BankManager bankManager = BankManager.Instance;
        ulong currentClientId = TurnManager.Instance.GetCurrentPlayerId();
        CellData data = BoardManager.GetInstance().GetCellData(currentTileIndex);
        if (bankManager.IsPropertyOwned(currentTileIndex))
        {
            ulong ownerId = bankManager.GetPropertyOwner(currentTileIndex);
            if (currentClientId == ownerId) // Chủ của property
            {
                Debug.Log("Client" + currentClientId + " is on his property");
                // Xử lý nâng cấp
                TurnManager.Instance.NextTurnServerRpc();
            }
            else // đi vào property của client khác
            {

                Debug.Log("Client" + currentClientId + " is on " + ownerId + " property");
                // Xử lý trả tiền thuê
                PlayerController senderClient = NetworkManager.Singleton.ConnectedClients[currentClientId].PlayerObject.GetComponent<PlayerController>();

                DecreaseClientMoneyServerRpc(currentClientId, data.price); // đồng bộ trừ tiền cho  client đang xử lý xử kiện

                AddClientMoneyServerRpc(ownerId, data.price); // đồng bộ cộng tiền cho chủ property

                int buyBackPrice = (int)(data.price * 1.5f);
                if (senderClient.GetMoney() >= buyBackPrice) // kiểm tra người chơi có đủ tiền mua lại property ko
                {
                    ShowBuyBackPropertyUiClientRpc(currentClientId, ownerId, tileIndex, data.name, buyBackPrice); // Bật ui mua lại property cho sender client
                }
                else
                {
                    TurnManager.Instance.NextTurnServerRpc();
                }
            }
        }
        else // property chưa ai mua
        {
            Debug.Log($"[Server] Calling ShowBuyPropertyUiClientRpc for Client {currentClientId}");
            ShowBuyPropertyUiClientRpc(currentClientId, tileIndex, data.name, data.price); // Bật ui mua property cho sender client
        }
    }


    [ClientRpc]
    private void ShowBuyBackPropertyUiClientRpc(ulong clientId, ulong ownerId, int tileIndex, string name, int price)
    {
        if (IsOwner && NetworkManager.Singleton.LocalClientId == clientId)
        {
            GameObject card = UIManager.Instance.CardBuyBackProperty;
            card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = name;
            card.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = price.ToString();
            card.transform.GetChild(3).gameObject.SetActive(true);
            card.transform.GetChild(4).gameObject.SetActive(true);

            Button btnBuyBack = card.transform.GetChild(3).GetComponent<Button>();
            btnBuyBack.onClick.RemoveAllListeners();
            btnBuyBack.onClick.AddListener(() =>
            {
                DecreaseClientMoneyServerRpc(clientId, price); // trả tiền mua lại property
                RequestBuyBackServerRpc(ownerId, tileIndex, price); // cập nhập lại chủ của property
                AddClientMoneyServerRpc(ownerId, price);  // cộng tiền cho chủ củ
                card.SetActive(false);
                TurnManager.Instance.NextTurnServerRpc();
            });

            card.SetActive(true);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestBuyBackServerRpc(ulong ownerId, int tileIndex, int offerPrice, ServerRpcParams rpcParams = default)
    {
        ulong buyerId = rpcParams.Receive.SenderClientId;

        // Lấy thông tin người chơi mua lại
        PlayerController buyer = NetworkManager.Singleton.ConnectedClients[buyerId].PlayerObject.GetComponent<PlayerController>();

        // Cập nhật chủ sở hữu mới
        BankManager.Instance.RemovePropertyOwner(tileIndex);
        BankManager.Instance.SetPropertyOwner(tileIndex, buyerId);

        // Xóa nhà cũ
        CellData data = BoardManager.GetInstance().GetCellData(tileIndex);
        if (data.houseObject != null)
        {
            NetworkObject netObj = data.houseObject.GetComponent<NetworkObject>();
            if (netObj.IsSpawned)
            {
                netObj.Despawn(true); // Hủy bỏ object trên tất cả client
            }

            data.houseObject = null; // Xóa reference trong CellData
        }
        // Spawn nhà mới
        SpawnHouseServerRpc(tileIndex);
    }



    [ClientRpc]
    private void ShowBuyPropertyUiClientRpc(ulong clientId, int tileIndex, string name, int price)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            GameObject card = UIManager.Instance.CardBuyProperty;
            card.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
            card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = price.ToString();
            card.transform.GetChild(2).gameObject.SetActive(true);
            card.transform.GetChild(3).gameObject.SetActive(true);
            card.SetActive(true);

            Button btn = card.transform.GetChild(2).GetComponent<Button>();
            btn.onClick.RemoveAllListeners();

            if (money - price < 0)
            {
                btn.gameObject.SetActive(false);
            }
            else
            {
                btn.gameObject.SetActive(true);
                btn.onClick.AddListener(() =>
                {
                    BuyProperty();
                    card.SetActive(false);
                });
            }
        }
    }

    public void BuyProperty()
    {
        CellData data = BoardManager.GetInstance().GetCellData(currentTileIndex);
        int propertyPrice = data.price;
        if (money >= propertyPrice)
        {
            BuyPropertyServerRpc(currentTileIndex, propertyPrice);

            SpawnHouseServerRpc(currentTileIndex);
        }
        else
        {
            Debug.Log("Not enough money to buy  property!");
        }
        UIManager.Instance.CardBuyProperty.SetActive(false);
        TurnManager.Instance.NextTurnServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void BuyPropertyServerRpc(int tileIndex, int propertyPrice, ServerRpcParams rpcParams = default)
    {
        ulong buyerId = rpcParams.Receive.SenderClientId;
        BankManager.Instance.SetPropertyOwner(tileIndex, buyerId);
        // Thông báo cho client cập nhật UI và số tiền
        UpdatePropertyOwnerClientRpc(tileIndex, buyerId, propertyPrice);
    }
    [ClientRpc]
    private void UpdatePropertyOwnerClientRpc(int tileIndex, ulong ownerId, int propertyPrice)
    {
        if (IsOwner && OwnerClientId == ownerId)
        {
            money -= propertyPrice;
            SoundManager.PlaySound(SoundManager.Sound.DecreaseMoney);
            UpdateMoneyUI();
            Debug.Log($"You bought property at index {tileIndex}.");
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void SpawnHouseServerRpc(int tileIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        CellData data = BoardManager.GetInstance().GetCellData(tileIndex);
        if (data.houseObject != null) return;

        GameObject house = Instantiate(housePrefab);
        Vector2 housePos = data.position + data.houseOffset;
        house.transform.position = new Vector3(housePos.x, 0.12f, housePos.y);

        NetworkObject netObj = house.GetComponent<NetworkObject>();
        netObj.Spawn();

        data.houseObject = house;
        SetHouseColorClientRpc(house.GetComponent<NetworkObject>(), (int)OwnerClientId);
        // Thông báo cho client cập nhật dữ liệu
        UpdateHouseClientRpc(tileIndex, netObj.NetworkObjectId);
    }
    [ClientRpc]
    private void UpdateHouseClientRpc(int tileIndex, ulong houseNetId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(houseNetId, out NetworkObject houseObj))
        {
            Debug.LogError("House object not found on client!");
            return;
        }

        // Lưu nhà vào dữ liệu trên client
        CellData data = BoardManager.GetInstance().GetCellData(tileIndex);
        data.houseObject = houseObj.gameObject;
    }

    [ClientRpc]
    private void SetHouseColorClientRpc(NetworkObjectReference houseRef, int materialIndex)
    {
        if (houseRef.TryGet(out NetworkObject houseObj))
        {
            MeshRenderer renderer = houseObj.GetComponent<MeshRenderer>();
            renderer.material = playerHouseMaterials[materialIndex];

        }
    }

    [ClientRpc]
    private void PayRentClientRpc(ulong clientId, string name, int price)
    {
        //GameObject card = UIManager.Instance.CardBuyProperty;
        //card.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        //card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = price.ToString();
        //card.transform.GetChild(2).gameObject.SetActive(false);
        //card.transform.GetChild(3).gameObject.SetActive(false);
        //StartCoroutine(HideCardAfterDelay(card, 3));
        Debug.Log("PayRent");

        if (money - price < 0)
        {
            // bankrupt
            isBankrupt = true;
            SoundManager.PlaySound(SoundManager.Sound.Bankrupt);
            StartCoroutine(HideCardAfterDelay(UIManager.Instance.Bankrupt, 4));
        }
        else
        {
            money -= price;
            SoundManager.PlaySound(SoundManager.Sound.DecreaseMoney);

            UpdateMoneyUI();

            PlayerController ownerPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
            if (ownerPlayer != null)
            {
                AddClientMoneyServerRpc(clientId, price);
            }

        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void AddClientMoneyServerRpc(ulong clientId, int amount)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            PlayerController player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.AddMoney(amount);
                UpdateMoneyClientRpc(clientId, player.GetMoney());
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void DecreaseClientMoneyServerRpc(ulong clientId, int amount)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            PlayerController player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.DecreaseMoney(amount);
                UpdateMoneyClientRpc(clientId, player.GetMoney());
            }
        }
    }

    [ClientRpc]
    private void UpdateMoneyClientRpc(ulong clientId, int amount)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            money = amount;
            UpdateMoneyUI();
        }
    }

    [ClientRpc]
    private void PayTaxClientRpc(string name, int price)
    {
        GameObject taxCard = UIManager.Instance.CardTax;
        taxCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        taxCard.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = price.ToString();
        StartCoroutine(HideCardAfterDelay(taxCard, 5));

        if (money - price < 0)
        {
            // phá sản
            isBankrupt = true;
            SoundManager.PlaySound(SoundManager.Sound.Bankrupt);
            StartCoroutine(HideCardAfterDelay(UIManager.Instance.Bankrupt, 2));
        }
        else
        {
            Debug.Log("money down");
            money -= price;
            SoundManager.PlaySound(SoundManager.Sound.DecreaseMoney);
            UpdateMoneyUI();

        }

    }

    [ClientRpc]
    private void ChestClientRpc(ulong clientId, string name)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            GameObject chestCard = UIManager.Instance.CardChest;
            chestCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
            int amount = 150;
            chestCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Grant: " + amount;
            StartCoroutine(HideCardAfterDelay(chestCard, 1));

            AddClientMoneyServerRpc(clientId, amount);
        }
    }

    [ClientRpc]
    private void GotoJailClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            StartCoroutine(GotoJailSequence());
        }
    }

    private IEnumerator GotoJailSequence()
    {
        Debug.Log("Go to Jail");
        GameObject goToJailCard = UIManager.Instance.CardGoToJail;
        goToJailCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Go To Jail!!!";

        yield return StartCoroutine(HideCardAfterDelay(goToJailCard, 3));

        int index = BoardManager.GetInstance().GetIndexOfCellType(CellType.GOTOJAIL);
        Debug.Log("Jail coming");
        isMoving = true;

        yield return StartCoroutine(MoveToTileNew(index));
    }
    private IEnumerator HideCardAfterDelay(GameObject card, float delayTime)
    {
        card.SetActive(true);
        yield return new WaitForSeconds(delayTime);
        card.SetActive(false);
    }
    private void Go()
    {
        money += 200;
        SoundManager.PlaySound(SoundManager.Sound.IncreaseMoney);
        UpdateMoneyUI();
    }
    [ClientRpc]
    private void JailClientRPC(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            StartCoroutine(HideCardAfterDelay(UIManager.Instance.CardJail, 2));
            isInJail = true;
            turnsInJail = 2;
            if (IsOwner)
            {
                TurnManager.Instance.NextTurnServerRpc();
            }
        }
    }

    public void CheckInJail()
    {
        if (isInJail)
        {
            turnsInJail--;
            if (turnsInJail <= 0)
            {
                ResetJailClientRpc();
            }
            TurnManager.Instance.NextTurnServerRpc();
        }
    }
    [ClientRpc]
    private void ResetJailClientRpc()
    {
        isInJail = false;
        turnsInJail = 0;
    }
    public void CheckBankrupt()
    {
        if (isBankrupt)
        {
            TurnManager.Instance.NextTurnServerRpc();
        }
    }

    [ClientRpc]
    private void HandleChanceClientRpc()
    {
        StartCoroutine(HandleChanceSequence());
    }

    private IEnumerator HandleChanceSequence()
    {
        GameObject chanceCard = UIManager.Instance.CardChance;
        TextMeshProUGUI decription = chanceCard.GetComponentInChildren<TextMeshProUGUI>();
        CellType target;

        switch (ChanceHandler.Instance.DrawChaneCard().type)
        {
            case ChanceType.GOTOJAIL:
                decription.text = "Go to jail";
                target = CellType.JAIL; break;
            case ChanceType.GOTOSTART:
                decription.text = "Go to start";
                target = CellType.GO; break;
            case ChanceType.GOTOTAX:
                decription.text = "Go to tax";
                target = CellType.TAX; break;
            default:
                target = CellType.GO; break;
        }

        SoundManager.PlaySound(SoundManager.Sound.Chance);

        // Chờ UI hiển thị trong 5 giây trước khi xử lý sự kiện
        yield return StartCoroutine(HideCardAfterDelay(chanceCard, 5));

        Debug.Log("ChaneCard: " + target);

        int index = BoardManager.GetInstance().GetIndexOfClosetCellType(target, currentTileIndex);
        isMoving = true;

        yield return StartCoroutine(MoveToTileNew(index));

        if (IsOwner)
        {
            TurnManager.Instance.NextTurnServerRpc();
        }
    }
    public void UpdateMoneyUI()
    {
        if (IsOwner)
        {
            moneyUI.text = "Money: " + money.ToString();
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UpdateClientNameClientRpc(name);
        if (IsOwner)
        {
            Debug.Log("Spawn " + name);
        }
        else
        {
            Debug.Log("not me");
        }
    }

    [ClientRpc]
    private void UpdateClientNameClientRpc(string name)
    {
        this.name = name;
    }

    private void SceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == "GameScene")
        {
            Debug.Log("Scene Loaded");

            TurnManager.Instance.Test();
            SpawnPlayerServerRpc();
            GameObject playerInfo = UIManager.Instance.PlayerInfo;
            playerInfo.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Name: " + name;
            moneyUI = playerInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            UpdateMoneyUI();
            if (IsServer)
            {
                //TurnManager.Instance.Init();
                TurnManager.Instance.SyncPlayerIdsServerRpc();
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc()
    {
        if (IsServer)
        {
            GameObject slots = GameObject.Find("Slots");
            for (int i = 0; i < slots.transform.childCount; i++)
            {
                Transform slotChild = slots.transform.GetChild(i);
                if (slotChild.childCount == 0)
                {
                    GameObject temp = new GameObject();
                    temp.transform.SetParent(slotChild);
                    SpawnPlayerInSlotClientRpc(slotChild.position, Quaternion.Euler(-90, 0, 0)); // Thông báo cho các client spawn player
                    break;
                }
            }
        }
    }
    [ClientRpc]
    private void SpawnPlayerInSlotClientRpc(Vector3 position, Quaternion rotation)
    {
        if (IsOwner)
        {
            transform.position = new Vector3(position.x, 0.8f, position.z);
            transform.rotation = rotation;
        }
    }
    public bool GetIsBankrupt()
    {
        return isBankrupt;
    }
    public int GetMoney()
    {
        return money;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        SoundManager.PlaySound(SoundManager.Sound.IncreaseMoney);
        UpdateMoneyUI();
    }
    public void DecreaseMoney(int amount)
    {
        money -= amount;
        SoundManager.PlaySound(SoundManager.Sound.DecreaseMoney);
        UpdateMoneyUI();
    }
}
