using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    GS_Ready = 0,
    GS_Playing,
    GS_GameEnd,
}

public class GameMgr : MonoBehaviourPunCallbacks
{
    //접속된 플레이어 수를 표시할 Text UI 항목 변수
    public Text txtConnect;

    public Button ExitRoomBtn;

    //접속 로그를 표시할 Text UI 항목 변수
    public Text txtLogMsg;

    //RPC 호출을 위한 PhotonView
    private PhotonView pv;

    public InputField textChat;
    bool bEnter = false;

    //--------------- 팀대전 관련 변수들...
    GameState m_OldState = GameState.GS_Ready;
    public static GameState m_GameState = GameState.GS_Ready;

    ExitGames.Client.Photon.Hashtable m_StateProps =
                      new ExitGames.Client.Photon.Hashtable();

    //--------------------Team Select 부분
    [Header("--- Team1 UI ---")]
    public GameObject Team1Panel;
    public Button m_Team1ToTeam2;
    public Button m_Team1Ready;
    public GameObject scrollTeam1;

    [Header("--- Team1 UI ---")]
    public GameObject Team2Panel;
    public Button m_Team2ToTeam1;
    public Button m_Team2Ready;
    public GameObject scrollTeam2;

    [Header("--- Tank Node ---")]
    public GameObject m_TkNodeItem;
    //--------------------Team Select 부분

    ExitGames.Client.Photon.Hashtable m_SelTeamProps =
                        new ExitGames.Client.Photon.Hashtable();

    ExitGames.Client.Photon.Hashtable m_PlayerReady =
                        new ExitGames.Client.Photon.Hashtable();

    ExitGames.Client.Photon.Hashtable SitPosInxProps =
                        new ExitGames.Client.Photon.Hashtable();

    [HideInInspector] public static Vector3[] m_Team1Pos = new Vector3[4];
    [HideInInspector] public static Vector3[] m_Team2Pos = new Vector3[4];
    //--------------- 팀대전 관련 변수들...

    //--------------- Round 관련 변수
    [Header("--- StartTmer UI ---")]
    public Text m_WaitTmText;           //게임 시작후 카운트 3, 2, 1, 0
    [HideInInspector] float m_GoWaitGame = 4.0f; //게임 시작후 카운트 Text UI

    int m_RoundCount = 0;       //라운드 5라운드로 진행
    double m_ChekWinTime = 2.0f; //라운드 시작후 승패 판정은 2초후부터 하기 위해..
    int IsRoomBuf_Team1Win = 0; //정확히 한번만 ++ 시키기위한 Room 기준의 버퍼 변수
    int m_Team1Win = 0;         //블루팀 승리 카운트
    int IsRoomBuf_Team2Win = 0; //정확히 한번만 ++ 시키기위한 Room 기준의 버퍼 변수
    int m_Team2Win = 0;         //블랙팀 승리 카운트
    [Header("--- WinLossCount ---")]
    public Text m_WinLossCount;         //승리 카운트 표시 Text UI

    ExitGames.Client.Photon.Hashtable m_Team1WinProps =
                        new ExitGames.Client.Photon.Hashtable();
    ExitGames.Client.Photon.Hashtable m_Team2WinProps =
                        new ExitGames.Client.Photon.Hashtable();
    //--------------- Round 관련 변수

    public Text m_GameEndText;

    void Awake()
    {
        //--------------- 팀대전 관련 변수 초기화
        m_Team1Pos[0] = new Vector3(88.4f, 20.0f, 77.9f);
        m_Team1Pos[1] = new Vector3(61.1f, 20.0f, 88.6f);
        m_Team1Pos[2] = new Vector3(34.6f, 20.0f, 98.7f);
        m_Team1Pos[3] = new Vector3(7.7f, 20.0f, 108.9f);

        m_Team2Pos[0] = new Vector3(-19.3f, 20.0f, -134.1f);
        m_Team2Pos[1] = new Vector3(-43.1f, 20.0f, -125.6f);
        m_Team2Pos[2] = new Vector3(-66.7f, 20.0f, -117.3f);
        m_Team2Pos[3] = new Vector3(-91.4f, 20.0f, -108.6f);

        m_GameState = GameState.GS_Ready;
        //--------------- 팀대전 관련 변수 초기화

        //PhotonView 컴포넌트 할당
        pv = GetComponent<PhotonView>();

        //탱크를 생성하는 함수 호출
        CreateTank();

        //모든 클아우드의 네트워크 메시지 수신을 다시 연결
        PhotonNetwork.IsMessageQueueRunning = true;

        //룸에 입장 후 기존 접속자 정보를 출력
        GetConnectPlayerCount();

        //----- CustomProperties 초기화
        InitSelTeamProps();
        InitReadyProps();
        InitGStateProps();
        InitTeam1WinProps();
        InitTeam2WinProps();
        //----- CustomProperties 초기화
    } //void Awake()

