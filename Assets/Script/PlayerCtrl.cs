using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
    public enum PlayerState
    {
        idle,
        move,
        run,
        zoom,
        shot,        
        death
    }

    public PlayerState playerstate = PlayerState.idle;

    //탱크의 이동 및 회전 속도를 나타내는 변수
    public float moveSpeed = 20.0f;
    public float rotSpeed = 50.0f;

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

    //총알 프리팹
    public GameObject bullet;
    //총알 발사좌표
    public Transform firePos;


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
            rbody.drag = 0;

        Shot();

        switch (playerstate)
        {
            case PlayerState.idle:
                if (isShot == false) ;
                AnimType("Idle");
                break;
            case PlayerState.move:
                if (isShot == false) ;
                AnimType("Move");
                Move(walkSpeed);
                break;
            case PlayerState.run:
                if (isShot == false) ;
                AnimType("Run");
                Run();
                break;
            case PlayerState.shot:

                break;
            case PlayerState.zoom:

                break;
            case PlayerState.death:

                break;
        }

        stateCheck();
        
        CameraRotation();      
        CharacterRotation();    
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
        if (fireDur <= 0.0f)
        {
            fireDur = 0.0f;
            // 마우스 왼쪽 버튼을 클릭했을 때 Fire 함수 호출
            if (Input.GetMouseButton(0))
            {
                isShot = true;
                //AnimType("Shot");
                playerstate = PlayerState.shot;
                Fire();
                fireDur = 0.1f;
            }
            else
            {
                isShot = false;
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //생성된 Ray를 Scene 뷰에 녹색 광선으로 표현
        Debug.DrawRay(ray.origin, ray.direction * 100.0f, Color.green);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                               1 << LayerMask.NameToLayer("TERRAIN")))
        {
            //Ray에 맞은 위치를 로컬좌표로 변환
            Vector3 relative = tr.InverseTransformPoint(hit.point);
            //역탄젠트 함수인 Atan2로 두 점 간의 각도를 계산
            float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
            //rotSpeed 변수에 지정된 속도로 회전
            tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
        }
        else
        {
            Vector3 a_OrgVec = ray.origin + ray.direction * 2000.0f;
            ray = new Ray(a_OrgVec, -ray.direction);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                                         1 << LayerMask.NameToLayer("TURRETPICKOBJ")))
            {
                //Ray에 맞은 위치를 로컬좌표로 변환
                Vector3 relative = tr.InverseTransformPoint(hit.point);
                //역탄젠트 함수인 Atan2로 두 점 간의 각도를 계산
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                //rotSpeed 변수에 지정된 속도로 회전
                tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
            }
        } //else

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
    }

    private void Run()
    {
        Move(walkSpeed + 10);
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
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGround = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGround = false;
        }
    }
}
