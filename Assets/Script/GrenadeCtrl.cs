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
        //���۰� ���ÿ� ��ü�� �߶����� �ʵ��� �ϱ� ���� �ڵ�
        Destroy(gameObject, 10.0f);

    }

    // Update is called once per frame
    void Update()
    {
        if (isShot == false)
        {
            rigid.isKinematic = false;
            //��ü�� ���� �������� �޵��� ����ϴ� �ڵ�
            Shoot();
            // �߻�!!
            isShot = true;
        }
    }
    public void Shoot()
    {   //Y������ 200��ŭ Z ������ 2000��ŭ�� ������ �߻��Ű�� �Լ�
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
        //���� ȿ�� ��ƼŬ ����
        GameObject explosion = Instantiate(expEffect,
                               this.transform.position, Quaternion.identity);
        Destroy(explosion,
            explosion.GetComponentInChildren<ParticleSystem>().main.duration + 2.0f);

        //������ ������ �߽����� 10.0f �ݰ� ���� ���� �ִ� Collider ��ü ����
        Collider[] colls = Physics.OverlapSphere(this.transform.position, 10.0f);

        //////������ Collider ��ü�� ���߷� ����
        //MonsterCtrl a_MonCtrl = null;
        //foreach (Collider coll in colls)
        //{
        //    a_MonCtrl = coll.GetComponent<MonsterCtrl>();
        //    if (a_MonCtrl == null)
        //        continue;

        //    a_MonCtrl.TakeDamage(150);
        //}

        //��� ����
        Destroy(gameObject);
    }
}
