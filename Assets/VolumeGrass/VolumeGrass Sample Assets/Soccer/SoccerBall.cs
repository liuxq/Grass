using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SoccerBall : MonoBehaviour {
	private LayerMask ballMask;
	public Material grassMaterial;
	public Collider groundCollider;
	public float radius;
	public float BendStrength;
	public float BendDarkening;
	
	private Light dlight1;
	private Light dlight2;
	private Light dlight3;
	private Light dlight4;
	private bool L_downflag=false;
	
	void Awake() {
		ballMask=1<<gameObject.layer;
		dlight1=GameObject.Find("Spotlight1").GetComponent<Light>();
		dlight2=GameObject.Find("Spotlight2").GetComponent<Light>();
		dlight3=GameObject.Find("Spotlight3").GetComponent<Light>();
		dlight4=GameObject.Find("Spotlight4").GetComponent<Light>();
	}
	
	void OnMouseDown() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 1000, ballMask)) {
			Vector3 dir=GetComponent<Rigidbody>().position-hit.point;
			dir.Normalize();
			GetComponent<Rigidbody>().AddForceAtPosition(dir*250, hit.point);
			GetComponent<Rigidbody>().AddForce(Vector3.up*10);
		}
	}
	
	void Update() {
		if (groundCollider==null || grassMaterial==null) return;
		if (Input.GetKey (KeyCode.L)) {
			if (!L_downflag) {
				L_downflag = true;
				if (dlight1.shadows == LightShadows.None) {
					dlight1.shadows = LightShadows.Hard;
				} else {
					dlight1.shadows = LightShadows.None;
				}
				if (dlight2.shadows == LightShadows.None) {
					dlight2.shadows = LightShadows.Hard;
				} else {
					dlight2.shadows = LightShadows.None; 
				}
				if (dlight3.shadows == LightShadows.None) {
					dlight3.shadows = LightShadows.Hard;
				} else {
					dlight3.shadows = LightShadows.None;
				}
				if (dlight4.shadows == LightShadows.None) {
					dlight4.shadows = LightShadows.Hard;
				} else {
					dlight4.shadows = LightShadows.None;
				}
			}
		} else {
			L_downflag = false;
		}

		Ray ray = new Ray(GetComponent<Rigidbody>().position+Vector3.up, -Vector3.up); 
		RaycastHit hit=new RaycastHit();
		if (groundCollider.Raycast(ray, out hit, 100f)) {
			grassMaterial.SetVector("_wind_affector", new Vector4(GetComponent<Rigidbody>().position.x, GetComponent<Rigidbody>().position.y, GetComponent<Rigidbody>().position.z, radius));
			grassMaterial.SetFloat("_wind_const_bend_affector", BendStrength);
			grassMaterial.SetFloat("affector_blend_darkening", BendDarkening);
			float dmp=Mathf.Clamp(1-(GetComponent<Rigidbody>().position.y-0.3042075f)/0.35f,0,1);
			GetComponent<Rigidbody>().drag=dmp*1.0f;
			float v=GetComponent<Rigidbody>().velocity.magnitude*5.0f;
			dmp/=(v<1) ? 1 : v;
			GetComponent<Rigidbody>().angularDrag=dmp*1.0f;
		}
	}
	
}
