using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Padoru.Networking
{

    [CustomEditor(typeof(NetworkIdentity))]
    public class NetworkIdentityEditor : Editor
    {
        private NetworkIdentity t;

        private void OnEnable()
        {
            t = (NetworkIdentity)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var id = t.Id;
            if(id == default)
            {
                EditorGUILayout.LabelField("Id", "Set at runtime");
            }
            else
            {
                EditorGUILayout.LabelField("Id", t.Id.ToString());
            }
        }
    }
}