    // Start is called before the first frame update
    void Start()
    {
        //-- TeamSetting
        //내가 입장할때 나를 포함한 다른 사람들에게 내 등장을 알린다. 
        //-- 팀1 버튼 처리
        if (m_Team1ToTeam2 != null)
            m_Team1ToTeam2.onClick.AddListener(() =>
            {
                SendSelTeam("black"); //블랙팀으로 이동
            });

        if (m_Team1Ready != null)
            m_Team1Ready.onClick.AddListener(() =>
            {
                SendReady(1);
            });
        //-- 팀1 버튼 처리

        //-- 팀2 버튼 처리
        if (m_Team2ToTeam1 != null)
            m_Team2ToTeam1.onClick.AddListener(() =>
            {
                SendSelTeam("blue"); //블루팀으로 이동
            });

        if (m_Team2Ready != null)
            m_Team2Ready.onClick.AddListener(() =>
            {
                SendReady(1);
            });
        //-- 팀2 버튼 처리
        //-- TeamSetting

        if (ExitRoomBtn != null)
            ExitRoomBtn.onClick.AddListener(OnClickExitRoom);

        //로그 메시지에 출력할 문자열 생성
        string msg = "\n<color=#00ff00>["
                     + PhotonNetwork.LocalPlayer.NickName
                     + "] Connected</color>";
        //RPC 함수 호출
        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg);
    }

    private void OnApplicationFocus(bool focus)  //윈도우 창 활성화 비활성화 일때
    {
        PhotonInit.isFocus = focus;
    }

    // Update is called once per frame
    void Update()
    {
        //게임 플로어를 돌려도 되는 상태인지 확인한다.
        if (IsGamePossible() == false) 
            return;

        if (m_GameState == GameState.GS_Ready)
        {
            if (IsDifferentList() == true)
            {
                RefreshPhotonTeam();  //리스트 UI 갱신
            }
        }//if (m_GameState == GameState.GS_Ready)

        //체팅 구현 예제
        if (Input.GetKeyDown(KeyCode.Return))
        { //<-- 엔터치면 인풋 필드 활성화
            bEnter = !bEnter;

            if (bEnter == true)
            {
                textChat.gameObject.SetActive(bEnter);
                textChat.ActivateInputField(); 
                //<--- 커서를 인풋필드로 이동시켜 줌
            }
            else
            {
                textChat.gameObject.SetActive(bEnter);

                if (textChat.text != "")
                {
                    EnterChat();
                }
            }
        }//if (Input.GetKeyDown(KeyCode.Return))     

        AllReadyObserver();

        if (m_GameState == GameState.GS_Playing)
        {
            Team1Panel.SetActive(false);
            Team2Panel.SetActive(false);
            m_WaitTmText.gameObject.SetActive(false);
        }//if (m_GameState == GameState.GS_Playing)

        WinLoseObserver();
    } //void Update()

    void EnterChat()
    {
        string msg = "\n<color=#ffffff>[" + 
                    PhotonNetwork.LocalPlayer.NickName + "] "
                    + textChat.text + "</color>";
        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg);

        textChat.text = "";
    }

    //탱크를 생성하는 함수 
    void CreateTank()
    {
        float pos = Random.Range(-100.0f, 100.0f);
        PhotonNetwork.Instantiate("Tank",
            new Vector3(pos, 20.0f, pos), Quaternion.identity, 0);
    }

    //룸 접속자 정보를 조회하는 함수
    void GetConnectPlayerCount()
    {
        //현재 입장한 룸 정보를 받아옴
        Room currRoom = PhotonNetwork.CurrentRoom;  //using Photon.Realtime;

        //현재 룸의 접속자 수와 최대 접속 가능한 수를 문자열로 구성한 후 Text UI 항목에 출력
        txtConnect.text = currRoom.PlayerCount.ToString()
                          + "/"
                          + currRoom.MaxPlayers.ToString();
    }

    //네트워크 플레이어가 접속했을 때 호출되는 함수 
    public override void OnPlayerEnteredRoom(Player a_Player)
    {
        GetConnectPlayerCount();
    }

    //네트워크 플레이어가 룸을 나가거나 접속이 끊어졌을 때 호출되는 함수
    public override void OnPlayerLeftRoom(Player outPlayer)
    {
        GetConnectPlayerCount();
    }

    //룸 나가기 버튼 클릭 이벤트에 연결될 함수
    public void OnClickExitRoom()
    {
        //로그 메시지에 출력할 문자열 생성
        string msg = "\n<color=#ff0000>["
                     + PhotonNetwork.LocalPlayer.NickName
                     + "] Disconnected</color>";
        //RPC 함수 호출
        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg);
        //설정이 완료된 후 빌드 파일을 여러개 실행해
        //동일한 룸에 입장해보면 접속 로그가 표기되는 것을 확인할 수 있다.
        //또한 PhotonTarget.AllBuffered 옵션으로
        //RPC를 호출했기 때문에 나중에 입장해도 기존의 접속 로그 메시지가 표시된다.

        //마지막 사람이 방을 떠날 때 룸의 CustomProperties를 초기화 해 주어야 한다.
        if (PhotonNetwork.PlayerList != null && PhotonNetwork.PlayerList.Length <= 1)
        {
            if (PhotonNetwork.CurrentRoom != null)
                PhotonNetwork.CurrentRoom.CustomProperties.Clear();
        }

        //지금 나가려는 탱크를 찾아서 그 탱크의  
        //모든 CustomProperties를 초기화 해 주고 나가는 것이 좋다. 
        //(그렇지 않으며 나갔다 즉시 방 입장시 오류 발생한다.)
        if (PhotonNetwork.LocalPlayer != null)
            PhotonNetwork.LocalPlayer.CustomProperties.Clear();
        //그래야 중개되던 것이 모두 초기화 될 것이다.

        //현재 룸을 빠져나가며 생성한 모든 네트워크 객체를 삭제
        PhotonNetwork.LeaveRoom();
    }

    //룸에서 접속 종료됐을 때 호출되는 콜백 함수
    //PhotonNetwork.LeaveRoom(); 성공했을 때 
    public override void OnLeftRoom()  
    {
        SceneManager.LoadScene("scLobby");
    }

    [PunRPC]
    void LogMsg(string msg)
    {
        //로그 메시지 Text UI에 텍스트를 누적시켜 표시
        txtLogMsg.text = txtLogMsg.text + msg;
    }

    public static bool IsPointerOverUIObject() //UGUI의 UI들이 먼저 피킹되는지 확인하는 함수
    {
        PointerEventData a_EDCurPos = new PointerEventData(EventSystem.current);

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)

			List<RaycastResult> results = new List<RaycastResult>();
			for (int i = 0; i < Input.touchCount; ++i)
			{
				a_EDCurPos.position = Input.GetTouch(i).position;  
				results.Clear();
				EventSystem.current.RaycastAll(a_EDCurPos, results);
                if (0 < results.Count)
                    return true;
			}

			return false;
