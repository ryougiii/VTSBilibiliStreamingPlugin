using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Klak.Ndi;
using UnityEngine;
using UnityEngine.UI;

public class ExtraController : MonoBehaviour {
    public Dropdown ndiSourceDropdown;
    public NdiReceiver vtsReceiver;
    public NdiSender vtsSender;

    public RectTransform configRoot;

    public void Start() {
        vtsReceiver.ndiName = PlayerPrefs.GetString("VtsNdiSourceName", "");
        vtsSender.ndiName = "Rainfall Room Only" + (Application.isEditor ? " (Editor)" : "");
    }
    
    public void ResetNdi() {
        var sources = NdiFinder.sourceNames.ToList();
        vtsReceiver.ndiName = sources.FirstOrDefault(a => a.Contains("Live2D")) ?? (sources.Any() ? sources[0] : "");
        ndiSourceDropdown.options = sources.Select(a => new Dropdown.OptionData(a)).ToList();
        ndiSourceDropdown.onValueChanged.RemoveAllListeners();
        ndiSourceDropdown.onValueChanged.AddListener((i) => {
            vtsReceiver.ndiName = sources[i];
            PlayerPrefs.SetString("VtsNdiSourceName", sources[i]);
            PlayerPrefs.Save();
        });
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            configRoot.gameObject.SetActive(!configRoot.gameObject.activeSelf);
        }
    }
}