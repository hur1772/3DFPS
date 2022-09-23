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
        RaycastHit hit;
        Debug.DrawRay(transform.position, -transform.up * 0.35f, Color.red);

        if (Physics.Raycast(transform.position, -transform.up, out hit, 0.35f))
        {
            ExpGrenade();
        }

        //Ray ray = gameObject.ScreenPointToRay(Input.mousePosition);
        ////������ Ray�� Scene �信 ��� �������� ǥ��
        //Debug.DrawRay(ray.origin, ray.direction * 100.0f, Color.green);
    }
    public void Shoot()
    {   //Y������ 200��ŭ Z ������ 2000��ŭ�� ������ �߻��Ű�� �Լ�
        Vector3 speed = transform.forward * 2000;
        rigid.AddForce(speed);
        
    }

    //void OnCollisionEnter(Collision coll)
    //{
    //    ExpGrenade();
    //}

    void ExpGrenade()
    {
        //���� ȿ�� ��ƼŬ ����
        GameObject explosion = Instantiate(expEffect,
                               this.transform.position, Quaternion.identity);
        Destroy(explosion,
            explosion.GetComponentInChildren<ParticleSystem>().main.duration + 1.0f);

        //������ ������ �߽����� 10.0f �ݰ� ���� ���� �ִ� Collider ��ü ����
        Collider[] colls = Physics.OverlapSphere(this.transform.position, 20.0f);

        ////������ Collider ��ü�� ���߷� ����
        PlayerDamage a_PlayerDamage = null;
        foreach (Collider coll in colls)
        {
            a_PlayerDamage = coll.GetComponent<PlayerDamage>();
            if (a_PlayerDamage == null)
                continue;

            float distance = Vector3.Distance(a_PlayerDamage.transform.position, transform.position);

            if ((int)(150 - distance * 20) > 0)
            {
                a_PlayerDamage.currHp -= (int)(150 - distance * 20);
            }
        }

        //��� ����
        Destroy(gameObject);
    }
}