#else
        a_EDCurPos.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(a_EDCurPos, results);
        return (0 < results.Count);
#endif
    }//public bool IsPointerOverUIObject() 

    bool IsDifferentList() //true면 다르다는 뜻 false면 같다는 뜻
    {
        GameObject[] a_TkNodeItems = GameObject.FindGameObjectsWithTag("TKNODE_ITEM");

        if (a_TkNodeItems == null)
            return true;

        if (PhotonNetwork.PlayerList.Length != a_TkNodeItems.Length)
            return true;

        foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)
        {
            bool a_FindNode = false;
            PlayerNodeItem PlayerData = null;
            foreach (GameObject a_Node in a_TkNodeItems)
            {
                PlayerData = a_Node.GetComponent<PlayerNodeItem>();
                if (PlayerData == null)
                    continue;

                if (PlayerData.m_UniqID == a_RefPlayer.ActorNumber)
                {
                    if (PlayerData.m_TeamKind != ReceiveSelTeam(a_RefPlayer))
                        return true;  //해당 유저의 팀이 변경 되었다면...

                    if (PlayerData.m_IamReady != ReceiveReady(a_RefPlayer))
                        return true;  //해당 Ready 상태가 변경 되었다면...

                    a_FindNode = true;
                    break;
                }
            }//foreach (GameObject a_Node in a_TkNodeItems)

            if (a_FindNode == false)
                return true; //해당 유저가 리스트에 존재하지 않으면....

        }//foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)

        return false; //일치한다는 뜻
    }

    void RefreshPhotonTeam()
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("TKNODE_ITEM"))
        {
            Destroy(obj);
        }

        GameObject[] a_tanks = GameObject.FindGameObjectsWithTag("TANK");

        string a_TeamKind = "blue";
        GameObject a_TkNode = null;
        foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)
        {
            a_TeamKind = ReceiveSelTeam(a_RefPlayer);
            a_TkNode = (GameObject)Instantiate(m_TkNodeItem);

            //팀이 뭐냐?에 따라서 스크롤 뷰를 분기 해 준다.
            if (a_TeamKind == "blue")
                a_TkNode.transform.SetParent(scrollTeam1.transform, false);
            else if (a_TeamKind == "black")
                a_TkNode.transform.SetParent(scrollTeam2.transform, false);

            //생성한 RoomItem에 표시하기 위한 텍스트 정보 전달
            PlayerNodeItem PlayerData = a_TkNode.GetComponent<PlayerNodeItem>();
            //텍스트 정보를 표시
            if (PlayerData != null)
            {
                PlayerData.m_UniqID = a_RefPlayer.ActorNumber;
                PlayerData.m_TeamKind = a_TeamKind;
                PlayerData.m_IamReady = ReceiveReady(a_RefPlayer);
                bool isMine = 
                    (PlayerData.m_UniqID == PhotonNetwork.LocalPlayer.ActorNumber);
                PlayerData.DispPlayerData(a_RefPlayer.NickName, isMine);
            }

            //이름표 색깔 바꾸기
            ChangeTankNameColor(a_tanks, a_RefPlayer.ActorNumber, a_TeamKind);

        } //foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)

        //-- 나의 Ready 상태에 따라서 UI 변경해 주기
        if (ReceiveReady(PhotonNetwork.LocalPlayer) == true)
        { //내가 Ready 상태라면...
            m_Team1Ready.gameObject.SetActive(false);
            m_Team2Ready.gameObject.SetActive(false);

            m_Team1ToTeam2.gameObject.SetActive(false);
            m_Team2ToTeam1.gameObject.SetActive(false);
        }
        else //내가 아직 Ready 상태가 아니라면
        {
            a_TeamKind = ReceiveSelTeam(PhotonNetwork.LocalPlayer);
            if (a_TeamKind == "blue")
            {
                m_Team1Ready.gameObject.SetActive(true);
                m_Team2Ready.gameObject.SetActive(false);
                m_Team1ToTeam2.gameObject.SetActive(true);
                m_Team2ToTeam1.gameObject.SetActive(false);
            }
            else if (a_TeamKind == "black")
            {
                m_Team1Ready.gameObject.SetActive(false);
                m_Team2Ready.gameObject.SetActive(true);
                m_Team1ToTeam2.gameObject.SetActive(false);
                m_Team2ToTeam1.gameObject.SetActive(true);
            }
        } //else //내가 아직 Ready 상태가 아니라면
        //-- 나의 Ready 상태에 따라서 UI 변경해 주기

    }//void RefreshPhotonTeam()

    bool IsGamePossible()  //게임이 가능한 상태인지? 체크하는 함수
    {
        //나가는 타이밍에 포톤 정보들이 한플레임 먼저 사라지고 
        //LoadScene()이 한플레임 늦게 호출되는 문제 해결법
        if (PhotonNetwork.CurrentRoom == null || 
            PhotonNetwork.LocalPlayer == null)
            return false; //동기화 가능한 상태 일때만 업데이트를 계산해 준다.

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameState") == false ||
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Team1Win") == false ||
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Team2Win") == false)
            return false;

        m_GameState = ReceiveGState();
        m_Team1Win = (int)PhotonNetwork.CurrentRoom.CustomProperties["Team1Win"];
        m_Team2Win = (int)PhotonNetwork.CurrentRoom.CustomProperties["Team2Win"];

        return true;
    }

