using System.Linq;
using Sfs2X.Entities.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Control a material property remotely or locally
	/// </summary>
	[ComponentReference ("Set the material property specified in the Property field. You can invoke the Set() methods locally or send a Key=Value message from host.\nIf you want to assign a texture remotely, you should use the TextureSelect component and invoke this component's Set method from it's OnChange event.")]
	public class MaterialProperty : ReplicatedComponent {
		[Header ("Key in message from host which can contain a property value")]
		public string Key;
		[Header ("Target object or its parent, can contain a MeshRenderer or a UI Image")]
		public GameObject TargetObject;
		[Header ("Optional name of instanced material to control")]
		public string MaterialName;
		[Header ("Material property name")]
		public string Property;

		public enum MaterialPropertyType {
			Integer,
			Float,
			Vector,
			Color,
			Texture,
			Keyword,
		}
		[Header ("Property type")]
		public MaterialPropertyType Type = MaterialPropertyType.Integer;

		Material MaterialInstance;

		void Start () {
			if (TargetObject == null) {
				TargetObject = gameObject;
			}
			SetTarget (TargetObject);
		}

		public void SetTarget (GameObject gameObject) {
			Log ($"MaterialProperty SetTarget {gameObject.GetScenePath ()}");
			TargetObject = gameObject;
			var renderer = TargetObject.GetComponentInChildren<Renderer> (includeInactive: true);
			if (renderer != null) {
				var materials = renderer.materials;
				if (materials.Length > 1 && !string.IsNullOrWhiteSpace (MaterialName)) {
					MaterialInstance = materials.FirstOrDefault (m => m.name == MaterialName);
					if (MaterialInstance == null) {
						LogError ($"Material with name={MaterialName} not found on Renderer in {gameObject.GetScenePath ()}");
					}
				} else {
					MaterialInstance = renderer.material;
				}
			} else {
				var image = TargetObject.GetComponentInChildren<Image> (includeInactive: true);
				if (image != null) {
					MaterialInstance = image.material;
				} else {
					var rawImage = TargetObject.GetComponentInChildren<RawImage> (includeInactive: true);
					if (rawImage != null) {
						MaterialInstance = rawImage.material;
					}
				}
			}
			if (MaterialInstance == null) {
				LogError ("No material instance found!");
			} else if (Type != MaterialPropertyType.Keyword && !MaterialInstance.HasProperty (Property)) {
				LogError ($"Material {MaterialInstance.name} has no property named {Property}");
			} else if (Type == MaterialPropertyType.Keyword && !MaterialInstance.shaderKeywords.Contains (Property)) {
				LogError ($"Material {MaterialInstance.name} has no keyword named {Property}");
			}
		}

		public void SetKeyword (bool v) {
			Log ($"MaterialProperty SetKeyword {(v ? "enable" : "disable")} {Property}");
			if (v)
				MaterialInstance.EnableKeyword (Property);
			else
				MaterialInstance.DisableKeyword (Property);
		}

		public void Set (Color v) {
			Log ($"MaterialProperty Set Color {Property}={v}");
			MaterialInstance.SetColor (Property, v);
		}

		public void Set (float v) {
			Log ($"MaterialProperty Set float {Property}={v}");
			MaterialInstance.SetFloat (Property, v);
		}

		public void Set (int v) {
			Log ($"MaterialProperty Set integer {Property}={v}");
			MaterialInstance.SetInteger (Property, v);
		}

		public void Set (Vector4 v) {
			Log ($"MaterialProperty Set Vector4 {Property}={v}");
			MaterialInstance.SetVector (Property, v);
		}

		public void Set (Vector3 v) {
			Log ($"MaterialProperty Set Vector3 {Property}={v}");
			MaterialInstance.SetVector (Property, v);
		}

		public void Set (Vector2 v) {
			Log ($"MaterialProperty Set Vector2 {Property}={v}");
			MaterialInstance.SetVector (Property, v);
		}

		public void Set (Texture v) {
			Log ($"MaterialProperty Set Texture {Property}={v.name}");
			MaterialInstance.SetTexture (Property, v);
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				float [] v;
				switch (Type) {
				case MaterialPropertyType.Integer:
					var i = dataObj.GetInt (Key);
					Log ($"MaterialProperty received {Property}={i}");
					MaterialInstance.SetInteger (Property, i);
					break;
				case MaterialPropertyType.Float:
					var f = dataObj.GetFloat (Key);
					Log ($"MaterialProperty received {Property}={f}");
					MaterialInstance.SetFloat (Property, f);
					break;
				case MaterialPropertyType.Color:
					v = dataObj.GetFloatArray (Key);
					Log ($"MaterialProperty received {Property}=(color)");
					MaterialInstance.SetColor (Property, new Color (v [0], v [1], v [2], (v.Length == 3) ? 1f : v [3]));
					break;
				case MaterialPropertyType.Vector:
					v = dataObj.GetFloatArray (Key);
					Log ($"MaterialProperty received {Property}=(vector)");
					switch (v.Length) {
					case 2:
						MaterialInstance.SetVector (Property, new Vector2 (v [0], v [1]));
						break;
					case 3:
						MaterialInstance.SetVector (Property, new Vector3 (v [0], v [1], v [2]));
						break;
					case 4:
						MaterialInstance.SetVector (Property, new Vector4 (v [0], v [1], v [2], v [3]));
						break;
					}
					break;
				case MaterialPropertyType.Keyword:
					if (dataObj.GetBool (Key)) {
						Log ($"MaterialProperty received enable keyword {Property}");
						MaterialInstance.EnableKeyword (Property);
					} else {
						Log ($"MaterialProperty received disable keyword {Property}");
						MaterialInstance.DisableKeyword (Property);
					}
					break;
				}
			}
		}
	}

}
