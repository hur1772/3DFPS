using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;

public class PlayerDamage : MonoBehaviourPunCallbacks, IPunObservable
{
    //��ũ ���� �� ���� ó���� ���� MeshRenderer ������Ʈ �迭
    private MeshRenderer[] renderers;

    //��ũ ���� ȿ�� �������� ������ ����
    private GameObject expEffect = null;

    //��ũ�� �ʱ� ����ġ
    private int initHp = 120;
    //��ũ�� ���� ����ġ
    public int currHp = 0;
    int NetHp = 0; //�ƹ�Ÿ ��ũ���� HP���� ����ȭ �����ֱ� ���� ����
    //����͸��� �ϴٰ� �ƹ�Ÿ������ �״� ������ �˰� �ͱ� ������ ���� ����

    //��ũ ������ Canvas ��ü�� ������ ����
    public Canvas hudCanvas;
    //Filled Ÿ���� Image UI �׸��� ������ ����
    public Image hpBar;

    PhotonView pv = null;

    //��ũ HUD�� ǥ���� ���ھ� Text UI �׸�
    public Text txtKillCount;

    //�÷��̾� Id�� �����ϴ� ����
    [HideInInspector] public int playerId = -1;

    //�� ��ũ �ı� ���ھ �����ϴ� ����
    int IsMineBuf_killCount = 0; //IsMine ��쿡�� ���� ����
    int m_killCount = 0;    //��� PC�� �� ��ũ���� ����(ų ī��Ʈ�� ���ÿ� �ٲٱ� ���� �뵵)
    int m_Cur_LAttID = -1;  //���� ������ �����ߴ���? 

    ExitGames.Client.Photon.Hashtable KillProps
                        = new ExitGames.Client.Photon.Hashtable();

    [HideInInspector] public float m_ReSetTime = 0.0f;   //��Ȱ�ð�������
    //�����Ŀ��� ������ �ֱ� 10�ʵ���

    PlayerCtrl m_playerCtrl;

