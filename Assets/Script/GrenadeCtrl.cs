using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeCtrl : MonoBehaviour
{
    public GameObject expEffect;

    Rigidbody rigid;
    bool isShot = false;

    // Start is called before the first frame update
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        rigid.isKinematic = true;
        //시작과 동시에 물체가 추락하지 않도록 하기 위한 코드
        Destroy(gameObject, 10.0f);

    }

    // Update is called once per frame
    void Update()
    {
        if (isShot == false)
        {
            rigid.isKinematic = false;
            //물체가 여러 물리력을 받도록 허용하는 코드
            Shoot();
            // 발사!!
            isShot = true;
        }
    }
    public void Shoot()
    {   //Y축으로 200만큼 Z 축으로 2000만큼의 힘으로 발사시키는 함수
        Vector3 speed = transform.forward + new Vector3(0, 200, 2000);
        rigid.AddForce(speed);
        
    }

    void OnCollisionEnter(Collision coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("TERRAIN"))
        {
            ExpGrenade();
        }
    }

    void ExpGrenade()
    {
        //폭발 효과 파티클 생성
        GameObject explosion = Instantiate(expEffect,
                               this.transform.position, Quaternion.identity);
        Destroy(explosion,
            explosion.GetComponentInChildren<ParticleSystem>().main.duration + 2.0f);

        //지정한 원점을 중심으로 10.0f 반경 내에 들어와 있는 Collider 객체 추출
        Collider[] colls = Physics.OverlapSphere(this.transform.position, 10.0f);

        //////추출한 Collider 객체에 폭발력 전달
        //MonsterCtrl a_MonCtrl = null;
        //foreach (Collider coll in colls)
        //{
        //    a_MonCtrl = coll.GetComponent<MonsterCtrl>();
        //    if (a_MonCtrl == null)
        //        continue;

        //    a_MonCtrl.TakeDamage(150);
        //}

        //즉시 제거
        Destroy(gameObject);
    }
}
