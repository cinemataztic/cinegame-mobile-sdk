using UnityEngine;
using UnityEngine.EventSystems;
using Sfs2X.Entities.Data;
using System;

namespace CineGame.MobileComponents {
	[RequireComponent (typeof (RectTransform))]
	public class DragDropAnywhereComponent : ReplicatedComponent {
		[Header ("Replication")]
		[Tooltip ("Smartfox uservariable name for x coordinate")]
		[SerializeField] string variableNameX = "x";
		[Tooltip ("Smartfox uservariable name for y coordinate")]
		[SerializeField] string variableNameY = "y";
		[Tooltip ("How often should coordinates be replicated")]
		[SerializeField] float updateInterval = 0.1f;
		[Tooltip ("Key in objectMessage to gamehost when drag begin or end (bool true/false)")]
		[SerializeField] string onDragStartedKey = "";

		[Header ("Settings")]
		public float Radius;

		[Header ("References")]
		[SerializeField] RectTransform circleTransform;
		[SerializeField] RectTransform analogStickTransform;

		private Vector3 lastMousePos;
		private Vector3 startPos;
		private Vector3 lastInputPos;
		private float lastUpdateTimer;
		private bool sendPosition = false;

		private void OnEnable () {
			startPos = transform.position;
			lastUpdateTimer = 0f;
		}

		private void Update () {
			if (Application.isEditor) {
				//Handle mouse input, for testing in editor
				if (Input.GetMouseButtonDown (0)) {
					lastMousePos = Input.mousePosition;
					OnBeginDrag (lastMousePos);
				} else if (Input.GetMouseButton (0)) {
					if (Input.mousePosition != lastMousePos) {
						OnDrag (Input.mousePosition, Input.mousePosition - lastMousePos);
					}
					lastMousePos = Input.mousePosition;
				} else if (Input.GetMouseButtonUp (0)) {
					OnEndDrag (Input.mousePosition);
				}
			} else {
				if (Input.touches.Length > 0) {
					var touch = Input.GetTouch (0);
					switch(touch.phase) {
					case TouchPhase.Began:
						OnBeginDrag (touch.position);
						break;
					case TouchPhase.Moved:
						OnDrag (touch.position, touch.deltaPosition);
						break;
					case TouchPhase.Ended:
						OnEndDrag (touch.position);
						break;
					default:
						break;
    				}
				}
			}

			lastUpdateTimer += Time.deltaTime;
			if (sendPosition && lastUpdateTimer >= updateInterval) {
				lastUpdateTimer = 0f;
				sendPosition = false;
				Send (variableNameX, lastInputPos.x);
				Send (variableNameY, lastInputPos.y);
			}
		}

		private void OnBeginDrag (Vector3 position) {
			sendPosition = true;
			lastInputPos = Vector3.zero;
			circleTransform.position = position;
			analogStickTransform.position = position;

			if (!string.IsNullOrEmpty (onDragStartedKey)) {
				Send (onDragStartedKey, true);
			}
		}

		private void OnDrag (Vector3 position, Vector3 deltaPosition) {
			sendPosition = true;
			Vector3 dist = position - circleTransform.position;
			Vector3 normalizedPos = new Vector3 (Mathf.Clamp (dist.x / Radius, -1f, 1f), Mathf.Clamp (dist.y / Radius, -1f, 1f), 0f);
			if (normalizedPos.sqrMagnitude > 1f) {
				normalizedPos = dist.normalized;
			}
			analogStickTransform.position = circleTransform.position + normalizedPos * Radius;

			lastInputPos = normalizedPos;
		}

		private void OnEndDrag (Vector3 position) {
			sendPosition = true;
			lastInputPos = Vector3.zero;
			circleTransform.position = startPos;
			analogStickTransform.position = startPos;

			if (!string.IsNullOrEmpty (onDragStartedKey)) {
				Send (onDragStartedKey, false);
			}
		}
	}
}
