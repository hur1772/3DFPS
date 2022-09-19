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

        //PhotonView Observed Components �Ӽ��� TankMove ��ũ��Ʈ�� ����
        pv.ObservedComponents[0] = this;

        if (pv.IsMine)
        {
            //�������� ������� �������� ���� �÷��̾ ���� ������ �ƴϸ�
            //��Ʈ��ũ�� ������ ���� �÷��̾ ���� ������� ������ ���δ�
            //�ش� �����տ� �߰��� PhotonView ������Ʈ�� IsMine �Ӽ����� �Ǵ��Ѵ�.

            //���� ī�޶� �߰��� SmoothFollow ��ũ��Ʈ�� ���� ����� ����
        }
        else
        {
            //���� ��Ʈ��ũ �÷��̾��� ��ũ�� �������� �̿����� ����
            playerCtrl.rbody.isKinematic = true;
            //������ ��ũ�� �߷��� �������� �ʰ�
            //currPos ���� ��ġ������ �����ϰڴٴ� �ǹ�
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
            //    //��ġ ���� �ʿ�
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
        {   //�������� ��ũ�� ��� �߰����(��ġ, ȸ����) ���� ����
            if (10.0f < (playerCtrl.playerTr.position - currPos).magnitude)
            {
                playerCtrl.playerTr.position = currPos;
            }
            else
            {
                //���� �÷��̾��� ��ũ�� ���Ź��� ��ġ���� �ε巴�� �̵���Ŵ
                playerCtrl.playerTr.position = Vector3.Lerp(playerCtrl.playerTr.position, currPos, Time.deltaTime * 10.0f);
            }

            //���� �÷��̾��� ��ũ�� ���Ź��� ���� ��ŭ �ε巴�� ȸ����Ŵ
            playerCtrl.playerTr.rotation = Quaternion.Slerp(playerCtrl.playerTr.rotation, currRot, Time.deltaTime * 10.0f);
        }

    }

    public void Move(float speed)
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

        playerCtrl.rbody.MovePosition(transform.position + _velocity * Time.deltaTime);
        playerCtrl.Aimpos(true);
    }

    public void Run()
    {
        Move(playerCtrl.walkSpeed * 2);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //���� �÷��̾��� ��ġ ���� �۽�
        if (stream.IsWriting)
        {
            stream.SendNext(playerCtrl.playerTr.position);
            stream.SendNext(playerCtrl.playerTr.rotation);
        }
        else  //���� �÷��̾��� ��ġ ���� ����
        {
            currPos = (Vector3)stream.ReceiveNext();
            currRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
