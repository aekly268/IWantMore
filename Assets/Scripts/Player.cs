using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
public enum EPlayerState
{
    NORMAL = 1,
    STOP = 2
}

public class Player : MonoBehaviour
{
    public GameObject hand;
    public Transform posR, posL, tempScaleTransform;
    public int hp;

    SpriteRenderer sp, bodySp;
    Collider2D col, bodyCol;

    bool prevFaceR;
    float angle;
    float currentVelocity;
    bool isMoving;
    float nowScale;
    int nowHp;
    float hitColdDownTime;
    float feverTimer;
    float maxFeverTime;
    float speed;
    float maxTurnSpeed;
    float tempScale;
    float delayFatScale;
    int addScaleCounter;
    Vector3 RT, LB;

    EPlayerState state;
    Animator animator;
    MMF_Player feedbackPlayer;

    public const string COLLIDER_TAG_HAND = "hand";
    public const string COLLIDER_TAG_PLAYER = "Player";

    const float HAND_RATIO = 5;
    const float SPEED = 5;

    const float INITIAL_SCALE = 1.0f;
    const float ADD_SCALE = 0.1f;
    const float DONT_CHANGE_DIRECTION_OFFSET = 0.6f;
    const float MIN_DISTANCE = 0.1f;
    const float COLD_DOWN_TIME = 2.0f;
    const float ADD_FEVER_TIME = 0.05f;
    const float ADD_FEVER_TIME_LAST_DIFFICULTY = 0.005f;
    const float FEVER_TIME = 3.0f;
    const float SPEED_UP_RATIO = 4.0f;
    const float TURN_SPEED = 10000;
    const float SMOOTH_TIME = 0.001f;
    const float DELAY_GET_FAT_TIME = 0.5f;

    const string ANIMATOR_IS_MOVING_NAME = "isMoving";
    const string ANIMATOR_IS_FEVER_NAME = "isFever";
    const string ANIMATOR_IS_HIT_NAME = "isHit";
    const float FEVER_SCALE = 4.0f;
    
    void Awake()
    {        
        sp = hand.GetComponentInChildren<SpriteRenderer>();
        bodySp = GetComponentInChildren<SpriteRenderer>();
        bodyCol = GetComponentInChildren<CircleCollider2D>();
        col = hand.GetComponentInChildren<CircleCollider2D>();
        Reset();
        GameUIController.Instance.InitHp(nowHp);
        animator = GetComponentInChildren<Animator>();
        feedbackPlayer = GetComponent<MMF_Player>();        
        RT = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height));
        LB = Camera.main.ScreenToWorldPoint(new Vector3(0, 0));
    }

    public int GetHp()
    {
        return nowHp;
    }

    public void Reset()
    {
        state = EPlayerState.STOP;
        nowHp = hp;
        nowScale = INITIAL_SCALE;
        isMoving = false;
        prevFaceR = true;
        feverTimer = maxFeverTime = hitColdDownTime = 0;        
        speed = SPEED;
        maxTurnSpeed = TURN_SPEED;        
        transform.position = Vector2.zero;
        transform.localScale = new Vector2(nowScale, nowScale);        
    }

    Vector3 GetMouseDirection(Vector3 mousePosition)
    {
        return mousePosition - hand.transform.position;
    }

    void UpdateView(Vector3 direction, Vector3 mousePosition)
    {
        float targetAngle = Vector2.SignedAngle(Vector2.right, direction);
        angle = Mathf.SmoothDampAngle(angle, targetAngle, ref currentVelocity, SMOOTH_TIME, maxTurnSpeed);
        hand.transform.eulerAngles = new Vector3(0, 0, angle);

        float distance = Vector2.Distance(mousePosition, hand.transform.position);
        float length = distance * HAND_RATIO / transform.localScale.x;
        sp.size = new Vector2(length, 1);
        col.offset = new Vector2(length, 0);
    }
    
    void UpdateFaceTo(Vector3 direction)
    {        
        if (Mathf.Abs(direction.x) < (DONT_CHANGE_DIRECTION_OFFSET * nowScale))
        {
            return;           
        }

        bool faceToR = (direction.x > 0);
        if (prevFaceR != faceToR)
        {
            bodySp.flipX = !faceToR;
            hand.transform.localPosition = faceToR ? posR.localPosition : posL.localPosition;
            bodyCol.offset = new Vector2(-bodyCol.offset.x, bodyCol.offset.y);
        }
        prevFaceR = direction.x > 0;
    }

    void SetIsMoving(bool _isMoving)
    {
        isMoving = _isMoving;
        animator.SetBool(ANIMATOR_IS_MOVING_NAME, _isMoving);
    }

    void CheckControl()
    {
        if (state == EPlayerState.STOP)
            return;

#if !UNITY_EDITOR && UNITY_WEBGL
        bool isMoveState = true;
        if (GameManager.Instance.CheckIfMobile() && (!GameManager.Instance.isMovingStateForMobile || 
        GameUIController.Instance.CheckIsInChangeStateRect(Input.mousePosition)))
            isMoveState = false;
        
        if (Input.GetMouseButtonDown(0) && isMoveState)
        {
            SetIsMoving(true);            
        }
        else if (Input.GetMouseButtonUp(0) && isMoveState)
            SetIsMoving(false);
#else

        if (Input.GetMouseButtonDown(0))
        {
             SetIsMoving(true);

        }
        else if (Input.GetMouseButtonUp(0))
            SetIsMoving(false);
#endif

    }

