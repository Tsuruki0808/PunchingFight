using UnityEngine;
using NativeWebSocket;


using UnityEngine.SceneManagement;//デバック用で追加

public class Player : MonoBehaviour
{
    public int ID;

    public Vector3 MyPosition;   // 座標
    public Vector3 MyDir;        // 向いている方向
    public float Speed = 5f;     // 移動速度
    public float interval = 0.5f;// 球発射の間隔

    public GameObject bulletPrefab;
    public Transform firePoint;

    private float shotTimer = 0f;
    public bool isMove = true;  // ← 移動可能フラグ

    public Transform deathPos;//死亡したときに移動する場所(死亡地点にあるオブジェクトをアタッチする)

    [SerializeField] private GameObject shieldObject;

    [SerializeField] private UIScaleIn Scalen;

    public bool isLocal = true;
    PlayerManager pm;
    ws client;
    bool bulletFlg = false;


    // Player.cs の中での例
    private VirtualJoystick joystick;
    void Start()
    {
        Scalen = FindObjectOfType<UIScaleIn>();
        pm = FindObjectOfType<PlayerManager>();
        client = FindObjectOfType<ws>();
        MyPosition = transform.position;
        MyDir = transform.forward;
        shotTimer = interval;

        joystick = FindObjectOfType<VirtualJoystick>();
    }

    void Update()
    {
        if (!isMove) return;
        //球発射のinterval用
        shotTimer += Time.deltaTime;

        //場外に落下したら死亡
        if (transform.position.y <= -3f && isMove == true)
        {
            Death();
        }


        // 移動可能かつintervalが経過していたら移動
        if (isMove && shotTimer >= interval)
        {
            //移動、球の発射
            Move();
            SetShield(true);//つにに表示しているグローブの表示切替
        }
        else
        {
            SetShield(false);

        }


        if (!isLocal) return;

        //ここで座標の更新
        UpdatePos();

        SendPlayerData();
    }

    public void SetID(int id) { ID = id; }
    public int getID() { return ID; }

    //移動(後にここで計算した値を代入する)
    // 移動処理
    void Move()
    {
        if (!isLocal) return;

        float h = 0f;
        float v = 0f;

        Vector2 inputDir = Vector2.zero;

        // 1. まずバーチャルスティックからの入力を取得を試みる
        if (joystick != null)
        {
            inputDir = joystick.GetInputDirection();
        }

        // 2. スティックが操作されていない（ゼロの）場合、キーボード入力を受け付ける
        if (inputDir != Vector2.zero)
        {
            h = inputDir.x;
            v = inputDir.y;
        }
        else
        {
            // キーボード操作（PC用 / フォールバック）
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.A)) h = -1f;
            if (Input.GetKey(KeyCode.D)) h = 1f;
            if (Input.GetKey(KeyCode.W)) v = 1f;
            if (Input.GetKey(KeyCode.S)) v = -1f;

            // キーボードで何か入力されているときだけログに出す
            if (h != 0f || v != 0f)
            {
                Debug.Log("キーボード入力された値: " + new Vector2(h, v));
            }
        }

        Vector3 moveDir = new Vector3(h, 0f, v).normalized;

        MyPosition += moveDir * Speed * Time.deltaTime;

        if (moveDir != Vector3.zero)
        {
            MyDir = moveDir;
        }

        // ▼ 発射
        if (Input.GetKeyDown(KeyCode.Space) && shotTimer >= interval)
        {
            strike();
        }
    }

    //座標の更新
    void UpdatePos()
    {
        Vector3 pos = transform.position;
        pos.x = MyPosition.x;
        pos.z = MyPosition.z;
        transform.position = pos;

        if (MyDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(MyDir);
        }
    }

    //球の発射
    public void strike()
    {
        if (shotTimer >= interval)
        {
            if (bulletPrefab == null || firePoint == null) return;
            shotTimer = 0f;
            bulletFlg = true;
            Instantiate(
                bulletPrefab,
                firePoint.position,
                transform.rotation
            );
        }
    }

    //死亡(動けなくし、画面にfinishの文字を表示)
   public void Death()
    {
        isMove = false;

        if (deathPos != null)
        {
           // transform.position = deathPos.position;
           // MyPosition = deathPos.position; //内部座標も同期させる
        }

        if (Scalen != null)
        {
            Debug.Log("キャンバスの関数呼び出し！");
            Scalen.Play();
        }
        else
        {
            Debug.Log("オブジェクトが見つかりません");
        }
       // Destroy(gameObject);
    }

    //盾(グローブの表示非表示切り替え)
    void SetShield(bool enable)
    {
        if (shieldObject != null)
        {
            shieldObject.SetActive(enable);
        }
    }

    //serverへ自身の状態を送信.
    async void SendPlayerData()
    {
        if (client?.webSocket == null) return;
        if (client.webSocket.State != WebSocketState.Open) return;

        //送信データの作成.
        var data = new EventWrapper
        {
            @event = "action_input",
            playerId = pm.myPlayerId,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            strikeFlg = bulletFlg,
        };
        //JSON形式に変換してサーバーへ送信.
        await client.webSocket.SendText(JsonUtility.ToJson(data));
        bulletFlg = false;
    }

}
