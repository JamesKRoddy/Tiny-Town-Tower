using System.Collections.Generic;
using UnityEngine;

public class SelectionPreviewList : PreviewListMenuBase<string, WorldItemBase>
{
    public override void DestroyPreviewSpecifics()
    {
        throw new System.NotImplementedException();
    }

    public override string GetItemCategory(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerable<WorldItemBase> GetItems()
    {
        throw new System.NotImplementedException();
    }

    public override string GetPreviewDescription(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }

    public override string GetPreviewName(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerable<(string resourceName, int requiredCount, int playerCount)> GetPreviewResourceCosts(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }

    public override Sprite GetPreviewSprite(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }

    public override void SetupItemButton(WorldItemBase item, GameObject button)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdatePreviewSpecifics(WorldItemBase item)
    {
        throw new System.NotImplementedException();
    }
}
