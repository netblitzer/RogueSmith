using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class WeaponSaver {

	private string path;
	private DirectoryInfo dir;
	private Dictionary<string, FileInfo> files;

	// Use this for initialization
	public int Init () {
		// go to and find all the files in the saves directory
		path = Path.Combine(Application.dataPath, "Resources/Saves/");
		dir = new DirectoryInfo (path);

		// init the dictionary
		files = new Dictionary<string, FileInfo> ();

		// load in all the files
		FileInfo[] info = dir.GetFiles ("*.json");

		// put each file into a dictionary with their name as the key
		foreach(FileInfo file in info) {
			string name = file.Name.Split('.')[0];
			Debug.Log ("Reading in weapon: " + name);
			files.Add (name, file);
		}

		return info.Length;
	}

	public int fileCount () {

		return files.Keys.ToArray ().Length;
	}

	public void loadWeapon () {
		
	}

	public void saveWeapon (Weapon _w, string _name) {
		string weaponJSON = _w.toJson ();

		string completedPath = path + _name + ".json";

		Debug.Log (weaponJSON);
		Debug.Log (completedPath);

		File.WriteAllText (completedPath, weaponJSON);
		FileInfo f = new FileInfo (completedPath);

		files.Add (_name, f);
	}
}
