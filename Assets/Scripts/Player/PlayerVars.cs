using UnityEngine;
using System.Collections;

public class PlayerVars : MonoBehaviour
{
	//Переменные
	public float HP = 100f;
	public float HPMAX = 100f;
	public float MP = 50f;
	public float AttackSpeed = 0.5f;
	public float Damage = 15f;
	public float Evasion = 0.1f;
	public float Armor = 10f;
	public float Regen = 4f;
	public float Range = 5f;

	public float DamageTick;
	public bool DamageTrig;

	private float RegenAlarm = 0f;

	public float Width = 100.0f;
	public float Height = 10.0f;

	private Texture2D hbar;
	private Texture2D hbarr;

	//Create a texture
	private Texture2D TEX(int width,int height,Color color)
	{
		Color[] pixel = new Color[width * height];
		for(int i=0; i<pixel.Length; i++)
		{
			pixel[i] = color;
		}
		Texture2D result = new Texture2D(width,height);
		result.SetPixels(pixel);
		result.Apply();
		return result;
	}

	void Start()
	{
		//Создаем однотонные текстуры бара и фона
		hbar = TEX(100,10,Color.red);
		hbarr = TEX(100,10,Color.black);
		RegenAlarm = Time.time;
	}

	void Update()
	{
		if(HP<=0)
		{
			//transform.position = new Vector3(40f,3.05f,40f);			
			//HP = HPMAX;
		}
		else if(HP<HPMAX && Time.time - RegenAlarm >= .5f)
		{
			HP+=Regen;
			RegenAlarm = Time.time;
		}
		if(DamageTrig)
		{
			if(Time.time-DamageTick > 4f)
			{
				Damage /= 2f;
				DamageTrig = false;
			}
		}
		//Ограничитель
		HP = Mathf.Min(Mathf.Max(0,HP),HPMAX);
	}

	void OnGUI()
	{
		//Отрисовка фона и самого бара
		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
		GUI.DrawTexture(new Rect(pos.x-Width/2,Screen.height - pos.y - Height*8,Width,Height),hbarr);
		GUI.DrawTexture(new Rect(pos.x-Width/2,Screen.height - pos.y - Height*8,Width*HP/HPMAX,Height),hbar);
	}
}