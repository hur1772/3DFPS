using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class PlayerMove : MonoBehaviourPunCallbacks, IPunObservable
{
    PlayerCtrl playerCtrl;
    // Start is called before the first frame update
    void Start()
    {
        playerCtrl = GetComponent<PlayerCtrl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameMgr.m_GameState == GameState.GS_Playing)
        {
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

    public void OnPhotonSerializeView(PhotonStream stream,
                                      PhotonMessageInfo info)
    {
        
    }
}
