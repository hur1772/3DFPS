using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCtrl : MonoBehaviour
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

    //이동 및 회전 속도를 나타내는 변수
    public float moveSpeed = 10.0f;
    public float rotSpeed = 50.0f;

    bool iszoomOnOff = false;

    //참조할 컴포넌트를 할당할 변수
    private Rigidbody rbody;

    [SerializeField]
    private float walkSpeed;

    [SerializeField]
    private float lookSensitivity;

    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX;

    [SerializeField]
    private Camera theCamera;
    public GameObject RightArm;

    [HideInInspector]public Animator animator;

    bool isGround = true;

    bool isRun = false;

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

    //에임 관련 변수
    public RectTransform[] aim; //{up,down,left,right}

    float maxaim = 70;
    float minaim = 25;
    float curaim = 25;

    //줌 관련 변수
    public GameObject aimGroup;
    public Image zoom;

    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        muzzleFlash.enabled = false;

        //Rigidbody의 무게중심을 낮게 설정
        rbody.centerOfMass = new Vector3(0.0f, -2.5f, 0.0f);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
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
                Aimpos(false);
                AnimType("Idle");
                break;
            case PlayerState.move:
                maxaim = 50;
                AnimType("Move");
                Move(walkSpeed);
                break;
            case PlayerState.run:
                if (iszoomOnOff) 
                    playerstate = PlayerState.move;
                maxaim = 70;
                AnimType("Run");
                Run();
                break;
            case PlayerState.shot:

                break;
            case PlayerState.death:

                break;
        }
        FirePosCheck();
        stateCheck();
        
        CameraRotation();      
        CharacterRotation();
        reloadingFunc();
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
                    fireDur = 0.1f;
                    curbullet--;
                    curBullettxt.text = curbullet.ToString();
                    Debug.Log(curbullet);
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

        //사운드 발생 함수
        //source.PlayOneShot(fireSfx, 0.2f);
        //잠시 기다리는 루틴을 위해 코루틴 함수로 호출
        StartCoroutine(this.ShowMuzzleFlash());
    }

    void CreateBullet()
    {
        //if (PhotonInit.isFocus == false) //윈도우 창이 비활성화 되어 있다면...
        //    return;

        //메인 카메라에서 마우스 커서의 위치로 캐스팅되는 Ray를 생성
       

        //Bullet 프리팹을 동적으로 생성
        Instantiate(bullet, firePos.position, firePos.rotation);
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

    private void Move(float speed)
    {
    //    h = Input.GetAxis("Horizontal");
    //    v = Input.GetAxis("Vertical");

    //    //회전과 이동처리
    //    tr.Rotate(Vector3.up * rotSpeed * h * Time.deltaTime);
    //    tr.Translate(Vector3.forward * v * moveSpeed * Time.deltaTime);

        float _moveDirX = Input.GetAxisRaw("Horizontal");
        float _moveDirZ = Input.GetAxisRaw("Vertical");
        Vector3 _moveHorizontal = transform.right * _moveDirX;
        Vector3 _moveVertical = transform.forward * _moveDirZ;

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * speed;

        rbody.MovePosition(transform.position + _velocity* Time.deltaTime);
        Aimpos(true);
    }

    void Aimpos(bool ismove)
    {
        if(ismove)
        {
            if (curaim <= maxaim)
            {
                curaim += Time.deltaTime*moveSpeed*10;
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
            if(curaim >=minaim)
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

    private void Run()
    {
        Move(walkSpeed * 2);
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
        if(Input.GetAxisRaw("Horizontal") != 0|| Input.GetAxisRaw("Vertical") != 0)
        {
            if(!isRun)
                playerstate = PlayerState.move;
        }
        else
        {
            playerstate = PlayerState.idle;
        }

        if(Input.GetKey(KeyCode.LeftShift))
        {
            if (iszoomOnOff)
            {
                playerstate = PlayerState.move;
                return;
            }

            isRun = true;
            playerstate = PlayerState.run;
        }
        if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            isRun = false;
        }

        if (Input.GetMouseButton(0))
        {
            isShot = true;
        }
        else
        {
            isShot = false;
        }

        if(Input.GetKeyDown(KeyCode.G))
        {
            Instantiate(Grenade, GrenadePos.position, GrenadePos.rotation);
        }

        if(Input.GetMouseButton(1))
        {
            aimGroup.SetActive(false);
            zoom.gameObject.SetActive(true);
            iszoomOnOff = true;
        }
        else//if(Input.GetMouseButtonUp(1))
        {
            zoom.gameObject.SetActive(false);
            aimGroup.SetActive(true);
            iszoomOnOff = false;
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            isReloading = true;
        }
    }

    void reloadingFunc()
    {
        if(isReloading)
        {
            reloadingTime -= Time.deltaTime;
            reloadingbar.gameObject.SetActive(true);
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
            reloadingbar.gameObject.SetActive(false);
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
}
