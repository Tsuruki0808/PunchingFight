using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("移動範囲の設定")]
    [SerializeField] private float movementRange = 50f; //初期位置から動かせる最大の範囲

    private RectTransform rectTransform;
    private Vector2 startPos;
    private Vector2 inputDirection = Vector2.zero;
    private Canvas canvas;
    private Camera cam;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        //ゲーム開始時の位置を初期位置として記憶
        startPos = rectTransform.anchoredPosition;

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            cam,
            out localPoint
        );

        //初期位置からのズレを計算
        Vector2 offset = localPoint - startPos;

        //指定した範囲（movementRange）の中に収まるように制限する
        offset = Vector2.ClampMagnitude(offset, movementRange);

        //制限された位置に移動させる
        rectTransform.anchoredPosition = startPos + offset;

        //入力値を計算して保持
        if (movementRange > 0f)
        {
            inputDirection = offset / movementRange;
        }
        else
        {
            inputDirection = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //離したら初期位置に戻す
        rectTransform.anchoredPosition = startPos;
        inputDirection = Vector2.zero;
    }

    //プレイヤーの移動スクリプトから呼び出して入力値を受け取る用
    public Vector2 GetInputDirection()
    {
        return inputDirection;
    }
}