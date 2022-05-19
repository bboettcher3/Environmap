using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditorInternal;

public class MapperDevice : MonoBehaviour {
    
    IntPtr dev = IntPtr.Zero;
    IntPtr sig = IntPtr.Zero;

    [Serializable]
    public struct MapperSignal {
        public enum SignalDirection {
            INCOMING = 0x01,
            OUTGOING = 0x02
        }

        public SignalDirection Direction;
        public string Name;
        public float Min;
        public float Max;
        public float Value;
    }

    public string deviceName = "UnityDevice";
    public List<MapperSignal> signals = new List<MapperSignal>();
    
    // Start is called before the first frame update
    void Start()
    {
        //dev = mpr.mpr_dev_new("TestUnity");
        //sig = mpr.mpr_sig_new(dev, mpr.Direction.INCOMING, "test_sig", 1, mpr.Type.FLOAT);
    }
    
    // Update is called once per frame
    void Update()
    {   
        mpr.mpr_dev_poll(dev);
        IntPtr myValue = mpr.mpr_sig_get_value(sig);
        if (myValue != IntPtr.Zero) {
            unsafe {
                float fval = *(float*)myValue;
                gameObject.transform.localScale = Vector3.one * fval;
            }
        }     
    }
    
    // Normalized update function, called once every physics update (instead of per frame)
    private void FixedUpdate()
    {

        
          
    }
}

[CustomEditor(typeof(MapperDevice))]
public class MapperDeviceEditor : Editor {
    public SerializedProperty deviceName;
    private ReorderableList signalList;
    
    private void OnEnable() {
        deviceName = serializedObject.FindProperty("deviceName");

        signalList = new ReorderableList(serializedObject, 
                serializedObject.FindProperty("signals"), 
                true, true, true, true);

        signalList.onAddCallback = (ReorderableList l) => {
            var index = l.serializedProperty.arraySize;
            l.serializedProperty.arraySize++;
            l.index = index;
            var element = l.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Direction").enumValueIndex = 0;
            element.FindPropertyRelative("Name").stringValue = "signal_" + l.index;
            element.FindPropertyRelative("Min").floatValue = 0;
            element.FindPropertyRelative("Max").floatValue = 1;
        };

        signalList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = signalList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            var itemHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 80, EditorGUIUtility.singleLineHeight),
                "Signal Name");
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y + itemHeight, 80, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Name"), GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x + 82, rect.y, 90, EditorGUIUtility.singleLineHeight),
                "Direction");
            EditorGUI.PropertyField(
                new Rect(rect.x + 82, rect.y + itemHeight, 90, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Direction"), GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x + 174, rect.y, 40, EditorGUIUtility.singleLineHeight),
                "Min");
            EditorGUI.PropertyField(
                new Rect(rect.x + 174, rect.y + itemHeight, 40, EditorGUIUtility.singleLineHeight), 
                    element.FindPropertyRelative("Min"), GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x + 216, rect.y, 40, EditorGUIUtility.singleLineHeight),
                "Max");
            EditorGUI.PropertyField(
                new Rect(rect.x + 216, rect.y + itemHeight, 40, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Max"), GUIContent.none);
        };

        signalList.elementHeightCallback = (index) => {
            return EditorGUIUtility.singleLineHeight * 2 + 10;
        };

        signalList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Signals");
        };
    }
    
    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUILayout.PropertyField(deviceName);
        signalList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}

public class mpr
    {
        public enum Direction {
            INCOMING = 0x01,
            OUTGOING = 0x02,
            ANY = 0x03,
            BOTH = 0x04,
        }
        public enum Type {
            DOUBLE = 0x64,
            FLOAT = 0x66,
            INT32 = 0x69,
            INT64 = 0x68,
        }

        // Define libmapper function for new device
        [DllImport ("mapper")]
        public static extern IntPtr mpr_dev_new(String name_prefix, int graph);
        [DllImport ("mapper")]
        public static extern int mpr_dev_poll(IntPtr dev, int block_ms);
        [DllImport ("mapper")]
        private static extern IntPtr mpr_sig_new(IntPtr parent_dev, Direction dir, String name, int length,
                        Type type, int unit, int min, int max, int num_inst, int h, int events);
        [DllImport ("mapper")]                
        public static extern IntPtr mpr_sig_get_value(IntPtr signal, int instance, int time);
        [DllImport ("mapper")]
        public static extern void mpr_sig_set_value(IntPtr signal, int id, int len, Type type, IntPtr val);
        [DllImport ("mapper")]
        private static extern int mpr_sig_reserve_inst(IntPtr sig, int num_reservations, int[] ids, IntPtr[] values);
        [DllImport ("mapper")]
        public static extern void mpr_sig_release_inst(IntPtr sig, int id);
        [DllImport ("mapper")]
        public static extern int mpr_sig_get_inst_is_active(IntPtr sig, int id);
        
        // Function overloads to allow calling the function without unnecessary parameters 
        public static IntPtr mpr_dev_new(String name_prefix) {
            return mpr_dev_new(name_prefix, 0);
        }
        public static int mpr_dev_poll(IntPtr dev) {
            return mpr_dev_poll(dev, 0);
        }
        public static int mpr_sig_reserve_inst(IntPtr sig, int num_reservations) {
            return mpr_sig_reserve_inst(sig, num_reservations, null, null);
        }
        public static IntPtr mpr_sig_new(IntPtr parent_dev, Direction dir, String name, int length, Type type) {
            return mpr_sig_new(parent_dev, dir, name, length, type, 0, 0, 0, 0, 0, 0);
        }
        public static IntPtr mpr_sig_get_value(IntPtr signal) {
            return mpr_sig_get_value(signal, 0, 0);
        }
        public static IntPtr mpr_sig_get_value(IntPtr signal, int instance) {
            return mpr_sig_get_value(signal, instance, 0);
        }
    }