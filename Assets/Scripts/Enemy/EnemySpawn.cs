using UnityEngine;
using System.Collections;

public class EnemySpawn: MonoBehaviour
{
	public float deltaTime;
	public float minTime;
	private int maxEnemy = 16;
	private int nowEnemy = 8;
	private bool play = true;
	public GameObject[] Enemies;

	void Update()
	{
		if((GameObject.FindGameObjectsWithTag("Enemy").Length<=0)&&(play))
		{
			play = false;
			StartCoroutine(SpawnEnemy());
		}
	}

	IEnumerator SpawnEnemy()
	{
		yield return new WaitForSeconds(10f);
		while(true)
		{
			if((Application.loadedLevel == 1)&&(Network.isServer))
			{
				if(GameObject.FindGameObjectsWithTag("Enemy").Length<nowEnemy)
				{
					Network.Instantiate(Resources.Load(Enemies[Random.Range(0,Enemies.Length)].name,typeof(GameObject)),transform.position, Quaternion.Euler(0,0,0), 0);
					deltaTime*=0.8f;
					if(deltaTime<minTime) deltaTime = minTime;
				}
				else
				{
					if(nowEnemy<maxEnemy) nowEnemy++;
					play = true;
					break;
				}
				yield return new WaitForSeconds(deltaTime);
			}
			else break;
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere (transform.position, 1.0f);
	}
}