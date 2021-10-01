using MonoMod.Cil;
using MonoMod.Utils;
using Quintessential;
using System;

namespace ZoomTool {
	public class ZoomToolMod : QuintessentialMod {

		public static float Zoom = 1f;
		protected static RenderTargetHandle target;
		protected static bool waitForRender = false;

		public override void Load() {
			new DynamicData(typeof(class_110)).Set("field_1010", true);
		}

		public override void PostLoad() {
			target = new RenderTargetHandle(class_115.field_1433.FlooredToInt());
			On.SolutionEditorScreen.method_50 += Scale;
			On.SolutionEditorPartsPanel.method_221 += DisablePartsWhenCursed;
			On.SolutionEditorProgramPanel.method_221 += DisableProgramWhenCursed;
			On.class_115.method_202 += ScaleMousePos;
		}

		private Vector2 ScaleMousePos(On.class_115.orig_method_202 orig) {
			return waitForRender ? (orig() * Zoom) : orig();
		}

		private void DisableProgramWhenCursed(On.SolutionEditorProgramPanel.orig_method_221 orig, SolutionEditorProgramPanel self, float param_5658) {
			if(!waitForRender)
				orig(self, param_5658);
		}

		private void DisablePartsWhenCursed(On.SolutionEditorPartsPanel.orig_method_221 orig, SolutionEditorPartsPanel self, float param_5635) {
			if(!waitForRender)
				orig(self, param_5635);
		}

		private void Scale(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen self, float param_5703) {
			if(class_115.method_193((enum_143)1)) {
				if(class_115.method_198(SDL2.SDL.enum_160.SDLK_PERIOD)) {
					Zoom += 0.25f;
				} else if(class_115.method_198(SDL2.SDL.enum_160.SDLK_COMMA)) {
					Zoom -= 0.25f;
				}
				Zoom = Math.Max(0.25f, Math.Min(3, Zoom));
			}
			
			target.field_2987 = (class_115.field_1433 * Zoom).FlooredToInt();
			var origScreenSize = class_115.field_1433;
			class_115.field_1433 = class_115.field_1433 * Zoom;
			Matrix4 matrix4_1 = Matrix4.method_1081(target.field_2987, Renderer.method_1304());
			Matrix4 matrix4_2 = Matrix4.method_1069();
			class_95 class95_1 = target.method_1351();
			using(class_226.method_598(class95_1, class95_1.method_93(), matrix4_1, matrix4_2)) {
				waitForRender = true;
				orig(self, param_5703);
			}
			class_115.field_1433 = origScreenSize;
			class_135.method_263(target.method_1351().field_937, Color.White, /*image position*/new Vector2(0, 0), /*image width*/class_115.field_1433);
			waitForRender = false;
			self.field_4001.method_221(param_5703);
			self.field_4003.method_221(param_5703);
		}


		public override void Unload() {
			On.SolutionEditorScreen.method_50 -= Scale;
		}
	}
}
