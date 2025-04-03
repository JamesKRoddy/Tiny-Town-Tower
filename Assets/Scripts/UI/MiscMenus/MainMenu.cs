using System;
using UnityEngine;

public class MainMenu : MenuBase
{
    public void StartBtn()
    {
        LoadScene("CampScene", GameMode.CAMP);
    }

    public void ContinueBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public void OptionsBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public void QuitBtn()
    {
        Debug.LogWarning("Not Implemented!!!!");
    }

    public override void Setup()
    {
        throw new NotImplementedException();
    }
}