#region ------------- 게임 상태 동기화 처리
    void InitGStateProps()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        m_StateProps.Clear();
        m_StateProps.Add("GameState", (int)GameState.GS_Ready);
        PhotonNetwork.CurrentRoom.SetCustomProperties(m_StateProps);
    }

    void SendGState(GameState a_GState)
    {
        if (m_StateProps == null)
        {
            m_StateProps = new ExitGames.Client.Photon.Hashtable();
            m_StateProps.Clear();
        }

        if (m_StateProps.ContainsKey("GameState") == true)
            m_StateProps["GameState"] = (int)a_GState;
        else
            m_StateProps.Add("GameState", (int)a_GState);

        PhotonNetwork.CurrentRoom.SetCustomProperties(m_StateProps);

    }

    GameState ReceiveGState() //GameState 받아서 처리하는 부분
    {
        GameState a_RmVal = GameState.GS_Ready;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameState") == true)
            a_RmVal = (GameState)PhotonNetwork.CurrentRoom.CustomProperties["GameState"];

        return a_RmVal;
    }

#endregion  //------------- 게임 상태 동기화 처리

#region --------------- 팀선택 동기화 처리
    void InitSelTeamProps()
    { //속도를 위해 버퍼를 미리 만들어 놓는다는 의미
        m_SelTeamProps.Clear();
        m_SelTeamProps.Add("MyTeam", "blue");   //기본적으로 나는 블루팀으로 시작한다.
        PhotonNetwork.LocalPlayer.SetCustomProperties(m_SelTeamProps);
        //캐릭터 별로 동기화 시키고 싶은 경우
    }//void InitSelTeamProps()

    void SendSelTeam(string a_Team)
    {
        if (string.IsNullOrEmpty(a_Team) == true)
            return;

        if (m_SelTeamProps == null)
        {
            m_SelTeamProps = new ExitGames.Client.Photon.Hashtable();
            m_SelTeamProps.Clear();
        }

        if (m_SelTeamProps.ContainsKey("MyTeam") == true)
            m_SelTeamProps["MyTeam"] = a_Team;
        else
            m_SelTeamProps.Add("MyTeam", a_Team);

        PhotonNetwork.LocalPlayer.SetCustomProperties(m_SelTeamProps);
        //캐릭터 별로 동기화 시키고 싶은 경우

    }//void SendSelTeam(string a_Team)

    string ReceiveSelTeam(Player a_Player) //SelTeam 받아서 처리하는 부분
    {
        string a_TeamKind = "blue";

        if (a_Player == null)
            return a_TeamKind;

        if (a_Player.CustomProperties.ContainsKey("MyTeam") == true)
            a_TeamKind = (string)a_Player.CustomProperties["MyTeam"];

        return a_TeamKind;
    }