/*
    Left Border -> x;
    Right Border -> z;
    Top Border -> w;
    Bottom Border -> y;
*/
    Vector3 CheckBounding(Vector3 pos)
    {
        float borderX = bodySp.sprite.border.z / bodySp.sprite.pixelsPerUnit;
        if (bodySp.bounds.max.x > RT.x + borderX)
            pos.x = RT.x - bodySp.bounds.extents.x + borderX;
        else if (bodySp.bounds.min.x + borderX < LB.x)
            pos.x = LB.x + bodySp.bounds.extents.x - borderX;

        float borderW = bodySp.sprite.border.w / bodySp.sprite.pixelsPerUnit;
        float borderB = bodySp.sprite.border.y / bodySp.sprite.pixelsPerUnit;
        if (bodySp.bounds.max.y > RT.y + borderW)
            pos.y = RT.y - bodySp.bounds.extents.y + borderW;
        else if (bodySp.bounds.min.y + borderB < LB.y)
            pos.y = LB.y + bodySp.bounds.extents.y - borderB;
        return pos;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 inputPosition = Input.mousePosition;
        //Debug.Log("inputPosition->" + inputPosition);

        if (inputPosition.x <= 0)
            inputPosition.x = 0;
        else if (inputPosition.x > Screen.width)
            inputPosition.x = Screen.width;
        if (inputPosition.y <= 0)
            inputPosition.y = 0;
        else if (inputPosition.y > Screen.height)
            inputPosition.y = Screen.height;


        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(inputPosition);
        Vector3 direction = GetMouseDirection(mousePosition);


        CheckControl();

        if (isMoving)
        {            
            Vector3 direction2 = mousePosition - hand.transform.position;
            UpdateFaceTo(direction2);
            direction.z = 0;
            transform.position += direction.normalized * speed * Time.deltaTime;
            transform.position = CheckBounding(transform.position);
            
            if (Vector2.Distance(mousePosition, hand.transform.position) <= MIN_DISTANCE)
            {
                SetIsMoving(false);
            }
        }

#if !UNITY_EDITOR && UNITY_WEBGL
        if (GameManager.Instance.CheckIfMobile() && 
        !GameUIController.Instance.CheckIsInChangeStateRect(Input.mousePosition) || !GameManager.Instance.CheckIfMobile())
            UpdateView(direction, mousePosition);//hand rotation & length
#else
        UpdateView(direction, mousePosition);//hand rotation & length
#endif

        if (hitColdDownTime > 0)
        {
            hitColdDownTime -= Time.deltaTime;
            if (hitColdDownTime <= 0)
            {
                bodyCol.enabled = true;
                animator.SetBool(ANIMATOR_IS_HIT_NAME, false);
            }
                
        }
        else if (feverTimer > 0)
        {
            feverTimer -= Time.deltaTime;
            if (feverTimer <= 0)
            {
                //Debug.Log("fever end");
                feverTimer = 0;
                bodyCol.tag = COLLIDER_TAG_PLAYER;
                GameUIController.Instance.SetFever(false);
                animator.SetBool(ANIMATOR_IS_FEVER_NAME, false);
                SetScale(1);
                delayFatScale = tempScale + ADD_SCALE * addScaleCounter;
                StartCoroutine("DelayFat");
                speed = SPEED;
                maxTurnSpeed = TURN_SPEED;
                AudioManager.Instance.PlaySound(EAudioClipKind.FEVER_END);
            }
        }
    }

    IEnumerator DelayFat()
    {
        yield return new WaitForSeconds(DELAY_GET_FAT_TIME);
        SetScale(delayFatScale);

    }
    void SetScale(float _scale)
    {
        tempScaleTransform.localScale = new Vector2(_scale, _scale);
        feedbackPlayer.PlayFeedbacks();
        nowScale = _scale;
    }
    public void SetState(EPlayerState _state)
    {
        state = _state;

    }

    public void SetEnd()
    {
        SetState(EPlayerState.STOP);
        isMoving = false;        
        animator.SetBool(ANIMATOR_IS_FEVER_NAME, true);
    }   
    
    public void SetHit()
    {
        if (hitColdDownTime > 0)
            return;

        nowHp--;
        GameUIController.Instance.SetHp(nowHp);
        animator.SetBool(ANIMATOR_IS_HIT_NAME, true);
        if (nowHp <= 0)
        {
            GameManager.Instance.OnPlayerDie();
        }
        else
        {
            AudioManager.Instance.PlaySound(EAudioClipKind.HURT);
            hitColdDownTime = COLD_DOWN_TIME;
            bodyCol.enabled = false;
        }
    }
    
    public void AddFeverTime()
    {
        if (feverTimer <= 0)
            return;

        if (GameManager.Instance.CheckIsLastStage())
            feverTimer += ADD_FEVER_TIME_LAST_DIFFICULTY;
        else
            feverTimer += ADD_FEVER_TIME;

        if (feverTimer > maxFeverTime)
            feverTimer = maxFeverTime;

        GameUIController.Instance.UpdateFeverTime(feverTimer);
    }

    
    public void SetFever()
    {
        StopCoroutine("DelayFat");
        if (feverTimer <= 0)
        {
            tempScale = nowScale;
            addScaleCounter = 0;
        }
        addScaleCounter++;
        feverTimer += FEVER_TIME;
        maxFeverTime = feverTimer;
        bodyCol.tag = COLLIDER_TAG_HAND;        
        speed = SPEED * SPEED_UP_RATIO;
        maxTurnSpeed = TURN_SPEED * SPEED_UP_RATIO;
        animator.SetBool(ANIMATOR_IS_FEVER_NAME, true);
        GameUIController.Instance.SetFever(true, feverTimer);
        SetScale(FEVER_SCALE);
    }
}
