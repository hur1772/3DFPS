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

    //�̵� �� ȸ�� �ӵ��� ��Ÿ���� ����
    public float moveSpeed = 10.0f;
    public float rotSpeed = 50.0f;

    bool iszoomOnOff = false;

    //������ ������Ʈ�� �Ҵ��� ����
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

    //�Ѿ� �߻� ���� ����
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

    //�Ѿ� ������
    public GameObject bullet;
    //�Ѿ� �߻���ǥ
    public Transform firePos;


    //����ź �߻� ���� ����
    public Transform GrenadePos;
    public GameObject Grenade;

    //���� ���� ����
    public RectTransform[] aim; //{up,down,left,right}

    float maxaim = 70;
    float minaim = 25;
    float curaim = 25;

    //�� ���� ����
    public GameObject aimGroup;
    public Image zoom;

    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        muzzleFlash.enabled = false;

        //Rigidbody�� �����߽��� ���� ����
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
                // ���콺 ���� ��ư�� Ŭ������ �� Fire �Լ� ȣ��
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
        //������ Ray�� Scene �信 ��� �������� ǥ��
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
        //�������� �Ѿ��� �����ϴ� �Լ�
        CreateBullet();

        //���� �߻� �Լ�
        //source.PlayOneShot(fireSfx, 0.2f);
        //��� ��ٸ��� ��ƾ�� ���� �ڷ�ƾ �Լ��� ȣ��
        StartCoroutine(this.ShowMuzzleFlash());
    }

    void CreateBullet()
    {
        //if (PhotonInit.isFocus == false) //������ â�� ��Ȱ��ȭ �Ǿ� �ִٸ�...
        //    return;

        //���� ī�޶󿡼� ���콺 Ŀ���� ��ġ�� ĳ���õǴ� Ray�� ����
       

        //Bullet �������� �������� ����
        Instantiate(bullet, firePos.position, firePos.rotation);
    }

    IEnumerator ShowMuzzleFlash()
    {
        //MuzzleFlash �������� �ұ�Ģ�ϰ� ����
        float scale = Random.Range(1f, 1.5f);
        muzzleFlash.transform.localScale = Vector3.one * scale;

        //MuzzleFlash�� Z���� �������� �ұ�Ģ�ϰ� ȸ����Ŵ
        Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0, 360));
        muzzleFlash.transform.localRotation = rot;

        //Ȱ��ȭ���� ���̰� ��
        muzzleFlash.enabled = true;

        //�ұ�Ģ���� �ð� ���� Delay�� ���� MeshRenderer�� ��Ȱ��ȭ
        yield return new WaitForSeconds(Random.Range(0.01f, 0.03f));  //Random.Range(0.05f, 0.3f));

        //��Ȱ��ȭ�ؼ� ������ �ʰ� ��
        muzzleFlash.enabled = false;
    }

    private void Move(float speed)
    {
    //    h = Input.GetAxis("Horizontal");
    //    v = Input.GetAxis("Vertical");

    //    //ȸ���� �̵�ó��
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

    private void CharacterRotation()  // �¿� ĳ���� ȸ��
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        rbody.MoveRotation(rbody.rotation * Quaternion.Euler(_characterRotationY)); // ���ʹϾ� * ���ʹϾ�
        // Debug.Log(myRigid.rotation);  // ���ʹϾ�
        // Debug.Log(myRigid.rotation.eulerAngles); // ����
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
