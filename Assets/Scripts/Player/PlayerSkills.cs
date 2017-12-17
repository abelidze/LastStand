using UnityEngine;
using System.Collections;

public class PlayerSkills : MonoBehaviour
{
	public float upspeed = 1.5f;
	public Texture[] spell;

	private Controller obj;
	private PlayerVars var;
	private float run,press;

	void Start()
	{
		obj = GetComponent<Controller>();
		var = GetComponent<PlayerVars>();
		run = Time.time-10f;
		press = Time.time-10f;
	}
	
	void OnGUI()
	{
		GUIStyle st = new GUIStyle();
		st.normal.background = null;

		//RUN
		if(Time.time - run < 10f)
		{
			GUI.color = new Color(0.25f,0.25f,0.25f);
			if((Time.time - run > 5f)&&(Time.time - run < 5.5f))
			{
				obj.speed /= upspeed;
				run-=1f;
			}
		}
		if((Input.GetKeyDown("r"))||(GUI.Button(new Rect(Screen.width*0.9f,Screen.height*0.1f,spell[0].width/540f*Screen.height,spell[0].height/540f*Screen.height),spell[0],st)))
		{
			if(Time.time-run>10f)
			{
				run = Time.time;
				obj.speed *= upspeed;
			}
		}

		GUI.color = new Color(1f,1f,1f);

		//POWER
		if(Time.time - press < 10f)
		{
			GUI.color = new Color(0.25f,0.25f,0.25f);
		}
		if((Input.GetKeyDown("f"))||(GUI.Button(new Rect(Screen.width*0.9f,Screen.height*0.25f,spell[1].width/540f*Screen.height,spell[1].height/540f*Screen.height),spell[1],st)))
		{
			if(Time.time-press>10f)
			{
				press = Time.time;
				GetComponent<NetworkView>().RPC("PowerUP", RPCMode.Others,GetComponent<NetworkView>().viewID);
				var.Damage*=2f;
				var.DamageTick = Time.time;
				var.DamageTrig = true;
			}
		}
	}


	[RPC]
	void PowerUP(NetworkViewID id)
	{
		if(id == GetComponent<NetworkView>().viewID)
		{
			var.Damage*=2f;
			var.DamageTick = Time.time;
			var.DamageTrig = true;
		}
	}
}
