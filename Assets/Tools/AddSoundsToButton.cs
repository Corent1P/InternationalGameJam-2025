using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;

public class AddSoundToButtons : EditorWindow
{
    private GameObject soundManagerGO;

    [MenuItem("Tools/Add Sound To All Buttons")]
    public static void ShowWindow()
    {
        GetWindow<AddSoundToButtons>("Add Sound To Buttons");
    }

    private void OnGUI()
    {
        GUILayout.Label("Paramètres", EditorStyles.boldLabel);

        soundManagerGO = (GameObject)EditorGUILayout.ObjectField(
            "Sound Manager",
            soundManagerGO,
            typeof(GameObject),
            true
        );

        if (GUILayout.Button("Ajouter action OnClick à tous les Buttons"))
        {
            if (soundManagerGO == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Veuillez assigner le Sound Manager.", "OK");
                return;
            }

            AddOnClickToAllButtons();
        }
    }

    private void AddOnClickToAllButtons()
    {
        var buttons = GameObject.FindObjectsOfType<Button>(true);
        if (buttons.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "Aucun Button trouvé dans la scène.", "OK");
            return;
        }

        SoundManager soundManager = soundManagerGO.GetComponent<SoundManager>();
        if (soundManager == null)
        {
            EditorUtility.DisplayDialog("Erreur", "Le GameObject n’a pas de composant SoundManager.", "OK");
            return;
        }

        int added = 0;

        foreach (var btn in buttons)
        {
            // Vérifie si le listener existe déjà
            bool alreadyHas = false;
            int count = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                Object target = btn.onClick.GetPersistentTarget(i);
                string methodName = btn.onClick.GetPersistentMethodName(i);
                if (target == (Object)soundManager && methodName == nameof(SoundManager.PlayButtonPressedSound))
                {
                    alreadyHas = true;
                    break;
                }
            }

            if (alreadyHas)
                continue;

            Undo.RecordObject(btn, "Add Button Sound Listener");
            UnityEventTools.AddPersistentListener(btn.onClick, soundManager.PlayButtonPressedSound);
            EditorUtility.SetDirty(btn);
            added++;
        }

        if (added > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        EditorUtility.DisplayDialog("Résultat", $"Ajouté sur {added} Button(s).", "OK");
    }
}