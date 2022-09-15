using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNodeItem : MonoBehaviour
{
    [HideInInspector] public int m_UniqID = -1;       //������ ������ȣ
    [HideInInspector] public string m_TeamKind = "";  //��
    [HideInInspector] public bool m_IamReady = false; //Ready����

    //Tank �̸� ǥ���� Text UI �׸�
    public Text TextTankName;
    //Ready ���� ǥ�ø� ���� Text UI �׸�
    public Text TextStateInfo;

    //// Start is called before the first frame update
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{

    //}

    public void DispPlayerData(string a_TankName, bool isMine = false)
    {
        if (isMine == true)
        {
            TextTankName.color = Color.magenta;
            TextTankName.text = a_TankName; //"<color=#ff00ff>" + a_TankName + "</color>";
        }
        else
            TextTankName.text = a_TankName;

        if (m_IamReady == true)
            TextStateInfo.text = "<color=#ff0000>Ready</color>";
        else
            TextStateInfo.text = "";
    }
}
