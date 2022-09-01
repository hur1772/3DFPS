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

    //��ũ�� �̵� �� ȸ�� �ӵ��� ��Ÿ���� ����
    public float moveSpeed = 20.0f;
    public float rotSpeed = 50.0f;

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

    //�Ѿ� ������
    public GameObject bullet;
    //�Ѿ� �߻���ǥ
    public Transform firePos;


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
            // ���콺 ���� ��ư�� Ŭ������ �� Fire �Լ� ȣ��
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //������ Ray�� Scene �信 ��� �������� ǥ��
        Debug.DrawRay(ray.origin, ray.direction * 100.0f, Color.green);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                               1 << LayerMask.NameToLayer("TERRAIN")))
        {
            //Ray�� ���� ��ġ�� ������ǥ�� ��ȯ
            Vector3 relative = tr.InverseTransformPoint(hit.point);
            //��ź��Ʈ �Լ��� Atan2�� �� �� ���� ������ ���
            float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
            //rotSpeed ������ ������ �ӵ��� ȸ��
            tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
        }
        else
        {
            Vector3 a_OrgVec = ray.origin + ray.direction * 2000.0f;
            ray = new Ray(a_OrgVec, -ray.direction);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity,
                                         1 << LayerMask.NameToLayer("TURRETPICKOBJ")))
            {
                //Ray�� ���� ��ġ�� ������ǥ�� ��ȯ
                Vector3 relative = tr.InverseTransformPoint(hit.point);
                //��ź��Ʈ �Լ��� Atan2�� �� �� ���� ������ ���
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                //rotSpeed ������ ������ �ӵ��� ȸ��
                tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
            }
        } //else

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
