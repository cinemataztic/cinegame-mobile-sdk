using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GC_DataHolder : MonoBehaviour 
{
	public static GC_DataHolder instance;

	private List<GameObject> sharedGameObjects;

	void Awake()
	{
		instance = this;
		sharedGameObjects = new List<GameObject> ();
	}

	public GameObject TakeAndRemove(string gameObjectName)
	{
		GameObject go = instance.sharedGameObjects.Where(x => x.gameObject.name.Equals(gameObjectName)).FirstOrDefault();
		instance.sharedGameObjects.Remove (go);
		return go;
	}

	public void Add(GameObject go)
	{
		if (go == null){
			Debug.LogError ("GC_DataHolder Add: go was null");
			return;
		} 
		else if (sharedGameObjects.Any (x => x.name.Equals (go.name))) {
			Debug.LogError ("GC_DataHolder Add: GameObject with same name already exists in sharedGameObjects list");
			return;
		}

		instance.sharedGameObjects.Add (go);
	}
}