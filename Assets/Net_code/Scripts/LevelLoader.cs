using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miner.NetCode
{
    public class LevelLoader : MonoBehaviour
    {
		private static LevelLoader _instance = null;

		public static LevelLoader Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = (new GameObject("LevelLoader", typeof(LevelLoader))).GetComponent<LevelLoader>();
				}
				return _instance;
			}
		}

		void Awake()
		{
			DontDestroyOnLoad(this.gameObject);
		}

		public void LoadLevel(int level, System.Action callback = null)
		{
			StartCoroutine(LoadLevelCoroutine(level, callback));
		}

		IEnumerator LoadLevelCoroutine(int levelNumber, System.Action callback)
		{
			AsyncOperation async = Application.LoadLevelAsync(levelNumber);
			yield return async;

			if (async.isDone)
			{
				callback?.Invoke();
			}
		}
	}
}