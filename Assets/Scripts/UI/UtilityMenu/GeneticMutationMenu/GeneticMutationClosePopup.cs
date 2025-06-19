using UnityEngine;

public class GeneticMutationClosePopup : PreviewPopupBase<object, string>
{

    protected override void Start()
    {
        base.Start();
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    public override void OnCloseClicked()
    {
        base.OnCloseClicked();
    }

}
