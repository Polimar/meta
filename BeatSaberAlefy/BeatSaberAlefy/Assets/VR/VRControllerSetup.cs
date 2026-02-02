using UnityEngine;
using System;
using System.Reflection;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Configura i controller XR e collega i Saber ai controller reali usando reflection
    /// per evitare dipendenze dirette da XR Interaction Toolkit nell'assembly principale.
    /// </summary>
    public class VRControllerSetup : MonoBehaviour
    {
        [Tooltip("Prefab Saber da collegare ai controller")]
        public GameObject SaberPrefab;

        void Start()
        {
            SetupControllers();
        }

        void SetupControllers()
        {
            try
            {
                // Usa reflection per accedere ai tipi XR Interaction Toolkit
                var xrControllerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRController, Unity.XR.Interaction.Toolkit");
                var xrDirectInteractorType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRDirectInteractor, Unity.XR.Interaction.Toolkit");
                var actionBasedControllerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedController, Unity.XR.Interaction.Toolkit");
                
                if (xrControllerType == null || xrDirectInteractorType == null)
                {
                    Debug.LogWarning("[VRControllerSetup] XR Interaction Toolkit non disponibile. I controller potrebbero non funzionare correttamente.");
                    return;
                }

                var xrOrigin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin == null)
                {
                    Debug.LogWarning("[VRControllerSetup] XR Origin non trovato.");
                    return;
                }

                // Cerca i controller esistenti o creali
                SetupController(xrOrigin, UnityEngine.XR.InputDeviceCharacteristics.Left, "Left Controller", 0);
                SetupController(xrOrigin, UnityEngine.XR.InputDeviceCharacteristics.Right, "Right Controller", 1);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VRControllerSetup] Errore nella configurazione: {ex.Message}");
            }
        }

        void SetupController(Unity.XR.CoreUtils.XROrigin xrOrigin, UnityEngine.XR.InputDeviceCharacteristics hand, string controllerName, int saberLane)
        {
            try
            {
                // Cerca controller esistente
                Transform controllerTransform = null;
                foreach (Transform child in xrOrigin.transform)
                {
                    if (child.name.Contains(controllerName) || 
                        (hand == UnityEngine.XR.InputDeviceCharacteristics.Left && child.name.Contains("Left")) ||
                        (hand == UnityEngine.XR.InputDeviceCharacteristics.Right && child.name.Contains("Right")))
                    {
                        controllerTransform = child;
                        break;
                    }
                }

                // Se non esiste, cerca i controller XR reali usando InputDevices
                if (controllerTransform == null)
                {
                    var inputDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                    UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(hand, inputDevices);
                    
                    if (inputDevices.Count > 0)
                    {
                        // Cerca GameObject che rappresenta questo controller
                        // I controller XR sono spesso creati automaticamente da XR Interaction Toolkit
                        var xrControllerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRController, Unity.XR.Interaction.Toolkit");
                        if (xrControllerType != null)
                        {
                            var allControllers = UnityEngine.Object.FindObjectsByType(xrControllerType, FindObjectsSortMode.None);
                            foreach (var ctrl in allControllers)
                            {
                                var nodeProp = xrControllerType.GetProperty("controllerNode");
                                if (nodeProp != null)
                                {
                                    var node = nodeProp.GetValue(ctrl);
                                    var nodeStr = node?.ToString() ?? "";
                                    bool isLeft = nodeStr.Contains("Left") || nodeStr.Contains("left");
                                    bool isRight = nodeStr.Contains("Right") || nodeStr.Contains("right");
                                    
                                    if ((hand == UnityEngine.XR.InputDeviceCharacteristics.Left && isLeft) ||
                                        (hand == UnityEngine.XR.InputDeviceCharacteristics.Right && isRight))
                                    {
                                        controllerTransform = (ctrl as MonoBehaviour).transform;
                                        Debug.Log($"[VRControllerSetup] Trovato controller XR reale: {controllerTransform.name}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Se ancora non esiste, cerca per nome comune dei controller XR
                if (controllerTransform == null)
                {
                    string[] commonNames = hand == UnityEngine.XR.InputDeviceCharacteristics.Left 
                        ? new[] { "LeftHand Controller", "Left Controller", "LeftHand", "Controller (Left)" }
                        : new[] { "RightHand Controller", "Right Controller", "RightHand", "Controller (Right)" };
                    
                    foreach (var name in commonNames)
                    {
                        var found = xrOrigin.transform.Find(name);
                        if (found != null)
                        {
                            controllerTransform = found;
                            Debug.Log($"[VRControllerSetup] Trovato controller per nome: {name}");
                            break;
                        }
                    }
                }

                // Se ancora non esiste, crea un controller placeholder che seguirà il controller reale
                if (controllerTransform == null)
                {
                    var controllerGo = new GameObject(controllerName);
                    controllerGo.transform.SetParent(xrOrigin.transform, false);
                    controllerTransform = controllerGo.transform;
                    Debug.LogWarning($"[VRControllerSetup] Controller {controllerName} non trovato, creato placeholder. I Saber potrebbero non seguire i controller reali.");
                }

                // Collega Saber se non esiste già
                if (SaberPrefab != null && controllerTransform.GetComponentInChildren<SliceDetector>() == null)
                {
                    var saber = Instantiate(SaberPrefab, controllerTransform);
                    saber.name = $"{controllerName} Saber";
                    saber.transform.localPosition = Vector3.zero;
                    saber.transform.localRotation = Quaternion.identity;

                    var sliceDetector = saber.GetComponent<SliceDetector>();
                    if (sliceDetector != null)
                    {
                        sliceDetector.SaberLane = saberLane;
                        var director = UnityEngine.Object.FindFirstObjectByType<GameplayDirector>();
                        if (director != null)
                        {
                            sliceDetector.AudioTimeProvider = director;
                            sliceDetector.AudioTimeMethodName = "GetAudioTime";
                        }
                    }
                    Debug.Log($"[VRControllerSetup] Saber collegato a {controllerName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VRControllerSetup] Errore nella configurazione di {controllerName}: {ex.Message}");
            }
        }
    }
}
