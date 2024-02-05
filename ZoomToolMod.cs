using Quintessential;
using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace ZoomTool;

public class ZoomToolMod : QuintessentialMod{
	
	public override Type SettingsType => typeof(ZoomToolSettings);

	public static float Zoom = 1f;

	private static bool AdjustMousePos = false, AdjustBoardSize = false;
	private static RenderTargetHandle ScaledTarget;
	private static Hook ScaleBoardForDragSelectHook;

	public ZoomToolMod(){
		Settings = new ZoomToolSettings();
	}

	public override void Load(){}

	public override void PostLoad(){
		On.SolutionEditorBase.method_1984 += ScaleBoard;
		
		On.class_115.method_202 += ScaleMousePos;
		On.class_153.method_361 += ScaleMouseDrag;
		
		On.SolutionEditorScreen.method_2122 += ScaleBoardSize;
		On.class_153.method_360 += ScaleBoardForCentering;

		IL.SolutionEditorScreen.method_50 += ScaleSES;
		
		ScaleBoardForDragSelectHook = new Hook(
			typeof(class_223).GetMethod("method_567", BindingFlags.Instance | BindingFlags.NonPublic),
			typeof(ZoomToolMod).GetMethod(nameof(ScaleBoardForDragSelect))
		);
		
		ScaledTarget = new RenderTargetHandle(Input.ScreenSize().FlooredToInt());
	}

	public override void Unload(){
		On.SolutionEditorBase.method_1984 -= ScaleBoard;
		
		On.class_115.method_202 -= ScaleMousePos;
		On.class_153.method_361 -= ScaleMouseDrag;
		
		On.SolutionEditorScreen.method_2122 += ScaleBoardSize;
		On.class_153.method_360 -= ScaleBoardForCentering;
		
		IL.SolutionEditorScreen.method_50 -= ScaleSES;
		
		ScaleBoardForDragSelectHook.Dispose();
	}

	private static Vector2 ScaleMousePos(On.class_115.orig_method_202 orig){
		if(AdjustMousePos)
			return orig() * Zoom;
		return orig();
	}
	
	private static void ScaleMouseDrag(On.class_153.orig_method_361 orig, class_153 self, enum_135 mode, Vector2 ignore, Vector2 offset){
		if(AdjustMousePos)
			offset *= Zoom;
		orig(self, mode, ignore, offset);
	}
	
	private static Bounds2 ScaleBoardSize(On.SolutionEditorScreen.orig_method_2122 orig, SolutionEditorScreen self){
		if(AdjustBoardSize){
			Vector2 origSize = class_115.field_1433;
			class_115.field_1433 *= Zoom;
			Bounds2 ret = orig(self);
			class_115.field_1433 = origSize;
			return ret;
		}
		return orig(self);
	}
	
	private static void ScaleBoardForCentering(On.class_153.orig_method_360 orig, class_153 self){
		AdjustBoardSize = true;
		orig(self);
		AdjustBoardSize = false;
	}
	
	public static void ScaleBoard(
		On.SolutionEditorBase.orig_method_1984 orig,
		SolutionEditorBase self,
		Vector2 v1,
		Bounds2 bounds1,
		Bounds2 bounds2,
		bool b1,
		Maybe<List<Molecule>> mls,
		bool b2
	){
		if(self is not SolutionEditorScreen){
			orig(self, v1, bounds1, bounds2, b1, mls, b2);
			return;
		}

		ZoomToolSettings settings = QApi.GetSettingsByType<ZoomToolSettings>();
		if(settings.ZoomOut.Pressed())
			Zoom += 0.25f;
		else if(settings.ZoomIn.Pressed())
			Zoom -= 0.25f;
		Zoom = Math.Max(1, Math.Min(3, Zoom));

		RedrawScaled(() =>
			orig(self,
				v1,
				Bounds2.WithSize(bounds1.BottomLeft, bounds1.Size * Zoom),
				Bounds2.WithSize(bounds2.BottomLeft, bounds2.Size * Zoom),
				b1,
				mls,
				b2));
	}

	public static Bounds2 ScaleBoardForDragSelect(Func<class_223, SolutionEditorScreen, Bounds2> orig, class_223 self, SolutionEditorScreen ses){
		AdjustBoardSize = true;
		Bounds2 ret = orig(self, ses);
		AdjustBoardSize = false;
		return ret;
	}

	public static void ScaleSES(ILContext ctx){
		ILCursor cursor = new ILCursor(ctx);
		
		cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt<interface_0>("method_0"));
		cursor.EmitDelegate<Func<SolutionEditorScreen, SolutionEditorScreen>>((self) => {
			AdjustMousePos = self.method_2123();
			return self;
		});
		cursor.Goto(cursor.Next, MoveType.After);
		cursor.EmitDelegate<Action>(() => {
			AdjustMousePos = false;
		});
		
		cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt<interface_0>("method_2"));
		cursor.Remove();
		cursor.EmitDelegate<Action<interface_0, SolutionEditorScreen>>((mode, self) => {
			if(self.method_2123()){
				AdjustMousePos = true;
				RedrawScaled(() => mode.method_2(self));
				AdjustMousePos = false;
			}else
				mode.method_2(self);
		});
		
		cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt<interface_0>("method_1"));
		cursor.Remove();
		cursor.EmitDelegate<Action<interface_0, SolutionEditorScreen>>((mode, self) => {
			if(self.method_2123()){
				AdjustMousePos = true;
				RedrawScaled(() => mode.method_1(self));
				AdjustMousePos = false;
			}else
				mode.method_1(self);
		});
	}

	private static void RedrawScaled(Action orig){
		ScaledTarget.field_2987 = (class_115.field_1433 * Zoom).FlooredToInt();
		var origScreenSize = class_115.field_1433;

		class_115.field_1433 = origScreenSize * Zoom;
		Matrix4 matrix4_1 = Matrix4.method_1081(ScaledTarget.field_2987, Renderer.method_1304());
		Matrix4 matrix4_2 = Matrix4.method_1069();
		class_95 class95_1 = ScaledTarget.method_1351();
		using(class_226.method_598(class95_1, class95_1.method_93(), matrix4_1, matrix4_2)){
			// clear
			class_226.method_600(Color.Transparent);
			
			orig();
		}

		class_115.field_1433 = origScreenSize;
		class_135.method_263(ScaledTarget.method_1351().field_937, Color.White, /*image position*/new Vector2(0, 0), /*image width*/class_115.field_1433);
	}
}