#endregion  //--------------- 팀선택 동기화 처리

#region ------------ Ready 상태 동기화 처리

    void InitReadyProps()
    { //속도를 위해 버퍼를 미리 만들어 놓는다는 의미
        m_PlayerReady.Clear();
        m_PlayerReady.Add("IamReady", 0); //기본적으로 아직 준비전 상태로 시작한다.
        PhotonNetwork.LocalPlayer.SetCustomProperties(m_PlayerReady);
        //캐릭터 별로 동기화 시키고 싶은 경우
    }//void InitSelTeamProps()

    void SendReady(int a_Ready = 1)
    {
        if (m_PlayerReady == null)
        {
            m_PlayerReady = new ExitGames.Client.Photon.Hashtable();
            m_PlayerReady.Clear();
        }

        if (m_PlayerReady.ContainsKey("IamReady") == true)
            m_PlayerReady["IamReady"] = a_Ready;
        else
            m_PlayerReady.Add("IamReady", a_Ready);

        PhotonNetwork.LocalPlayer.SetCustomProperties(m_PlayerReady);  //캐릭터 별로 동기화 시키고 싶은 경우
    }

    bool ReceiveReady(Player a_Player)
    {
        if (a_Player == null)
            return false;

        if (a_Player.CustomProperties.ContainsKey("IamReady") == false)
            return false;

        if ((int)a_Player.CustomProperties["IamReady"] == 1)
            return true;

        return false;
    }

#endregion  //----------- Ready 상태 동기화 처리

