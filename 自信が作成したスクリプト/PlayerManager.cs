using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    [SerializeField] private GameObject localPlayerObj;
    ws ws;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>(); // プレイヤーの一覧
    public string myPlayerId; // 自分のプレイヤーID

    public void CreatePlayer(string id, Vector3 pos, bool localFlg)
    {
        // プレイヤーオブジェクト生成
        var player = Instantiate(playerPrefab, pos, Quaternion.identity);
        if (player.GetComponent<Player>() != null)
        {
            player.GetComponent<Player>().isLocal = localFlg;
        }

        //if (!localFlg)
        //{
        //    var rb = player.GetComponent<Rigidbody>();
        //    if (rb != null)
        //    {
        //        rb.isKinematic = true;
        //        rb.useGravity = false;
        //    }

        //    var colliders = player.GetComponentsInChildren<Collider>();
        //    foreach (var col in colliders)
        //    {
        //        col.enabled = false;
        //    }
        //}

        player.SetActive(true);
        // リスト追加
        players[id] = player;
    }

    public void RegisterLocalPlayer(GameObject localPlayer, string id)
    {
        players[id] = localPlayer;
    }

    void Awake()
    {
        QualitySettings.vSyncCount = 0; // VSync無効化
        Application.targetFrameRate = 60; // フレームレートを固定
        Time.fixedDeltaTime = 1f / 60f; // 物理更新間隔も固定
    }
    public void UpdatePlayer(EventWrapper pd)
    {
        if (pd.playerId == myPlayerId) return;

        if (players.ContainsKey(pd.playerId))
        {
            var player = players[pd.playerId];

           
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null && !playerScript.isMove)
            {
                return; 
            }

            Vector3 prevPos = player.transform.position;
            Vector3 newPos = new Vector3(pd.x, pd.y, pd.z);

            Vector3 moveDir = new Vector3(pd.x - prevPos.x, 0, pd.z - prevPos.z);

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, 0.2f);
            }

            player.transform.position = newPos;
            if (pd.strikeFlg == true)
            {
                if (playerScript != null)
                {
                    playerScript.strike();
                }
            }
        }
    }

    public void HandleWebSocketMessage(string msg)
    {
        var playerData = JsonUtility.FromJson<EventWrapper>(msg);
        if (playerData.playerId == myPlayerId) return;

        //Debug.Log(msg);

        if (!players.ContainsKey(playerData.playerId))
        {
            Vector3 spawnPos = new Vector3(playerData.x, playerData.y, playerData.z);
            CreatePlayer(playerData.playerId, spawnPos, false);
        }
        else
        {
            UpdatePlayer(playerData);
        }
    }
    public void RemovePlayer(string id)
    {
        if (players.TryGetValue(id, out GameObject obj))
        {
            Destroy(obj);
            players.Remove(id);
        }
    }

    public void ClearAllPlayers()
    {
        foreach (var player in players.Values)
        {
            if (player != null)
            {
                Destroy(player);
            }
        }
        players.Clear();
    }

    void Start()
    {
        ws = FindObjectOfType<ws>();

        if (ws != null)
        {
            // 初期化（プレイヤー生成など）
            myPlayerId = ws.playerId;
            RegisterLocalPlayer(localPlayerObj, myPlayerId);           
        }
    }

    // ▼ 指定したIDのプレイヤーを強制的に死亡（デス）させる
    public void KnockoutPlayer(string id)
    {
        if (players.ContainsKey(id))
        {
            var playerObj = players[id];
            if (playerObj != null)
            {
                var playerScript = playerObj.GetComponent<Player>();
                if (playerScript != null)
                {
                    Debug.Log($"[PlayerManager] プレイヤーID: {id} をノックアウトします！");
                    playerScript.Death();
                }
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] ノックアウト対象のIDが見つかりませんでした: {id}");
        }
    }
}
