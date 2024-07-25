using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VRTemplate;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Trajectory;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;

public class JointRecordingAndUI : MonoBehaviour
{
    public ROSConnection ros;
    public GameObject robotUI;
    public String topicName;
    public List<Transform> knobs;
    public List<double> jointPositions;
    public List<String> jointNames;
    private bool recordROS = false;


    void Start() {
        LoadUI();
        if (ros == null) ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<JointTrajectoryMsg>(topicName);
        InvokeRepeating("sendJointPositionMessage", 1.0f, 1.0f); 
    }

    void LoadUI() {
        if (robotUI == null) Debug.Log("robotUI null");
        else Debug.Log("ui ok is loading");
        GameObject contentGameObject = robotUI.GetNamedChild("Spatial Panel Scroll").GetNamedChild("Scroll View").GetNamedChild("Viewport").GetNamedChild("Content");

        // button
        GameObject buttonObject = contentGameObject.GetNamedChild("List Item Button").GetNamedChild("Text Poke Button");
        Button button = buttonObject.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObject.GetNamedChild("Button Front").GetNamedChild("Text (TMP) ").GetComponent<TextMeshProUGUI>();

        button.onClick.AddListener(() => {
            if (recordROS == true) {
                recordROS = false;
                buttonText.text = "Start Recording";
            } else {
                recordROS = true;
                buttonText.text = "Stop Recording";
            }
        });

        // dropdown and slider
        TMP_Dropdown dropdown = contentGameObject.GetNamedChild("List Item Dropdown").GetNamedChild("Dropdown").GetComponent<TMP_Dropdown>();
        Slider slider = contentGameObject.GetNamedChild("List Item Slider").GetNamedChild("MinMax Slider").GetComponent<Slider>();
        TextMeshProUGUI sliderText = slider.gameObject.GetNamedChild("Value Text").GetComponent<TextMeshProUGUI>();

        dropdown.AddOptions(jointNames);
        int dropdownIndex = 0;
        slider.value = knobs[dropdownIndex].GetComponentInParent<XRKnob>().value;
        sliderText.text = knobs[dropdownIndex].transform.localRotation.eulerAngles.y.ToString();

        dropdown.onValueChanged.AddListener(delegate {
            dropdownIndex = dropdown.value;
            slider.value = knobs[dropdown.value].GetComponentInParent<XRKnob>().value;
        });

        slider.onValueChanged.AddListener(delegate {
            knobs[dropdownIndex].GetComponentInParent<XRKnob>().value = slider.value;
            sliderText.text = knobs[dropdownIndex].transform.localRotation.eulerAngles.y.ToString();
        });
    }

    void sendJointPositionMessage() {
        if (recordROS) {
            for (int i = 0; i < knobs.Count; i++) {
                jointPositions[i] = knobs[i].transform.localRotation.eulerAngles.y;
            }

            JointTrajectoryMsg jointTrajectory = new JointTrajectoryMsg();

            HeaderMsg header = new HeaderMsg
            {
                frame_id = gameObject.name,
                stamp = new TimeMsg {
                    sec = (int)Time.time,
                    nanosec = (uint)((Time.time - (int)Time.time) * 1e9)
                }
            };
            jointTrajectory.header = header;
            jointTrajectory.joint_names = jointNames.ToArray();

            JointTrajectoryPointMsg jointTrajectoryPoint = new JointTrajectoryPointMsg
            {
                positions = jointPositions.ToArray(), 
                time_from_start = new DurationMsg(1, 0),
            };
            jointTrajectory.points = new JointTrajectoryPointMsg[] { jointTrajectoryPoint };
            ros.Publish(topicName, jointTrajectory);
        }
    }
}
