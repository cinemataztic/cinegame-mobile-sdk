using UnityEngine;
using System.Collections;

/// <summary>
/// Utility class for interpolating [0..1] on a quad or cubic curve
/// </summary>
public static class Interpolation {

	public enum Type {
		Linear = 0,
		EaseInQuad,
		EaseOutQuad,
		EaseInOutQuad,
		EaseInCubic,
		EaseOutCubic,
		EaseInOutCubic
	}

	public static float Interp (float t, Type type) {
		switch (type) {
		case Type.EaseInQuad:
			return EaseInQuad (t);
		case Type.EaseOutQuad:
			return EaseOutQuad (t);
		case Type.EaseInOutQuad:
			return EaseInOutQuad (t);
		case Type.EaseInCubic:
			return EaseInCubic (t);
		case Type.EaseOutCubic:
			return EaseOutCubic (t);
		case Type.EaseInOutCubic:
			return EaseInOutCubic (t);
		default:
			return t;
		}
	}

	/**
	 * Freely from: http://wiki.unity3d.com/index.php?title=Interpolate
	 * Original author: Fernando Zapata (fernando@cpudreams.com)
	 **/

	/**
     * quadratic easing in - accelerating from zero velocity
     */
	public static float EaseInQuad(float t) {
		return t * t;
	}
	
	/**
     * quadratic easing out - decelerating to zero velocity
     */
	public static float EaseOutQuad(float t) {
		return -t * (t - 2f);
	}
	
	/**
     * quadratic easing in/out - acceleration until halfway, then deceleration
     */
	public static float EaseInOutQuad(float t) {
		if (t < .5f) return EaseInQuad(t*2f) *.5f;
		return EaseOutQuad ((t-.5f)*2f) *.5f+.5f;
	}
	
	/**
     * cubic easing in - accelerating from zero velocity
     */
	public static float EaseInCubic(float t) {
		return t * t * t;
	}
	
	/**
     * cubic easing out - decelerating to zero velocity
     */
	public static float EaseOutCubic(float t) {
		float t1 = t - 1f;
		return t1 * t1 * t1 + 1f;
	}
	
	/**
     * cubic easing in/out - acceleration until halfway, then deceleration
     */
	public static float EaseInOutCubic(float t) {
		if (t < .5f) return EaseInCubic(t*2f) *.5f;
		return EaseOutCubic ((t-.5f)*2f) *.5f+.5f;
	}
	
}
