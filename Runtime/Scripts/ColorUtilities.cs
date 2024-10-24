using UnityEngine;

namespace GrandO.Generic {

	public static class ColorUtilities {

		public static Color HSLToRGB(float _h, float _s, float _l, float _a = 1f) {
			if (_s == 0) {
				return new Color(_l, _l, _l, _a);
			}
			float q = _l < 0.5f ? _l * (1 + _s) : _l + _s - _l * _s;
			float p = 2 * _l - q;
			float r = HueToRGB(p, q, _h + 1.0f / 3.0f);
			float g = HueToRGB(p, q, _h);
			float b = HueToRGB(p, q, _h - 1.0f / 3.0f);
			return new Color(r, g, b, _a);
		}
    
		public static Color HSLToRGB(Vector3 _hslColor) {
			return HSLToRGB(_hslColor.x, _hslColor.y, _hslColor.z);
		}
    
		public static Color HSLToRGB(Vector4 _hslColor) {
			return HSLToRGB(_hslColor.x, _hslColor.y, _hslColor.z, _hslColor.w);
		}
		
		private static float HueToRGB(float p, float q, float t) {
			if (t < 0) t += 1;
			if (t > 1) t -= 1;
			if (t < 1.0f / 6.0f) return p + (q - p) * 6 * t;
			if (t < 1.0f / 2.0f) return q;
			if (t < 2.0f / 3.0f) return p + (q - p) * (2.0f / 3.0f - t) * 6;
			return p;
		}
		
		public static Vector4 RGBToHSL(this Color rgbColor) {
			float r = rgbColor.r,g = rgbColor.g,b = rgbColor.b;
			float max = Mathf.Max(r, Mathf.Max(g, b)), min = Mathf.Min(r, Mathf.Min(g, b));
			float h, s, l = (max + min) / 2;
			if (max == min) {
				h = s = 0; // achromatic
			} else {
				float d = max - min;
				s = l > 0.5f ? d / (2 - max - min) : d / (max + min);
				if (max == r)
					h = (g - b) / d + (g < b ? 6 : 0);
				else if (max == g)
					h = (b - r) / d + 2;
				else
					h = (r - g) / d + 4;
				h /= 6;
			}
			return new Vector4(h, s, l, rgbColor.a);
		}

	}

}