#region --------------- Observer Method 모음 

    // 참가 유저 모두 Ready 버튼 눌렀는지 감시하고 게임을 시작하게 처리하는 함수
    void AllReadyObserver()
    {
        if (m_GameState != GameState.GS_Ready) //GS_Ready 상태에서만 확인한다.
            return;

        int a_OldGoWait = (int)m_GoWaitGame;

        bool a_AllReady = true;
        foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)
        {
            if (ReceiveReady(a_RefPlayer) == false)
            {
                a_AllReady = false;
                break;
            }
        }//foreach (Player a_RefPlayer in PhotonNetwork.PlayerList)

        if (a_AllReady == true) //모두가 준비 버튼을 누르고 기다리고 있다는 뜻 
        {
            //누가 발생시켰든 동기화 시키려고 하면....
            if (m_RoundCount == 0 && PhotonNetwork.CurrentRoom.IsOpen == true)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                //게임이 시작되면 다른 유저 들어오지 못하도록 막는 부분
                //PhotonNetwork.CurrentRoom.IsVisible = false; 
                //로비에서 방 목록에서도 보이지 않게 하기
            }

            //--- 각 플레이어 PC 별로 3, 2, 1, 0 타이머 UI 표시를 위한 코드
            if (0.0f < m_GoWaitGame)  //타이머 카운티 처리
            {
                m_GoWaitGame = m_GoWaitGame - Time.deltaTime;

                if (m_WaitTmText != null)
                {
                    m_WaitTmText.gameObject.SetActive(true);
                    m_WaitTmText.text = ((int)m_GoWaitGame).ToString();
                }

                //마스터 클라이언트는 각 유저의 자리배치를 해 줄 것이다.
                //총 3번만 보낸다. MasterClient가 나갈 경우를 대비해서...
                if (PhotonNetwork.IsMasterClient == true)
                if (0.0f < m_GoWaitGame && a_OldGoWait != (int)m_GoWaitGame)
                { //자리 배정
                    SitPosInxMasterCtrl();
                }//if(a_OldGoWait != (int)m_GoWaitGame) //자리 배정

                if (m_GoWaitGame <= 0.0f) //이건 한번만 발생할 것이다.
                {//진짜 게임 시작 준비
                    m_RoundCount++;

                    Team1Panel.SetActive(false);
                    Team2Panel.SetActive(false);
                    m_WaitTmText.gameObject.SetActive(false);

                    m_ChekWinTime = 2.0f;
                    m_GoWaitGame = 0.0f;

                }//if (m_GoWaitGame <= 0.0f)

            }//if (0.0f < m_GoWaitGame) 
            //--- 각 플레이어 PC 별로 타이머 UI 표시를 위한 코드

            //게임이 시작 되었어야 하는데 아직 시작 되지 않았다면....
            if (PhotonNetwork.IsMasterClient == true) //마스터 클라이언트만 체크하고 보낸다.
            if (m_GoWaitGame <= 0.0f) 
            { // && ReceiveGState() == GameState.GS_Ready) //위에서 체크함 
                SendGState(GameState.GS_Playing);
            }

        }//if (a_AllReady == true)

    }// void AllReadyObserver()

    void SitPosInxMasterCtrl()
    {
        int a_Tm1Count = 0;
        int a_Tm2Count = 0;
        string a_TeamKind = "blue";
        foreach (Player _player in PhotonNetwork.PlayerList) //using Photon.Realtime;
        {
            if (_player.CustomProperties.ContainsKey("MyTeam") == true)
                a_TeamKind = (string)_player.CustomProperties["MyTeam"];

            if (a_TeamKind == "blue")
            {
                SitPosInxProps.Clear();
                SitPosInxProps.Add("SitPosInx", a_Tm1Count);
                _player.SetCustomProperties(SitPosInxProps);
                a_Tm1Count++;
            }
            else if (a_TeamKind == "black")
            {
                SitPosInxProps.Clear();
                SitPosInxProps.Add("SitPosInx", a_Tm2Count);
                _player.SetCustomProperties(SitPosInxProps);
                a_Tm2Count++;
            }

        }//foreach (Player _player in PhotonNetwork.PlayerList)
    }//void SitPosInxMasterCtrl()

    #endregion //--------------- Observer Method 모음 


    void ChangeTankNameColor(GameObject[] a_tanks, int ActorNumber, string a_TeamKind)
    {
        //이름표 색깔 바꾸기
        DisplayUserID a_DpUserId = null;
        foreach (GameObject tank in a_tanks)
        {
            a_DpUserId = tank.GetComponent<DisplayUserID>();
            if (a_DpUserId == null)
                continue;

            if (a_DpUserId.pv.Owner.ActorNumber == ActorNumber)
            {
                if (a_TeamKind == "blue")
                    a_DpUserId.userId.color = new Color32(60, 60, 255, 255);
                else
                    a_DpUserId.userId.color = Color.black;

                break;
            }//if (a_DpUserId.pv.Owner.ActorNumber == ActorNumber)

        }//foreach (GameObject tank in a_tanks)
    }//void ChangeTankNameColor()

    void OnGUI()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        // 게임이 아직 시작되지 않은 경우는 
        // 각 "유저의 별명 : 킬수 : 사망상태"는 표시하지 않는다.
        if (PhotonNetwork.CurrentRoom.IsOpen == true)
            return;

        if (m_RoundCount == 0) //게임이 아직 시작되지 않았으면 리턴
            return;

        //현재 입장한 룸에 접속한 모든 네트워크 플레이어 정보를 저장
        int a_CurHP = 0;
        int currKillCount = 0;
        Player[] players = PhotonNetwork.PlayerList; //using Photon.Realtime;
        string PlayerTeam = "blue";

        GameObject[] tanks = GameObject.FindGameObjectsWithTag("TANK");

        foreach (Player _player in players)
        {
            currKillCount = 0;
            if (_player.CustomProperties.ContainsKey("KillCount") == true)
                currKillCount = (int)_player.CustomProperties["KillCount"];

            PlayerTeam = "blue";
            if (_player.CustomProperties.ContainsKey("MyTeam") == true)
                PlayerTeam = (string)_player.CustomProperties["MyTeam"];

            PlayerDamage playerDamage = null;
            foreach (GameObject tank in tanks)
            {
                PlayerDamage a_tankDmg = tank.GetComponent<PlayerDamage>();
                //탱크의 playerId가 포탄의 playerId와 동일한지 판단
                if (a_tankDmg == null)
                    continue;
                if (a_tankDmg.playerId == _player.ActorNumber)
                {
                    playerDamage = a_tankDmg;
                    break;
                }
            }//foreach (GameObject tank in tanks)

            if (playerDamage != null) 
            { //모든 캐릭터의 에너지바 동기화
                a_CurHP = playerDamage.currHp; 
            }//(tankDamage != null) 

            //if (PlayerTeam == "blue")
            //{
            //    if (a_CurHP <= 0) //죽어 있을 때 
            //    {
            //        GUILayout.Label("<color=Blue><size=25>" +
            //            "[" + _player.ActorNumber + "] " + _player.NickName + " "
            //            + currKillCount + " kill" + "</size></color>"
            //            + "<color=Red><size=25>" + " <Die>" + "</size></color>");
            //    }
            //    else  //살아 있을 때 
            //    {
            //        GUILayout.Label("<color=Blue><size=25>" + 
            //            "[" + _player.ActorNumber + "] " + _player.NickName + " " 
            //            + currKillCount + " kill" + "</size></color>");
            //    }
            //}
            //else //if (PlayerTeam == "black")
            //{
            //    if (a_CurHP <= 0)
            //    {
            //        GUILayout.Label("<color=Black><size=25>" +
            //            "[" + _player.ActorNumber + "] " + _player.NickName + " "
            //            + currKillCount + " kill" + "</size></color>"
            //            + "<color=Red><size=25>" + " <Die>" + "</size></color>");
            //    }
            //    else
            //    {
            //        GUILayout.Label("<color=Black><size=25>" + 
            //            "[" + _player.ActorNumber + "] " + _player.NickName + " " 
            //            + currKillCount + " kill" + "</size></color>");
            //    }
            //}// else //if (PlayerTeam == "black")

        }//foreach (Player _player in players)
    }//void OnGUI()

    //한쪽팀이 전멸했는지 체크하고 승리 / 패배 를 감시하고 처리해 주는 함수
    void WinLoseObserver()
    {
        //------------------- 승리 / 패배 체크
        if (m_GameState == GameState.GS_Playing)
        {   //GS_Ready 상태의 중계가 좀 늦게와서 한쪽이 전멸 상태라는 걸 몇번 체크할 수는 있다.
            m_ChekWinTime = m_ChekWinTime - Time.deltaTime;
            if (m_ChekWinTime <= 0.0f) //게임이 시작된 후 2초 뒤부터 판정을 시작하기 위한 부분
            {
                CheckAliveTeam();
            }
        }//if (m_GameState == GameState.GS_Playing)

        if (m_WinLossCount != null)
            m_WinLossCount.text = "<color=Blue>" + "Team1 : " +
                                   m_Team1Win.ToString() + " 승 " + "</color> / "
                                + "<color=Black>" + "Team2 : " +
                                   m_Team2Win.ToString() + " 승 " + "</color>";

        if (5 <= (m_Team1Win + m_Team2Win)) // 5Round까지 모두 플레이된 상황이라면... 
        {
            //Game Over 처리
            if (PhotonNetwork.IsMasterClient == true)
                SendGState(GameState.GS_GameEnd); //<--- 여기서는 지금 룸을 의미함 

            if (m_GameEndText != null)
            {
                m_GameEndText.gameObject.SetActive(true);
                if (m_Team1Win < m_Team2Win)
                    m_GameEndText.text = "<color=Black>" + "블랙팀 승" + "</color>";
                else //if (m_Team2Win < m_Team1Win)
                    m_GameEndText.text = "<color=Blue>" + "블루팀 승" + "</color>";
            }

            if (m_WaitTmText != null)
                m_WaitTmText.gameObject.SetActive(false);

            return;
        }//if (5 <= (m_Team1Win + m_Team2Win))

        //-------------- 한 Round가 끝나고 다음 Round의 게임을 시작 시키기 위한 부분... 
        //모든탱크 GS_Ready 상태일 때 모든 탱크 대기 상태로 만들기...
        if (m_OldState != GameState.GS_Ready && m_GameState == GameState.GS_Ready)
        {
            GameObject[] tanks = GameObject.FindGameObjectsWithTag("TANK");
            foreach (GameObject tank in tanks)
            {
                PlayerDamage playerDamage = tank.GetComponent<PlayerDamage>();
                if (playerDamage != null)
                    playerDamage.ReadyStateTank(); //다음 라운드 준비 --> 1
            }
        }
        m_OldState = m_GameState;
        //-------------- 한 Round가 끝나고 다은 Round의 게임을 시작 시키기 위한 부분... 


    }//void WinLoseObserver()

    void CheckAliveTeam()
    {
        int a_Tm1Count = 0;
        int a_Tm2Count = 0;
        int rowTm1 = 0;
        int rowTm2 = 0;
        int a_CurHP = 0;
        string a_PlrTeam = "blue"; //Player Team

        GameObject[] tanks = GameObject.FindGameObjectsWithTag("TANK");

        Player[] players = PhotonNetwork.PlayerList; //using Photon.Realtime;
        foreach (Player _player in players)
        {
            if (_player.CustomProperties.ContainsKey("MyTeam") == true)
                a_PlrTeam = (string)_player.CustomProperties["MyTeam"];

            PlayerDamage playerDamage = null;
            foreach (GameObject tank in tanks)
            {
                PlayerDamage a_tankDmg = tank.GetComponent<PlayerDamage>();
                //탱크의 playerId가 포탄의 playerId와 동일한지 판단
                if (a_tankDmg == null)
                    continue;

                if (a_tankDmg.playerId == _player.ActorNumber)
                {
                    playerDamage = a_tankDmg;
                    break;
                }
            }//foreach (GameObject tank in tanks)

            if (a_PlrTeam == "blue")
            {
                if (playerDamage != null && 0 < playerDamage.currHp)
                    rowTm1 = 1;  //팀1 중에 한명이라도 살아 있다는 의미
                a_Tm1Count++;    //이 방에 남아 있는 팀1 의 플레이어 수
            }
            else if (a_PlrTeam == "black")
            {
                if (playerDamage != null && 0 < playerDamage.currHp)
                    rowTm2 = 1;  //팀2 중에 한명이라도 살아 있다는 의미
                a_Tm2Count++;    //이 방에 남아 있는 팀2 의 플레이어 수
            }

        }//foreach (Player _player in players)

        //GameState.GS_Playing 상태일 때 계속 셋팅하다가
        m_GoWaitGame = 4.0f; //다시 4초후에 게임이 시작되도록...
        //GameState.GS_Ready 상태로 바뀌면 m_GoWaitGame 감소시키며 카운팅한다.

        if (0 < rowTm1 && 0 < rowTm2) //양 팀이 모두 한명 이상 살아 있다는 의미
            return;

        if( 5 <= (m_Team1Win + m_Team2Win)) 
            return;     //5Round까지 모두 진행 했으면 체크할 필요 없음

        if (PhotonNetwork.IsMasterClient == false)
            return;     //승리 패배 값의 중계는 마스터 클라이언트만 하겠다는 의미

        SendGState(GameState.GS_Ready);

        if (rowTm1 == 0) //팀1 전멸 상태
        {
            if (-99999.0f < m_ChekWinTime) //한번만 ++ 시키기 위한 용도
            {
                m_Team2Win++;
                if (m_GameState != GameState.GS_GameEnd && a_Tm1Count <= 0)  //팀1이 모두 나가버린 경우 강제 승리 처리
                    m_Team2Win = 5 - m_Team1Win;

                //여러번 발생하더라도 아직은 업데이트가 
                //안된 상태이기 때문에 이전 값에서 추가될 것이다.
                IsRoomBuf_Team2Win = m_Team2Win;
                m_ChekWinTime = -150000.0f;
            }
            SendTeam2Win(IsRoomBuf_Team2Win);
        }
        else if (rowTm2 == 0) //팀2 전멸 상태
        {
            if (-99999.0f < m_ChekWinTime) //한번만 ++ 시키기 위한 용도
            {
                m_Team1Win++;
                if (m_GameState != GameState.GS_GameEnd && a_Tm2Count <= 0)  //팀2이 모두 나가버린 경우 강제 승리 처리
                    m_Team1Win = 5 - m_Team2Win;
                IsRoomBuf_Team1Win = m_Team1Win;
                m_ChekWinTime = -150000.0f;
            }
            SendTeam1Win(IsRoomBuf_Team1Win);
        }

    }// void CheckAliveTeam()

