using System;

namespace GameCore.Companions
{
    public interface ICompanionPurchaseProvider
    {
        CompanionStorePlatform GetCurrentPlatform();

        bool SupportsPlatformPurchase();

        string GetDisplayPrice(CompanionDef def);

        string GetProductId(CompanionDef def);

        void Purchase(CompanionDef def, Action<bool> onComplete);
    }
}