using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bullet : MonoBehaviour
{
    public float Speed = 10f;           // 移動速度
    public float VanishingPoint = 5f;   // どのくらい移動したら消えるか

    private Vector3 startPos;           // 発射時の位置

    [SerializeField] private UIScaleIn Scalen;

    // ▼ ① wsクラス（WebSocket通信用）の変数を宣言
    private ws client;

    // Start is called before the first frame update
    void Start()
    {
        Scalen = FindObjectOfType<UIScaleIn>();

        // ▼ ② Start() の中で ws コンポーネントを取得
        client = FindObjectOfType<ws>();

        // 発射された位置を保存
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // ▼ トランスフォームポジションで前方向に移動
        transform.position += transform.forward * Speed * Time.deltaTime;

        // ▼ どれだけ移動したか計算
        float distance = Vector3.Distance(startPos, transform.position);

        // もしバニシングポイント以上移動していたら
        if (distance >= VanishingPoint)
        {
            // 自身を削除
            Destroy(gameObject);
        }
    }

    // ▼ Triggerで相手オブジェクトに触れたか
    void OnTriggerEnter(Collider c)
    {
        // もし shield という名前のタグの付いたオブジェクトに当たってたら自身を削除
        if (c.CompareTag("shield"))
        {
            Debug.Log("盾に触れて消滅");
            Destroy(gameObject);
        }
        else if (c.CompareTag("Player"))
        {
            Rigidbody rb = c.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 dir = transform.forward;

                float horizontalPower = 20f; // 前に飛ぶ力
                float upPower = 25f;         // 上に飛ぶ力

                Vector3 force =
                    dir * horizontalPower +
                    Vector3.up * upPower;

                rb.AddForce(force, ForceMode.Impulse);
            }

            Player player = c.GetComponent<Player>();
            if (player != null)
            {
                int id = player.getID();
                ReportServer(id); // ← ここでサーバーにIDを伝える！
                player.Death();
            }

            Scalen.Play();

            Destroy(gameObject);
        }
    }

    // ③ 触れた相手のIDを取得して、サーバーに通知する
    async void ReportServer(int id)
    {
        Debug.Log("当たったプレイヤーID : " + id);

        if (client?.webSocket == null) return;
        if (client.webSocket.State != NativeWebSocket.WebSocketState.Open) return;

        // サーバーへ送るメッセージの作成
        var data = new EventWrapper
        {
            @event = "player_knockout",
            targetPlayerId = id.ToString() // 当たった相手のIDをセット
        };

        await client.webSocket.SendText(JsonUtility.ToJson(data));
        Debug.Log("サーバーへ player_knockout を送信しました！");
    }
}