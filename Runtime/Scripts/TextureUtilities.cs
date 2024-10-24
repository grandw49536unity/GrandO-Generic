using UnityEngine;

namespace GrandO.Generic {

	public static class TextureUtilities {
		
		public static RenderTexture Texture2DToRenderTexture(Texture2D _input) { 
			RenderTexture output = new RenderTexture(_input.width, _input.height, 0);
			output.enableRandomWrite = true;
			Texture2DToRenderTexture(_input, output);
			return output;
		}

		public static void Texture2DToRenderTexture(Texture2D _input, RenderTexture _output) { 
			RenderTexture.active = _output;
			Graphics.Blit(_input, _output);
			RenderTexture.active = null;
		}

		public static Texture2D RenderTextureToTexture2D(RenderTexture _input, bool _applyGamma = false) {
			Texture2D output = new Texture2D(_input.width, _input.height);
			RenderTextureToTexture2D(_input, output, _applyGamma);
			return output;
		}

		public static void RenderTextureToTexture2D(RenderTexture _input, Texture2D _output, bool _applyGamma = false) {
			if (!_input || !_output) return;
			RenderTexture.active = _input;
			_output.ReadPixels(new Rect(0, 0, _input.width, _input.height), 0, 0);
			RenderTexture.active = null;
			if (_applyGamma) {
				Color[] colors = _output.GetPixels();
				for (int i = 0; i < colors.Length; i++) {
					colors[i] = colors[i].gamma;
				}
				_output.SetPixels(colors);
			}
			_output.Apply();
		}

	}

}