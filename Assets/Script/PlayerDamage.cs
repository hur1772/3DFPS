using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class PlayerDamage : MonoBehaviourPunCallbacks, IPunObservable
{
    //탱크 폭파 후 투명 처리를 위한 MeshRenderer 컴포넌트 배열
    private MeshRenderer[] renderers;

    //탱크 폭발 효과 프리팹을 연결할 변수
    private GameObject expEffect = null;

    //탱크의 초기 생명치
    private int initHp = 120;
    //탱크의 현재 생명치
    public int currHp = 0;
    int NetHp = 0; //아바타 탱크들의 HP값를 동기화 시켜주기 위한 변수
    //모니터링을 하다가 아바타에서도 죽는 시점을 알고 싶기 때문에 만든 변수

    //탱크 하위의 Canvas 객체를 연결할 변수
    public Canvas hudCanvas;
    //Filled 타입의 Image UI 항목을 연결할 변수
    public Image hpBar;

    PhotonView pv = null;

    //탱크 HUD에 표현할 스코어 Text UI 항목
    public Text txtKillCount;

    //플레이어 Id를 저장하는 변수
    [HideInInspector] public int playerId = -1;

    //적 탱크 파괴 스코어를 저장하는 변수
    int IsMineBuf_killCount = 0; //IsMine 경우에만 사용될 변수
    int m_killCount = 0;    //모든 PC의 내 탱크들의 변수(킬 카운트를 동시에 바꾸기 위한 용도)
    int m_Cur_LAttID = -1;  //누가 마지막 공격했는지? 

    ExitGames.Client.Photon.Hashtable KillProps
                        = new ExitGames.Client.Photon.Hashtable();

    [HideInInspector] public float m_ReSetTime = 0.0f;   //부활시간딜레이
    //시작후에도 딜레이 주기 10초동안

    PlayerCtrl m_playerCtrl;

    void Awake()
    {
        m_playerCtrl = GetComponent<PlayerCtrl>();

        //탱크 모델의 모든 Mesh Renderer 컴포넌트를 추출한 후 배열에 할당
        renderers = GetComponentsInChildren<MeshRenderer>();

        //현재 생명치를 초기 생명치로 초깃값 설정
        currHp = initHp;
        NetHp = initHp;
        //탱크 폭발 시 생성시킬 폭발 효과를 로드

        //PhotonView 컴포넌트 할당
        pv = GetComponent<PhotonView>();
        pv.ObservedComponents[1] = this;
        //if (2 <= pv.ObservedComponents.Count)
        //    pv.ObservedComponents[1] = this;
        if (pv.IsMine)
        {
            hpBar = GameObject.Find("hpBarImg").GetComponent<Image>();
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        InitCustomProperties(pv);

        //PhotonView의 ownerId를 PlayerId에 저장
        //pv.ownerId -> pv.Owner.ActorNumber 
        playerId = pv.Owner.ActorNumber;
    }


    int a_UpdateCk = 2;
    // Update is called once per frame
    void Update()
    {
        if (0 < a_UpdateCk)
        {
            a_UpdateCk--;
            if (a_UpdateCk <= 0)
            {
                ReadyStateTank();
            }
        }//if (0 < a_UpdateCk)
         //이 부분은 탱크가 처음 방에 입장할 때 한번만 호출하게 하기 위한 부분
         //우선 탱크의 상태를 파괴된 이후처럼.. 
         //보이지 않게 하고 모두 Ready상태가 되었을 때 시작하게 한다. 
         //이상하게 모든 Update를 돌고난 후에 적용해야 UI가 깨지지 않는다.
         //(탱크 생성시 처음 한번만 발생되도록 한다.)

        if (0.0f < m_ReSetTime)
            m_ReSetTime -= Time.deltaTime;

        if (PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.LocalPlayer == null)
            return;
        //동기화 가능한 상태 일때만 업데이트를 계산해 준다.

        if (pv.IsMine == false)
        { //원격 플레이어(아바타 탱크 입장)일 때 수행

            if (0 < currHp)
            {
                currHp = NetHp;
                //현재 생명치 백분율 = (현재 생명치) / (초기 생명치)
                //hpBar.fillAmount = (float)currHp / (float)initHp;

                //생명 수치에 따라 Filled 이미지의 색상을 변경
                if (currHp <= 0) //이때만 사망처리
                {
                    currHp = 0;

                    if (0 <= m_Cur_LAttID)
                    {
                        // 지금 죽는 탱크 입장에서는 
                        // 아바타들 중에서 AttackerId (<-IsMine) 를 찾아서 
                        // KillCount를 증가 시켜 주어야 한다.
                        //TakeDamage를 받을 탱크를 찾아야 한다.
                        //자신을 파괴시킨 적 탱크의 스코어를 증가시키는 함수를 호출
                        SaveKillCount(m_Cur_LAttID);
                    }//if(0 <= m_Net_LAttID)

                    StartCoroutine(this.ExplosionTank());
                } //이때만 사망처리

            } //if (0 < currHp)
            else //if (currHp <= 0) 
            {
                m_playerCtrl.playerstate = PlayerCtrl.PlayerState.death;
                //currHp = NetHp;
                ////<---OtherPC들이 부활할 때도 동기화 되어야 한다.
                //if ((int)(initHp * 0.95f) < currHp)
                //{ //이때가 부활 연출이 되어야 하는 상황
                //    //Filled 이미지 초깃값으로 환원
                //    hpBar.fillAmount = 1.0f;
                //    //HUD 활성화
                //    hudCanvas.enabled = true;

                //    //리스폰 시 생명 초깃값 설정
                //    currHp = initHp;

                //    //탱크를 다시 보이게 처리
                //    SetTankVisible(true);
                //} //if ((int)(initHp * 0.95f) < currHp)
            } //if (currHp <= 0) 
        } //if (pv.IsMine == false)

        ReceiveKillCount();
    } //void Update()

    private void OnCollisionEnter(Collision coll)
    {
        if (0 < currHp && coll.gameObject.tag == "BULLET")
        {
            int a_Att_ID = -1;
            string a_AttTeam = "blue";
            BulletCtrl a_refCanon = coll.gameObject.GetComponent<BulletCtrl>();
            //if (a_refCanon != null)
            //{
            //    a_Att_ID = a_refCanon.AttackerId;
            //    //포탄이 갖고 있는 공격자 ID 가져오기
            //    a_AttTeam = a_refCanon.AttackerTeam;
            //}

            TakeDamage(a_Att_ID, a_AttTeam);  //여기서의 매개변수는 나를 공격한 유저 ID


            currHp -= 20;
            Debug.Log(currHp);
            if (pv.IsMine)
            {
                //현재 생명치 백분율 = (현재 생명치) / (초기 생명치)
                hpBar.fillAmount = (float)currHp / (float)initHp;
            }
            if (currHp <= 0)
            {
                m_playerCtrl.playerstate = PlayerCtrl.PlayerState.death;
                StartCoroutine(this.ExplosionTank());
            }
        }
    }

    //void OnTriggerEnter(Collider coll)
    //{
    //    //충돌한 Collider의 태그 비교
    //    if (0 < currHp && coll.tag == "BULLET")
    //    {
    //        Debug.Log("충돌");
    //        int a_Att_ID = -1;
    //        string a_AttTeam = "blue";
    //        BulletCtrl a_refCanon = coll.gameObject.GetComponent<BulletCtrl>();
    //        //if (a_refCanon != null)
    //        //{
    //        //    a_Att_ID = a_refCanon.AttackerId;
    //        //    //포탄이 갖고 있는 공격자 ID 가져오기
    //        //    a_AttTeam = a_refCanon.AttackerTeam;
    //        //}

    //        TakeDamage(a_Att_ID, a_AttTeam);  //여기서의 매개변수는 나를 공격한 유저 ID


    //        currHp -= 20;

    //        //현재 생명치 백분율 = (현재 생명치) / (초기 생명치)
    //        hpBar.fillAmount = (float)currHp / (float)initHp;

    //        if (currHp <= 0)
    //        {
    //            StartCoroutine(this.ExplosionTank());
    //        }
    //    }
    //}//void OnTriggerEnter(Collider coll)

    public void TakeDamage(int AttackerId, string a_AttTeam = "blue")
    {
        if (currHp <= 0.0f)
            return;

        //자기가 쏜 총알은 자신이 맞으면 안되기 때문에...
        if (AttackerId == playerId)
            return;

        if (pv.IsMine == false)
            return;

        if (0.0f < m_ReSetTime)  //게임 시작 후 10초 동안 딜레이 주기
            return;

        string a_DamageTeam = "blue";
        if (pv.Owner.CustomProperties.ContainsKey("MyTeam") == true)
            a_DamageTeam = (string)pv.Owner.CustomProperties["MyTeam"];

        //지금 데미지를 받는 탱크가 AttackerId 공격자 팀과 
        //다른 팀일때만 데미지가 들어가도록 처리
        if (a_AttTeam == a_DamageTeam)
            return;

        //실제 데미지가 깍이는 부분은 IsMine 일 때만 하겠다는 뜻
        m_Cur_LAttID = AttackerId;
        currHp -= 20;
        if (currHp < 0)
            currHp = 0;

        //현재 생명치 백분율 = (현재 생명치) / (초기 생명치)
        hpBar.fillAmount = (float)currHp / (float)initHp;

        if (currHp <= 0)  //죽는 처리 
        {
            StartCoroutine(this.ExplosionTank());
        }
    } //public void TakeDamage(int AttackerId)


    IEnumerator ExplosionTank()
    {
        ////폭발 효과 생성
        //if (5.0f < Time.time)
        //{
        //    //게임 시작 후 5초가 지난다음에 이펙트 터지도록.... 
        //    //게임이 시작하자마자 기존에 죽어 있는 
        //    //애들 이펙트가 터지니까 이상하다.
        //    Object effect = GameObject.Instantiate(expEffect,
        //                            transform.position, Quaternion.identity);

        //    Destroy(effect, 3.0f);
        //}

        //탱크 투명 처리
        SetTankVisible(false);

        yield return null;

        //if (pv != null && pv.IsMine == true)
        //{
        //    //10초 동안 기다렸다가 활성화하는 로직을 수행
        //    yield return new WaitForSeconds(10.0f);

        //    //Filled 이미지 초깃값으로 환원
        //    hpBar.fillAmount = 1.0f;
        //    //Filled 이미지 색상을 녹색으로 설정
        //    hpBar.color = Color.green;
        //    //HUD 활성화
        //    hudCanvas.enabled = true;

        //    //리스폰 시 생명 초깃값 설정
        //    currHp = initHp;

        //    //탱크를 다시 보이게 처리
        //    SetTankVisible(true);
        //} //if (pv != null && pv.IsMine == true)
        //else
        //{
        //    yield return null;
        //}

    }//IEnumerator ExplosionTank()

    void SetTankVisible(bool isVisible)
    {
        foreach (MeshRenderer _renderer in renderers)
        {
            _renderer.enabled = isVisible;
        }

        CapsuleCollider[] a_CapsuleColls = this.GetComponentsInChildren<CapsuleCollider>(true);
        foreach (CapsuleCollider _a_CapsuleColls in a_CapsuleColls)
        {
            if (isVisible == false)
                _a_CapsuleColls.gameObject.layer = LayerMask.NameToLayer("DiePlayer");
            else
                _a_CapsuleColls.gameObject.layer = LayerMask.NameToLayer("Default");
        }

        if (isVisible == true)
            m_ReSetTime = 10.0f; //게임 시작후에도 딜레이 주기

    } //void SetTankVisible(bool isVisible)

    public void OnPhotonSerializeView(PhotonStream stream,
                                      PhotonMessageInfo info)
    {
        if (stream.IsWriting)  //로컬 플레이어의 정보 송신
        {
            stream.SendNext(m_Cur_LAttID);
            stream.SendNext(currHp);
        }
        else //원격 플레이어(아바타)의 정보 수신
        {
            m_Cur_LAttID = (int)stream.ReceiveNext();
            NetHp = (int)stream.ReceiveNext();
        }
    }

    #region --------------- CustomProperties KillCount 초기화
    //자신을 파괴시킨 적 탱크를 검색해 스코어를 증가시키는 함수
    //firePlayerId : Kill 수를 증가 시키기 위한 탱크 ID 캐릭터 찾아오기
    void SaveKillCount(int firePlayerId)
    {
        if (firePlayerId < 0)
            return;

        //TAKE 태그를 지정된 모든 탱크를 가져와 배열에 저장
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject tank in tanks)
        {
            var tankDamage = tank.GetComponent<PlayerDamage>();
            //탱크의 playerId가 포탄의 playerId와 동일한지 판단
            if (tankDamage != null && tankDamage.playerId == firePlayerId)
            {
                //동일한 탱크일 경우 스코어를 증가시킴
                tankDamage.IncKillCount();
                return;
            }
        }
    }

    void IncKillCount() //때린 탱크 입장으로 호출됨
    {
        if (pv != null && pv.IsMine == true)
        {
            IsMineBuf_killCount++;

            SendKillCount(IsMineBuf_killCount);
            //브로드 케이팅(중계) <--//이걸 해 줘야 브로드 케이팅 된다.
        }//if (pv != null && pv.IsMine == true)
    }//void IncKillCount()

    void InitCustomProperties(PhotonView pv)
    { //속도를 위해 버퍼를 미리 만들어 놓는다는 의미
        //pv.IsMine == true 내가 조정하고 있는 탱크이고 스폰시점에...
        if (pv != null && pv.IsMine == true)
        {
            KillProps.Clear();
            KillProps.Add("KillCount", 0);
            pv.Owner.SetCustomProperties(KillProps);
        }
    }//void InitCustomProperties(PhotonView pv)

    void SendKillCount(int a_KillCount = 0)
    {
        if (pv == null)
            return;

        if (pv.IsMine == false)
            return;

        if (KillProps == null)
        {
            KillProps = new ExitGames.Client.Photon.Hashtable();
            KillProps.Clear();
        }

        if (KillProps.ContainsKey("KillCount") == true)
            KillProps["KillCount"] = a_KillCount;
        else
            KillProps.Add("KillCount", a_KillCount);

        pv.Owner.SetCustomProperties(KillProps);
    }

    void ReceiveKillCount() //KillCount 받아서 처리하는 부분
    {
        if (pv == null)
            return;

        if (pv.Owner == null)
            return;

        if (pv.Owner.CustomProperties.ContainsKey("KillCount") == true)
        {
            int a_KillCnt = (int)pv.Owner.CustomProperties["KillCount"];
            if (m_killCount != a_KillCnt)
            {
                m_killCount = a_KillCnt;
                if (txtKillCount != null)
                    txtKillCount.text = m_killCount.ToString();
            }
        }
    } //void ReceiveKillCount() //KillCount 받아서 처리하는 부분

    #endregion  //--------------- CustomProperties KillCount 초기화

    public void ReadyStateTank()
    {
        if (GameMgr.m_GameState != GameState.GS_Ready)
            return;

        StartCoroutine(this.WaitReadyTank());
    }

    //게임 시작 대기...
    IEnumerator WaitReadyTank()
    {
        //탱크 투명 처리
        SetTankVisible(false);

        while (GameMgr.m_GameState == GameState.GS_Ready)
        {
            yield return null;
        }

        //탱크 특정한 위치에 리스폰되도록...
        float pos = Random.Range(-100.0f, 100.0f);
        Vector3 a_SitPos = new Vector3(pos, 20.0f, pos);

        string a_TeamKind = ReceiveSelTeam(pv.Owner); //자기 소속 팀 받아오기
        int a_SitPosInx = ReceiveSitPosInx(pv.Owner); //자기 자리 번호 받아오기
        if (0 <= a_SitPosInx && a_SitPosInx < 4)
        {
            if (a_TeamKind == "blue")
            {
                a_SitPos = GameMgr.m_Team1Pos[a_SitPosInx];
                this.gameObject.transform.eulerAngles =
                                        new Vector3(0.0f, 201.0f, 0.0f);
            }
            else if (a_TeamKind == "black")
            {
                a_SitPos = GameMgr.m_Team2Pos[a_SitPosInx];
                this.gameObject.transform.eulerAngles =
                                        new Vector3(0.0f, 19.5f, 0.0f);
            }
        }//if (0 <= a_SitPosInx && a_SitPosInx < 4)

        this.gameObject.transform.position = a_SitPos;
        //m_ReSetTime = 10.0f; //게임 시작후에도 딜레이 주기
        //--------- 탱크 특정한 위치에 리스폰되도록...

        if (pv.IsMine)
        {
            //Filled 이미지 초깃값으로 환원
            hpBar.fillAmount = 1.0f;
        }

        if (pv != null && pv.IsMine == true)  //리스폰 시 생명 초깃값 설정
            currHp = initHp;

        //탱크를 다시 보이게 처리
        SetTankVisible(true);

    }//IEnumerator WaitReadyTank()

    //-- CustomProperties Receive 함수 모음
    string ReceiveSelTeam(Player a_Player) //SelTeam 받아서 처리하는 부분
    {
        string a_TeamKind = "blue";

        if (a_Player == null)
            return a_TeamKind;

        if (a_Player.CustomProperties.ContainsKey("MyTeam") == true)
            a_TeamKind = (string)a_Player.CustomProperties["MyTeam"];

        return a_TeamKind;
    }

    int ReceiveSitPosInx(Player a_Player)
    {
        int a_SitIdx = -1;

        if (a_Player == null)
            return a_SitIdx;

        if (a_Player.CustomProperties.ContainsKey("SitPosInx") == true)
            a_SitIdx = (int)a_Player.CustomProperties["SitPosInx"];

        return a_SitIdx;
    }
}