    void Awake()
    {
        m_playerCtrl = GetComponent<PlayerCtrl>();

        //��ũ ���� ��� Mesh Renderer ������Ʈ�� ������ �� �迭�� �Ҵ�
        renderers = GetComponentsInChildren<MeshRenderer>();

        //���� ����ġ�� �ʱ� ����ġ�� �ʱ갪 ����
        currHp = initHp;
        NetHp = initHp;
        //��ũ ���� �� ������ų ���� ȿ���� �ε�

        //PhotonView ������Ʈ �Ҵ�
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

        //PhotonView�� ownerId�� PlayerId�� ����
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
         //�� �κ��� ��ũ�� ó�� �濡 ������ �� �ѹ��� ȣ���ϰ� �ϱ� ���� �κ�
         //�켱 ��ũ�� ���¸� �ı��� ����ó��.. 
         //������ �ʰ� �ϰ� ��� Ready���°� �Ǿ��� �� �����ϰ� �Ѵ�. 
         //�̻��ϰ� ��� Update�� ���� �Ŀ� �����ؾ� UI�� ������ �ʴ´�.
         //(��ũ ������ ó�� �ѹ��� �߻��ǵ��� �Ѵ�.)

        if (0.0f < m_ReSetTime)
            m_ReSetTime -= Time.deltaTime;

        if (PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.LocalPlayer == null)
            return;
        //����ȭ ������ ���� �϶��� ������Ʈ�� ����� �ش�.

        if (pv.IsMine == false)
        { //���� �÷��̾�(�ƹ�Ÿ ��ũ ����)�� �� ����

            if (0 < currHp)
            {
                currHp = NetHp;
                //���� ����ġ ����� = (���� ����ġ) / (�ʱ� ����ġ)
                //hpBar.fillAmount = (float)currHp / (float)initHp;

                //���� ��ġ�� ���� Filled �̹����� ������ ����
                if (currHp <= 0) //�̶��� ���ó��
                {
                    currHp = 0;

                    if (0 <= m_Cur_LAttID)
                    {
                        // ���� �״� ��ũ ���忡���� 
                        // �ƹ�Ÿ�� �߿��� AttackerId (<-IsMine) �� ã�Ƽ� 
                        // KillCount�� ���� ���� �־�� �Ѵ�.
                        //TakeDamage�� ���� ��ũ�� ã�ƾ� �Ѵ�.
                        //�ڽ��� �ı���Ų �� ��ũ�� ���ھ ������Ű�� �Լ��� ȣ��
                        SaveKillCount(m_Cur_LAttID);
                    }//if(0 <= m_Net_LAttID)

                    StartCoroutine(this.ExplosionTank());
                } //�̶��� ���ó��

            } //if (0 < currHp)
            else //if (currHp <= 0) 
            {
                m_playerCtrl.playerstate = PlayerCtrl.PlayerState.death;
                //currHp = NetHp;
                ////<---OtherPC���� ��Ȱ�� ���� ����ȭ �Ǿ�� �Ѵ�.
                //if ((int)(initHp * 0.95f) < currHp)
                //{ //�̶��� ��Ȱ ������ �Ǿ�� �ϴ� ��Ȳ
                //    //Filled �̹��� �ʱ갪���� ȯ��
                //    hpBar.fillAmount = 1.0f;
                //    //HUD Ȱ��ȭ
                //    hudCanvas.enabled = true;

                //    //������ �� ���� �ʱ갪 ����
                //    currHp = initHp;

                //    //��ũ�� �ٽ� ���̰� ó��
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
            //    //��ź�� ���� �ִ� ������ ID ��������
            //    a_AttTeam = a_refCanon.AttackerTeam;
            //}

            TakeDamage(a_Att_ID, a_AttTeam);  //���⼭�� �Ű������� ���� ������ ���� ID


            currHp -= 20;
            Debug.Log(currHp);
            if (pv.IsMine)
            {
                //���� ����ġ ����� = (���� ����ġ) / (�ʱ� ����ġ)
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
    //    //�浹�� Collider�� �±� ��
    //    if (0 < currHp && coll.tag == "BULLET")
    //    {
    //        Debug.Log("�浹");
    //        int a_Att_ID = -1;
    //        string a_AttTeam = "blue";
    //        BulletCtrl a_refCanon = coll.gameObject.GetComponent<BulletCtrl>();
    //        //if (a_refCanon != null)
    //        //{
    //        //    a_Att_ID = a_refCanon.AttackerId;
    //        //    //��ź�� ���� �ִ� ������ ID ��������
    //        //    a_AttTeam = a_refCanon.AttackerTeam;
    //        //}

    //        TakeDamage(a_Att_ID, a_AttTeam);  //���⼭�� �Ű������� ���� ������ ���� ID


    //        currHp -= 20;

    //        //���� ����ġ ����� = (���� ����ġ) / (�ʱ� ����ġ)
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

        //�ڱⰡ �� �Ѿ��� �ڽ��� ������ �ȵǱ� ������...
        if (AttackerId == playerId)
            return;

        if (pv.IsMine == false)
            return;

        if (0.0f < m_ReSetTime)  //���� ���� �� 10�� ���� ������ �ֱ�
            return;

        string a_DamageTeam = "blue";
        if (pv.Owner.CustomProperties.ContainsKey("MyTeam") == true)
            a_DamageTeam = (string)pv.Owner.CustomProperties["MyTeam"];

        //���� �������� �޴� ��ũ�� AttackerId ������ ���� 
        //�ٸ� ���϶��� �������� ������ ó��
        if (a_AttTeam == a_DamageTeam)
            return;

        //���� �������� ���̴� �κ��� IsMine �� ���� �ϰڴٴ� ��
        m_Cur_LAttID = AttackerId;
        currHp -= 20;
        if (currHp < 0)
            currHp = 0;

        //���� ����ġ ����� = (���� ����ġ) / (�ʱ� ����ġ)
        hpBar.fillAmount = (float)currHp / (float)initHp;

        if (currHp <= 0)  //�״� ó�� 
        {
            StartCoroutine(this.ExplosionTank());
        }
    } //public void TakeDamage(int AttackerId)


    IEnumerator ExplosionTank()
    {
        ////���� ȿ�� ����
        //if (5.0f < Time.time)
        //{
        //    //���� ���� �� 5�ʰ� ���������� ����Ʈ ��������.... 
        //    //������ �������ڸ��� ������ �׾� �ִ� 
        //    //�ֵ� ����Ʈ�� �����ϱ� �̻��ϴ�.
        //    Object effect = GameObject.Instantiate(expEffect,
        //                            transform.position, Quaternion.identity);

        //    Destroy(effect, 3.0f);
        //}

        //��ũ ���� ó��
        SetTankVisible(false);

        yield return null;

        //if (pv != null && pv.IsMine == true)
        //{
        //    //10�� ���� ��ٷȴٰ� Ȱ��ȭ�ϴ� ������ ����
        //    yield return new WaitForSeconds(10.0f);

        //    //Filled �̹��� �ʱ갪���� ȯ��
        //    hpBar.fillAmount = 1.0f;
        //    //Filled �̹��� ������ ������� ����
        //    hpBar.color = Color.green;
        //    //HUD Ȱ��ȭ
        //    hudCanvas.enabled = true;

        //    //������ �� ���� �ʱ갪 ����
        //    currHp = initHp;

        //    //��ũ�� �ٽ� ���̰� ó��
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
            m_ReSetTime = 10.0f; //���� �����Ŀ��� ������ �ֱ�

    } //void SetTankVisible(bool isVisible)

    public void OnPhotonSerializeView(PhotonStream stream,
                                      PhotonMessageInfo info)
    {
        if (stream.IsWriting)  //���� �÷��̾��� ���� �۽�
        {
            stream.SendNext(m_Cur_LAttID);
            stream.SendNext(currHp);
        }
        else //���� �÷��̾�(�ƹ�Ÿ)�� ���� ����
        {
            m_Cur_LAttID = (int)stream.ReceiveNext();
            NetHp = (int)stream.ReceiveNext();
        }
    }

    #region --------------- CustomProperties KillCount �ʱ�ȭ
    //�ڽ��� �ı���Ų �� ��ũ�� �˻��� ���ھ ������Ű�� �Լ�
    //firePlayerId : Kill ���� ���� ��Ű�� ���� ��ũ ID ĳ���� ã�ƿ���
    void SaveKillCount(int firePlayerId)
    {
        if (firePlayerId < 0)
            return;

        //TAKE �±׸� ������ ��� ��ũ�� ������ �迭�� ����
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject tank in tanks)
        {
            var tankDamage = tank.GetComponent<PlayerDamage>();
            //��ũ�� playerId�� ��ź�� playerId�� �������� �Ǵ�
            if (tankDamage != null && tankDamage.playerId == firePlayerId)
            {
                //������ ��ũ�� ��� ���ھ ������Ŵ
                tankDamage.IncKillCount();
                return;
            }
        }
    }

    void IncKillCount() //���� ��ũ �������� ȣ���
    {
        if (pv != null && pv.IsMine == true)
        {
            IsMineBuf_killCount++;

            SendKillCount(IsMineBuf_killCount);
            //��ε� ������(�߰�) <--//�̰� �� ��� ��ε� ������ �ȴ�.
        }//if (pv != null && pv.IsMine == true)
    }//void IncKillCount()

    void InitCustomProperties(PhotonView pv)
    { //�ӵ��� ���� ���۸� �̸� ����� ���´ٴ� �ǹ�
        //pv.IsMine == true ���� �����ϰ� �ִ� ��ũ�̰� ����������...
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

    void ReceiveKillCount() //KillCount �޾Ƽ� ó���ϴ� �κ�
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
    } //void ReceiveKillCount() //KillCount �޾Ƽ� ó���ϴ� �κ�

    #endregion  //--------------- CustomProperties KillCount �ʱ�ȭ

    public void ReadyStateTank()
    {
        if (GameMgr.m_GameState != GameState.GS_Ready)
            return;

        StartCoroutine(this.WaitReadyTank());
    }

    //���� ���� ���...
    IEnumerator WaitReadyTank()
    {
        //��ũ ���� ó��
        SetTankVisible(false);

        while (GameMgr.m_GameState == GameState.GS_Ready)
        {
            yield return null;
        }

        //��ũ Ư���� ��ġ�� �������ǵ���...
        float pos = Random.Range(-100.0f, 100.0f);
        Vector3 a_SitPos = new Vector3(pos, 20.0f, pos);

        string a_TeamKind = ReceiveSelTeam(pv.Owner); //�ڱ� �Ҽ� �� �޾ƿ���
        int a_SitPosInx = ReceiveSitPosInx(pv.Owner); //�ڱ� �ڸ� ��ȣ �޾ƿ���
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
        //m_ReSetTime = 10.0f; //���� �����Ŀ��� ������ �ֱ�
        //--------- ��ũ Ư���� ��ġ�� �������ǵ���...

        if (pv.IsMine)
        {
            //Filled �̹��� �ʱ갪���� ȯ��
            hpBar.fillAmount = 1.0f;
        }

        if (pv != null && pv.IsMine == true)  //������ �� ���� �ʱ갪 ����
            currHp = initHp;

        //��ũ�� �ٽ� ���̰� ó��
        SetTankVisible(true);

    }//IEnumerator WaitReadyTank()

    //-- CustomProperties Receive �Լ� ����
    string ReceiveSelTeam(Player a_Player) //SelTeam �޾Ƽ� ó���ϴ� �κ�
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