#region --------------- Team1 Win Count

    void InitTeam1WinProps()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        m_Team1WinProps.Clear();
        m_Team1WinProps.Add("Team1Win", 0);
        PhotonNetwork.CurrentRoom.SetCustomProperties(m_Team1WinProps);
    }

    void SendTeam1Win(int a_WinCount)
    {
        if (m_Team1WinProps == null)
        {
            m_Team1WinProps = new ExitGames.Client.Photon.Hashtable();
            m_Team1WinProps.Clear();
        }

        if (m_Team1WinProps.ContainsKey("Team1Win") == true)
            m_Team1WinProps["Team1Win"] = a_WinCount;
        else
            m_Team1WinProps.Add("Team1Win", a_WinCount);

        PhotonNetwork.CurrentRoom.SetCustomProperties(m_Team1WinProps);
    }
#endregion  //--------------- Team1 Win Count

#region --------------- Team2 Win Count
    void InitTeam2WinProps()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        m_Team2WinProps.Clear();
        m_Team2WinProps.Add("Team2Win", 0);
        PhotonNetwork.CurrentRoom.SetCustomProperties(m_Team2WinProps);
    }

    void SendTeam2Win(int a_WinCount)
    {
        if (m_Team2WinProps == null)
        {
            m_Team2WinProps = new ExitGames.Client.Photon.Hashtable();
            m_Team2WinProps.Clear();
        }

        if (m_Team2WinProps.ContainsKey("Team2Win") == true)
            m_Team2WinProps["Team2Win"] = a_WinCount;
        else
            m_Team2WinProps.Add("Team2Win", a_WinCount);

        PhotonNetwork.CurrentRoom.SetCustomProperties(m_Team2WinProps);
    }

#endregion  //--------------- Team2 Win Count

}
