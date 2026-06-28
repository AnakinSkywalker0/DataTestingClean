using System;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class StartTraining : MonoBehaviour
{

    [SerializeField] private TMP_InputField[] Input;
    [SerializeField] private TMP_Text WarningText;
    


    public void SaveData()
    {
        if (checkNameFeild())
        {
            string datastream = $"{Input[0].text},{Input[1].text},{Input[2].text},{Input[3].text}";
            CSVWriter.Save(datastream);
            Debug.Log(datastream + " saved to csv");
            Debug.Log($"height {float.Parse(Input[3].text)} " );
            StoreData.Store_Data(Input[0].text, Input[1].text, float.Parse(Input[2].text), float.Parse(Input[3].text));

            SceneManager.LoadScene("MainMenu");
            Debug.Log("MainMenu loaded");
        }
    }

    private bool checkNameFeild()
    {
        foreach (var input in Input)
        {
            if (String.IsNullOrEmpty(input.text) || String.IsNullOrWhiteSpace(input.text))
            {
                WarningText.text = " <!> please Enter Name , ID";
                WarningText.color = Color.red;
                return false;
            }
        }
        return true;
        }
    }