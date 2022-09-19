using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class PlayerMove : MonoBehaviourPunCallbacks, IPunObservable
{
    public PlayerCtrl playerCtrl;

    private Vector3 currPos = Vector3.zero;
    private Quaternion currRot = Quaternion.identity;

    private PhotonView pv = null;

    void Awake()
    {
        //playerCtrl = GetComponent<PlayerCtrl>();
        pv = GetComponent<PhotonView>();

        //PhotonView Observed Components 속성에 TankMove 스크립트를 연결
        pv.ObservedComponents[0] = this;

        if (pv.IsMine)
        {
            //동적으로 만들어진 프리팹이 로컬 플레이어가 만든 것인지 아니면
            //네트워크에 접속한 원격 플레이어에 의해 만들어진 것인지 여부는
            //해당 프리팹에 추가된 PhotonView 컴포넌트의 IsMine 속성으로 판단한다.

            //메인 카메라에 추가된 SmoothFollow 스크립트에 추적 대상을 연결
        }
        else
        {
            //원격 네트워크 플레이어의 탱크는 물리력을 이용하지 않음
            playerCtrl.rbody.isKinematic = true;
            //원격지 탱크는 중력을 적용하지 않고
            //currPos 값을 위치값으로 적용하겠다는 의미
        }

        currPos = playerCtrl.playerTr.position;
        currRot = playerCtrl.playerTr.rotation;
    }

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (pv.IsMine == true)
        {
            if (this.gameObject.layer == LayerMask.NameToLayer("DieTank"))
                return;


            if (GameMgr.m_GameState == GameState.GS_Playing)
            {
                if (playerCtrl.maincam.activeSelf == true)
                {
                    playerCtrl.maincam.SetActive(false);
                    playerCtrl.theCamera.enabled = true;
                }
            }

            //if (-10.0f < playerCtrl.playerTr.position.y)
            //{
            //    //위치 고정 필요
            //    float pos = Random.Range(-100.0f, 100.0f);
            //    playerCtrl.playerTr.position = new Vector3(pos, 10.0f, pos);
            //    //this.transform.position = new Vector3(pos, 20.0f, pos);

            //    return;
            //}

            if (GameMgr.m_GameState != GameState.GS_Playing)
                return;

            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            {
                if (!playerCtrl.isRun)
                    playerCtrl.playerstate = PlayerCtrl.PlayerState.move;
            }
            else
            {
                playerCtrl.playerstate = PlayerCtrl.PlayerState.idle;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (playerCtrl.iszoomOnOff)
                {
                    playerCtrl.playerstate = PlayerCtrl.PlayerState.move;
                    return;
                }

                playerCtrl.isRun = true;
                playerCtrl.playerstate = PlayerCtrl.PlayerState.run;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                playerCtrl.isRun = false;
            }

        }//if (pv.IsMine)
        else
        {   //원격지의 탱크의 경우 중계받음(위치, 회전값) 값을 적용
            if (10.0f < (playerCtrl.playerTr.position - currPos).magnitude)
            {
                playerCtrl.playerTr.position = currPos;
            }
            else
            {
                //원격 플레이어의 탱크를 수신받은 위치까지 부드럽게 이동시킴
                playerCtrl.playerTr.position = Vector3.Lerp(playerCtrl.playerTr.position, currPos, Time.deltaTime * 10.0f);
            }

            //원격 플레이어의 탱크를 수신받은 각도 만큼 부드럽게 회전시킴
            playerCtrl.playerTr.rotation = Quaternion.Slerp(playerCtrl.playerTr.rotation, currRot, Time.deltaTime * 10.0f);
        }

    }

    public void Move(float speed)
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

        playerCtrl.rbody.MovePosition(transform.position + _velocity * Time.deltaTime);
        playerCtrl.Aimpos(true);
    }

    public void Run()
    {
        Move(playerCtrl.walkSpeed * 2);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //로컬 플레이어의 위치 정보 송신
        if (stream.IsWriting)
        {
            stream.SendNext(playerCtrl.playerTr.position);
            stream.SendNext(playerCtrl.playerTr.rotation);
        }
        else  //원격 플레이어의 위치 정보 수신
        {
            currPos = (Vector3)stream.ReceiveNext();
            currRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
