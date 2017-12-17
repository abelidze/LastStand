using UnityEngine;
using System.Collections;

public class DropControl : MonoBehaviour
{
	private Rigidbody rig;
	
	//Animation
	private Animation anim;
	enum CharS
	{
		Idle = 0,
		Open = 1,
		Fade = 2,
	}
	private CharS charS;

	private bool open = false;

	//Interpolation
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = new Vector3(50f,50f,50f);
	private Vector3 syncEndPosition = new Vector3(50f,50f,50f);
	private int ch;
	private CharS chr = CharS.Idle;
	private Quaternion rtIN;
	private Quaternion rtOUT;

	void Start()
	{
		anim = GetComponent<Animation>();
		rig = GetComponent<Rigidbody>();
		//Дабы не случилось форсмажоров
		transform.position = new Vector3(transform.position.x,50f,transform.position.z);
		anim.CrossFade("Idle");
	}

	void Update()
	{
		if(GetComponent<NetworkView>().isMine)
		{
			if(open)
			{
				if(!anim.IsPlaying("Open") && charS == CharS.Open)
				{
					anim.CrossFade("Fade");
					charS = CharS.Fade;
					rig.detectCollisions = false;
				}
				if(!anim.IsPlaying("Fade") && charS == CharS.Fade)
				{
					Network.Destroy(GetComponent<NetworkView>().viewID);
					Network.RemoveRPCs(GetComponent<NetworkView>().viewID);
				}
			}

			LayerMask layer = (1 << 8);
			if(Physics.Raycast(transform.position,-transform.up,1f,layer) && !open)
			{
				anim.CrossFade("Open");
				charS = CharS.Open;
				open = true;
			}
		}

		//Если это клон других игроков, то назначаем принятые переменные в методе OnSerializeNetworkView
		else
		{
			syncTime += Time.deltaTime;
			rig.position = Vector3.Slerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
			
			
			charS = chr;
			transform.rotation = rtOUT;
			
			//Контроллер анимаций
			if(anim)
			{
				switch(charS)
				{
				case CharS.Idle:
				anim.CrossFade("Idle");
				break;
					
				case CharS.Open:
				anim.CrossFade("Open");
				break;
					
				case CharS.Fade:
				anim.CrossFade("Fade");
				break;
				}
			}
		}
	}



	void OnMouseDown()
	{
		if(!open && rig.velocity.magnitude<1f)
		{
			anim.CrossFade("Open");
			charS = CharS.Open;
			open = true;
		}
	}



	//Обмен значениями данного скрипта
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		int ch = (int)charS;
		Quaternion rtIN = transform.rotation;
		if (stream.isWriting)
		{
			syncPosition = rig.position;
			stream.Serialize(ref syncPosition);
			
			syncVelocity = rig.velocity;
			stream.Serialize(ref syncVelocity);
			
			stream.Serialize(ref rtIN);
			
			stream.Serialize(ref ch);
		}
		else
		{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			stream.Serialize(ref rtIN);
			stream.Serialize(ref ch);
			
			chr = (CharS)ch;
			rtOUT = rtIN;
			
			//Интерполяция передвижения
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rig.position;
		}
	}
}
