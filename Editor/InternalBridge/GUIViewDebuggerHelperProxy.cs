using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal static class GUIViewDebuggerHelperProxy
    {
        public static void GetViews(List<GUIViewProxy> views)
        {
            List<GUIView> guiViews = new();
            GUIViewDebuggerHelper.GetViews(guiViews);
            foreach (GUIView view in guiViews)
                views.Add(new GUIViewProxy(view));
        }

        public static void DebugWindow(GUIViewProxy guiViewProxy)
        {
            GUIViewDebuggerHelper.DebugWindow(guiViewProxy.GuiView);
        }

        public static void GetDrawInstructions(List<IMGUIDrawInstructionProxy> drawInstructions)
        {
            List<IMGUIDrawInstruction> instructions = new();
            GUIViewDebuggerHelper.GetDrawInstructions(instructions);
            foreach (IMGUIDrawInstruction instruction in instructions)
                drawInstructions.Add(new IMGUIDrawInstructionProxy(instruction));
        }

        public static void GetNamedControlInstructions(List<IMGUINamedControlInstructionProxy> namedControlInstructions)
        {
            List<IMGUINamedControlInstruction> instructions = new();
            GUIViewDebuggerHelper.GetNamedControlInstructions(instructions);
            foreach (IMGUINamedControlInstruction instruction in instructions)
                namedControlInstructions.Add(new IMGUINamedControlInstructionProxy(instruction));
        }

        public static void GetPropertyInstructions(List<IMGUIPropertyInstructionProxy> propertyInstructions)
        {
            List<IMGUIPropertyInstruction> instructions = new();
            GUIViewDebuggerHelper.GetPropertyInstructions(instructions);
            foreach (IMGUIPropertyInstruction instruction in instructions)
                propertyInstructions.Add(new IMGUIPropertyInstructionProxy(instruction));
        }

        public static void GetUnifiedInstructions(List<IMGUIInstructionProxy> unifiedInstructions)
        {
            List<IMGUIInstruction> instructions = new();
            GUIViewDebuggerHelper.GetUnifiedInstructions(instructions);
            foreach (IMGUIInstruction instruction in instructions)
                unifiedInstructions.Add(new IMGUIInstructionProxy(instruction));
        }

        public static void StopDebugging()
        {
            GUIViewDebuggerHelper.StopDebugging();
        }
    }

    internal class IMGUIDrawInstructionProxy
    {
        IMGUIDrawInstruction m_IMGUIDrawInstruction;

        internal IMGUIDrawInstructionProxy(IMGUIDrawInstruction imguiDrawInstruction)
        {
            m_IMGUIDrawInstruction = imguiDrawInstruction;
        }

        public GUIContent usedGUIContent => m_IMGUIDrawInstruction.usedGUIContent;
        public Rect rect => m_IMGUIDrawInstruction.rect;
        public string usedGUIStyleName => m_IMGUIDrawInstruction.usedGUIStyle.name;
    }

    internal class IMGUINamedControlInstructionProxy
    {
        IMGUINamedControlInstruction m_IMGUINamedControlInstructionProxy;

        internal IMGUINamedControlInstructionProxy(IMGUINamedControlInstruction imguiNamedControlInstruction)
        {
            m_IMGUINamedControlInstructionProxy = imguiNamedControlInstruction;
        }

        public string name => m_IMGUINamedControlInstructionProxy.name;
        public Rect rect => m_IMGUINamedControlInstructionProxy.rect;
    }

    internal class IMGUIPropertyInstructionProxy
    {
        IMGUIPropertyInstruction m_IMGUIPropertyInstruction;

        internal IMGUIPropertyInstructionProxy(IMGUIPropertyInstruction imguiPropertyInstruction)
        {
            m_IMGUIPropertyInstruction = imguiPropertyInstruction;
        }

        public string targetTypeName => m_IMGUIPropertyInstruction.targetTypeName;
        public string path => m_IMGUIPropertyInstruction.path;
        public Rect rect => m_IMGUIPropertyInstruction.rect;
    }

    internal enum InstructionTypeProxy
    {
        StyleDraw = 1,
        ClipPush = 2,
        ClipPop = 3,
        LayoutBeginGroup = 4,
        LayoutEndGroup = 5,
        LayoutEntry = 6,
        PropertyBegin = 7,
        PropertyEnd = 8,
        LayoutNamedControl = 9,
    }

    internal class IMGUIInstructionProxy
    {
        IMGUIInstruction m_IMGUIInstruction;

        internal IMGUIInstructionProxy(IMGUIInstruction imguiInstruction)
        {
            m_IMGUIInstruction = imguiInstruction;
        }

        public InstructionTypeProxy type => (InstructionTypeProxy)m_IMGUIInstruction.type;
        public int level => m_IMGUIInstruction.level;
        public int typeInstructionIndex => m_IMGUIInstruction.typeInstructionIndex;
    }
}
