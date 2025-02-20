using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerManager : NetworkBehaviour
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
    public void MovePlayer(int steps)
    {
        Debug.Log("move");
        int targetTileIndex = (currentTileIndex + steps) % BoardManager.GetInstance().GetCellDataLength();
        isMoving = true;
        //StartCoroutine(MoveToTile(targetTileIndex));
        StartCoroutine(MoveToTileNew(targetTileIndex));
    }

    private IEnumerator MoveToTileNew(int targetCellIndex)
    {
        Debug.Log("TEESSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSST");
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
            //while (Vector3.Distance(transform.position, nextCellPosition) > 0.1f)
            //{
            //    transform.position = Vector3.MoveTowards(transform.position, nextCellPosition, moveSpeed * Time.deltaTime); 
            //    yield return null;
            //}
            //Debug.Log("Sync move!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            //MovePlayerClientRpc(transform.position); // Đồng bộ vị trí với client
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
        switch (data.type)
        {
            case CellType.GO:
                TurnManager.Instance.NextTurnServerRpc();
                break;
            case CellType.GOTOJAIL:
                SoundManager.PlaySound(SoundManager.Sound.GotoJail);
                GotoJailClientRpc();
                break;
            case CellType.JAIL:
                JailClientRPC();
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
                ChestClientRpc(data.name);
                break;
            case CellType.PROPERTY:
                BankManager bankManager = BankManager.Instance;
                if (bankManager.IsPropertyOwned(currentTileIndex))
                {
                    if (OwnerClientId == bankManager.GetPropertyOwner(currentTileIndex))
                    {
                        // nâng cấp
                        Debug.Log("My house");
                    }
                    else
                    {
                        // trả tiền thuê
                        PayRentClientRpc(bankManager.GetPropertyOwner(currentTileIndex), data.name, data.price);
                    }
                    TurnManager.Instance.NextTurnServerRpc();
                }
                else
                {
                    //// mua
                    //// bật ui mua property
                    //GameObject card = UIManager.Instance.CardBuyProperty;
                    //card.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = data.name;
                    //card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = data.price.ToString();
                    //card.SetActive(true);

                    //Button btn = card.transform.GetChild(2).GetComponent<Button>();
                    //btn.onClick.RemoveAllListeners();
                    //if (money - data.price < 0)
                    //{
                    //    btn.gameObject.SetActive(false);
                    //}
                    //else
                    //{
                    //    btn.gameObject.SetActive(true);
                    //    btn.onClick.AddListener(BuyProperty);
                    //}
                    ShowBuyPropertyUiClientRpc(currentTileIndex, data.name, data.price);

                }
                break;
        }
        Debug.Log(data.type);
    }
    [ClientRpc]
    private void ShowBuyPropertyUiClientRpc(int tileIndex, string name, int price)
    {
        if (IsOwner)
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
            SpawnHouseServerRpc(data.position);
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
    private void SpawnHouseServerRpc(Vector2 position)
    {
        if (IsServer)
        {
            GameObject house = Instantiate(housePrefab);
            house.transform.position = new Vector3(position.x, 0.12f, position.y);
            house.GetComponent<NetworkObject>().Spawn();
            SetHouseColorClientRpc(house.GetComponent<NetworkObject>(), (int)OwnerClientId);

        }
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
        GameObject card = UIManager.Instance.CardBuyProperty;
        card.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        card.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = price.ToString();
        card.transform.GetChild(2).gameObject.SetActive(false);
        card.transform.GetChild(3).gameObject.SetActive(false);
        StartCoroutine(HideCardAfterDelay(card, 3));
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

            PlayerManager ownerPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerManager>();
            if (ownerPlayer != null)
            {
                ownerPlayer.AddMoney(price);

            }

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
            StartCoroutine(HideCardAfterDelay(UIManager.Instance.Bankrupt, 4));
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
    private void ChestClientRpc(string name)
    {
        GameObject chestCard = UIManager.Instance.CardChest;
        chestCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        StartCoroutine(HideCardAfterDelay(chestCard, 3));
    }

    [ClientRpc]
    private void GotoJailClientRpc()
    {
        Debug.Log("Go to Jail");
        GameObject goToJailCard = UIManager.Instance.CardGoToJail;
        goToJailCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Go To Jail!!!";
        StartCoroutine(HideCardAfterDelay(goToJailCard, 3));
        int index = BoardManager.GetInstance().GetIndexOfCellType(CellType.GOTOJAIL);
        Debug.Log("jaill comming");
        isMoving = true;
        StartCoroutine(MoveToTileNew(index));
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
    private void JailClientRPC()
    {
        isInJail = true;
        turnsInJail = 2;
        if (IsOwner)
        {
            TurnManager.Instance.NextTurnServerRpc();
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
        StartCoroutine(HideCardAfterDelay(chanceCard, 5));
        Debug.Log("ChaneCard: " + target);
        int index = BoardManager.GetInstance().GetIndexOfClosetCellType(target, currentTileIndex);
        isMoving = true;
        StartCoroutine(MoveToTileNew(index));
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

    public void MoveToChaneCard()
    {
        Debug.Log("Move to chance card");
        int index = BoardManager.GetInstance().GetIndexOfCellType(CellType.CHANCE);
        isMoving = true;
        StartCoroutine(MoveToTile(index));
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
}
