using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerCtrl : MonoBehaviourPunCallbacks, IPunObservable
{
    public enum PlayerState
    {
        idle,
        move,
        run,
        shot,        
        death
    }

    public PlayerState playerstate = PlayerState.idle;
    public PlayerState Netplayerstate = PlayerState.idle;

    //이동 및 회전 속도를 나타내는 변수
    public float moveSpeed = 10.0f;
    public float rotSpeed = 50.0f;
    public Transform playerTr;

    public bool iszoomOnOff = false;

    //참조할 컴포넌트를 할당할 변수
    public Rigidbody rbody;

    public float walkSpeed = 1.0f;

    [SerializeField]
    private float lookSensitivity;

    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX;

    [SerializeField]
    public Camera theCamera;
    public GameObject RightArm;

    [HideInInspector]public Animator animator;

    bool isGround = true;

    public bool isRun = false;

    bool isShot = false;

    //총알 발사 관련 변수
    public MeshRenderer muzzleFlash;
    private RaycastHit hit;
    float fireDur = 0.1f;
    int curbullet = 30;
    int maxbullet = 150;
    public Text maxBullettxt;
    public Text curBullettxt;
    bool isReloading = false;
    float reloadingTime = 2.0f;
    public RectTransform reloadingbar;

    //총알 프리팹
    public GameObject bullet;
    //총알 발사좌표
    public Transform firePos;


    //수류탄 발사 관련 변수
    public Transform GrenadePos;
    public GameObject Grenade;

    int GrenadeCount = 2;

    //에임 관련 변수
    public RectTransform[] aim; //{up,down,left,right}

    float maxaim = 70;
    float minaim = 25;
    float curaim = 25;

    //줌 관련 변수
    public GameObject aimGroup;
    public Image zoom;

    //게임 상태 관련 변수
    public bool isCursor = true;

    public Material RedBody;
    public Material Body;

    public SkinnedMeshRenderer skin;

    PlayerMove playerMove;

    public GameObject maincam;

    private PhotonView pv = null;

    Vector3 DeathArm = new Vector3(0.17f,0.24f,-1.31f);
    Vector3 SaveArmPos;
    PlayerDamage playerdmg;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        //PhotonView Observed Components 속성에 TankMove 스크립트를 연결
        pv.ObservedComponents[2] = this;
    }

    void Start()
    {
        //rbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerdmg = GetComponent<PlayerDamage>();

        SaveArmPos = RightArm.transform.localPosition;

        muzzleFlash.enabled = false;

        //Rigidbody의 무게중심을 낮게 설정
        rbody.centerOfMass = new Vector3(0.0f, -2.5f, 0.0f);
        maxBullettxt = GameObject.Find("maxbullet").GetComponent<Text>();
        curBullettxt = GameObject.Find("curbullet").GetComponent<Text>();
        aimGroup = GameObject.Find("AimGroup");
        aim = aimGroup.GetComponentsInChildren<RectTransform>();
        zoom = GameObject.Find("zoom").GetComponent<Image>();
        reloadingbar = GameObject.Find("reloadingbar").GetComponent<RectTransform>();
        maincam = GameObject.Find("Main Camera");
        playerMove = GetComponent<PlayerMove>();
    }

    void Update()
    {
        if (pv.IsMine)
        {
            if (GameMgr.m_GameState == GameState.GS_GameEnd)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (GameMgr.m_GameState == GameState.GS_Playing)
            {
                if (playerdmg.isdeath && this.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    RightArm.transform.localPosition = SaveArmPos;
                    curbullet = 30;
                    maxbullet = 150;
                    playerstate = PlayerState.idle;
                }

                if (playerstate != PlayerState.death)
                {
                    if (isCursor)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                    if (isGround)
                    {
                        rbody.drag = 50;
                    }
                    else
                        rbody.drag = -5;

                    Shot();
                    switch (playerstate)
                    {
                        case PlayerState.idle:
                            if (muzzleFlash.enabled != false)
                                muzzleFlash.enabled = false;
                            Aimpos(false);
                            AnimType("Idle");
                            break;
                        case PlayerState.move:
                            maxaim = 50;
                            AnimType("Move");
                            playerMove.Move(walkSpeed);
                            break;
                        case PlayerState.run:
                            if (iszoomOnOff)
                                playerstate = PlayerState.move;
                            maxaim = 70;
                            AnimType("Run");
                            playerMove.Run();
                            break;
                        case PlayerState.shot:

                            break;
                        case PlayerState.death:
                            AnimType("Death");
                            RightArm.transform.localPosition = Vector3.Lerp(RightArm.transform.localPosition, DeathArm, Time.deltaTime * 2.0f);
                            break;
                    }
                    FirePosCheck();
                    stateCheck();


                    CameraRotation();
                    CharacterRotation();
                    reloadingFunc();
                }
                CursorCheck();
            }
        }
        else
        {
            playerstate = Netplayerstate;
            switch (playerstate)
            {
                case PlayerState.idle:
                    if (muzzleFlash.enabled != false)
                        muzzleFlash.enabled = false;
                    AnimType("Idle");
                    break;
                case PlayerState.move:
                    AnimType("Move");
                    break;
                case PlayerState.run:
                    if (iszoomOnOff)
                        playerstate = PlayerState.move;
                    AnimType("Run");
                    break;
                case PlayerState.shot:

                    break;
                case PlayerState.death:
                    AnimType("Death");
                    RightArm.transform.localPosition = Vector3.Lerp(RightArm.transform.localPosition, DeathArm, Time.deltaTime * 10.0f);
                    break;
            }
        }
    }

    void CursorCheck()
    {
        if(Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void AnimType(string anim)
    {
        animator.SetBool("Idle", false);
        animator.SetBool("Move", false);
        animator.SetBool("Run", false);
        animator.SetBool("Death", false);
        animator.SetBool("Shot", false);

        animator.SetBool(anim, true);
    }

    public void Shot()
    {
        fireDur = fireDur - Time.deltaTime;
        if (curbullet > 0)
        {
            if (fireDur <= 0.0f)
            {
                fireDur = 0.0f;
                // 마우스 왼쪽 버튼을 클릭했을 때 Fire 함수 호출
                if (Input.GetMouseButton(0))
                {
                    isReloading = false;
                    isShot = true;
                    //AnimType("Shot");
                    playerstate = PlayerState.shot;
                    Fire();
                    fireDur = 0.25f;
                    curbullet--;
                    curBullettxt.text = curbullet.ToString();
                }
                else
                {
                    isShot = false;
                }
            }
        }
    }

    void FirePosCheck()
    {
        Ray ray = theCamera.ScreenPointToRay(Input.mousePosition);
        //생성된 Ray를 Scene 뷰에 녹색 광선으로 표현
        Debug.DrawRay(ray.origin, ray.direction * 300.0f, Color.green);

        float x = Random.Range(-curaim / 40, curaim / 40);
        float yrange = curaim - x;
        float y = Random.Range(-yrange / 40, yrange / 40);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                               1 << LayerMask.NameToLayer("TERRAIN")))
        {
            Vector3 cur = hit.point;
            cur.x += x;
            cur.y += y;
            firePos.LookAt(cur);
            if (iszoomOnOff)
            {
                firePos.LookAt(hit.point);
            }
            GrenadePos.LookAt(hit.point);
            //Debug.Log(x);
        }
        else
        {
            Vector3 a_OrgVec = ray.origin + ray.direction * 2000.0f;
            ray = new Ray(a_OrgVec, -ray.direction);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                                         1 << LayerMask.NameToLayer("TURRETPICKOBJ")))
            {
                Vector3 cur = hit.point;
                cur.x += x*5;
                cur.y += y*5;
                firePos.LookAt(cur);
                if (iszoomOnOff)
                {
                    firePos.LookAt(hit.point);
                }
                GrenadePos.LookAt(hit.point);
            }
        }
    }

    void Fire()
    {
        //동적으로 총알을 생성하는 함수
        CreateBullet();
        pv.RPC("CreateBullet", RpcTarget.Others, null);

        //사운드 발생 함수
        //source.PlayOneShot(fireSfx, 0.2f);
        //잠시 기다리는 루틴을 위해 코루틴 함수로 호출
        StartCoroutine(this.ShowMuzzleFlash());
    }

    public void RedSkin()
    {
        Material[] svMtrl = skin.materials;
        svMtrl[2] = RedBody;
        skin.materials = svMtrl;
    }

    [PunRPC]
    void CreateBullet()
    {
        //if (PhotonInit.isFocus == false) //윈도우 창이 비활성화 되어 있다면...
        //    return;

        //메인 카메라에서 마우스 커서의 위치로 캐스팅되는 Ray를 생성


        //Bullet 프리팹을 동적으로 생성
        GameObject a_Bullet = Instantiate(bullet, firePos.position, firePos.rotation);
        a_Bullet.GetComponent<BulletCtrl>().AttackerId = pv.Owner.ActorNumber;  //ownerId
        //Owner : 
        //(나를 포함한 원격지의 탱크들) 입장에서 IsMine의 정보를 접근할 수 있는 참조 변수
        //원격지 탱크들 입장에서 지금 이 탱크의 IsMine 정보에 접든 할수 있는 방법

        if (pv.Owner.CustomProperties.ContainsKey("MyTeam") == true)
        {
            a_Bullet.GetComponent<BulletCtrl>().AttackerTeam =
                    (string)pv.Owner.CustomProperties["MyTeam"];
        }
    }

    IEnumerator ShowMuzzleFlash()
    {
        //MuzzleFlash 스케일을 불규칙하게 변경
        float scale = Random.Range(1f, 1.5f);
        muzzleFlash.transform.localScale = Vector3.one * scale;

        //MuzzleFlash를 Z축을 기준으로 불규칙하게 회전시킴
        Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0, 360));
        muzzleFlash.transform.localRotation = rot;

        //활성화에서 보이게 함
        muzzleFlash.enabled = true;

        //불규칙적인 시간 동안 Delay한 다음 MeshRenderer를 비활성화
        yield return new WaitForSeconds(Random.Range(0.01f, 0.03f));  //Random.Range(0.05f, 0.3f));

        //비활성화해서 보이지 않게 함
        muzzleFlash.enabled = false;
    }

    
    public void Aimpos(bool ismove)
    {
        if (ismove)
        {
            if (curaim <= maxaim)
            {
                curaim += Time.deltaTime * moveSpeed * 10;
            }
            else
            {
                curaim -= Time.deltaTime * moveSpeed * 10;
            }

            for (int i = 0; i < 4; i++)
            {
                Aimsetpos(aim[i], i);
            }
        }
        else
        {
            if (curaim >= minaim)
            {
                curaim -= Time.deltaTime * moveSpeed * 10;
            }

            for (int i = 0; i < 4; i++)
            {
                Aimsetpos(aim[i], i);
            }
        }
    }

    void Aimsetpos(RectTransform aimobj, int count)
    {
        Vector3 curaimpos = aimobj.anchoredPosition;
        switch (count)
        {
            case 0:
                curaimpos.y = curaim;
                break;
            case 1:
                curaimpos.y = -curaim;
                break;
            case 2:
                curaimpos.x = curaim;
                break;
            case 3:
                curaimpos.x = -curaim;
                break;
        }
        aimobj.anchoredPosition = curaimpos;

        //Debug.Log(curaimpos);
    }

   

    private void CameraRotation()
    {
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        float _cameraRotationX = _xRotation * lookSensitivity;

        currentCameraRotationX -= _cameraRotationX;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);

        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
        RightArm.transform.localEulerAngles = new Vector3((currentCameraRotationX) + 165, 0, 17.19f);
    }

    private void stateCheck()
    {
        if (GameMgr.m_GameState == GameState.GS_Playing)
        {
            if (playerstate != PlayerState.death)
            {
                if (Input.GetMouseButton(0))
                {
                    isShot = true;
                }
                else
                {
                    isShot = false;
                }

                if (GrenadeCount > 0)
                {
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        Instantiate(Grenade, GrenadePos.position, GrenadePos.rotation);
                        GrenadeCount--;
                    }
                }

                if (Input.GetMouseButton(1))
                {
                    aimGroup.SetActive(false);
                    zoom.enabled = true;
                    iszoomOnOff = true;
                }
                else//if(Input.GetMouseButtonUp(1))
                {
                    zoom.enabled = false;
                    aimGroup.SetActive(true);
                    iszoomOnOff = false;
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    isReloading = true;
                }
            }
        }
    }

    void reloadingFunc()
    {
        if(isReloading)
        {
            reloadingTime -= Time.deltaTime;
            reloadingbar.gameObject.GetComponent<Image>().enabled = true;
            reloadingbar.sizeDelta = new Vector2(300- (300-(reloadingTime * 100)), 12);
            if (reloadingTime <= 0)
            {
                maxbullet += curbullet;
                if (maxbullet >= 30)
                {
                    maxbullet -= 30;
                    curbullet = 30;
                }
                else
                {
                    curbullet = maxbullet;
                    maxbullet = 0;
                }
                reloadingTime = 2.0f;
                maxBullettxt.text = maxbullet.ToString();
                curBullettxt.text = curbullet.ToString();
                isReloading = false;
            }
        }
        else
        {
            reloadingTime = 3.0f;
            reloadingbar.gameObject.GetComponent<Image>().enabled = false;
        }
    }

    private void CharacterRotation()  // 좌우 캐릭터 회전
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        rbody.MoveRotation(rbody.rotation * Quaternion.Euler(_characterRotationY)); // 쿼터니언 * 쿼터니언
        // Debug.Log(myRigid.rotation);  // 쿼터니언
        // Debug.Log(myRigid.rotation.eulerAngles); // 벡터
    }

    void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("TERRAIN"))
        {
            isGround = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("TERRAIN"))
        {
            isGround = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        
        if (stream.IsWriting)
        {
            stream.SendNext(playerstate);
        }
        else  
        {
            Netplayerstate = (PlayerState)stream.ReceiveNext();
        }
    }
}
